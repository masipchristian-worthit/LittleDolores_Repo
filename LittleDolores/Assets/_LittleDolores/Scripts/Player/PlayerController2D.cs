using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("MOVEMENT CONFIG")]
    [SerializeField] float airSpeedDivisor = 2f;

    [Header("DASH CONFIG")]
    [SerializeField] float dashSpeed = 20f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 1f;

    [Header("JUMP CONFIG")]
    [SerializeField] float jumpCooldown = 0.2f;
    [SerializeField] float coyoteTime = 0.2f;
    [SerializeField] float jumpBufferTime = 0.2f;

    [Header("BETTER JUMP")]
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2f;
    [SerializeField] float maxFallSpeed = 20f;

    [Header("CHECKS")]
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] LayerMask groundLayer;
    [SerializeField] bool isGrounded;

    public BoxCollider2D groundCheckCollider;

    [Header("Wall Detection")]
    [SerializeField] Transform wallCheck;
    [SerializeField] float wallCheckDistance = 0.2f;
    [SerializeField] LayerMask wallLayer;

    [Header("Attack Hitbox")]
    [SerializeField] BoxCollider2D attackHitbox;

    [Tooltip("Tag the attack hitbox GameObject as 'PlayerAttack' - Remove PlayerAttackHitbox and PlayerWeapon scripts!")]

    [Header("Interact Hitbox")]
    [SerializeField] Collider2D interactCollider;
    [SerializeField] float interactActiveTime = 1f;

    [Header("FEEDBACK VISUAL")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Color damageColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] Color healColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] float flashDuration = 0.15f;
    [SerializeField] int flashCount = 3;

    [Header("AUDIO")]
    [SerializeField] int landingSoundIndex = 0;
    [Tooltip("Índice del sonido de aterrizaje en el sfxLibrary del AudioManager")]
    [SerializeField] [Range(0f, 1f)] float landingSoundVolume = 1f;
    [Tooltip("Volumen del sonido de aterrizaje (0 = silencio, 1 = máximo)")]
    
    [SerializeField] int dashSoundIndex = 1;
    [Tooltip("Índice del sonido de dash en el sfxLibrary del AudioManager")]
    [SerializeField] [Range(0f, 1f)] float dashSoundVolume = 1f;
    [Tooltip("Volumen del sonido de dash (0 = silencio, 1 = máximo)")]

    private Coroutine currentFlashRoutine;
    private int lastKnownHealth;

    Rigidbody2D playerRb;
    Animator anim;
    PlayerInput input;
    Vector2 moveInput;

    bool canAttack;
    bool canJump = true;
    bool isJumpPressed;
    bool jumpRequest;
    bool isAttacking;
    bool isDashing;
    bool canDash = true;
    bool isFacingRight = true;
    bool wasGrounded;

    float coyoteTimeCounter;
    float jumpBufferCounter;
    float attackBufferCounter;
    float defaultGravity;

    [Header("Attack Buffer")]
    [SerializeField] float attackBufferTime = 0.2f;

    [Header("AnimationBools")]
    bool isJumping => playerRb.linearVelocity.y > 0.1f && !isGrounded;
    bool isFalling => playerRb.linearVelocity.y < -0.1f && !isGrounded && !isJumping;
    bool isMoving => Mathf.Abs(playerRb.linearVelocity.x) > 0.1f;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInput>();
        defaultGravity = playerRb.gravityScale;
        canAttack = true;

        if (interactCollider != null) interactCollider.enabled = false;
        if (attackHitbox != null) attackHitbox.enabled = false;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        isFacingRight = true;
        wasGrounded = isGrounded;
        
        // Inicializar la salud conocida
        if (GameManager.Instance != null)
        {
            lastKnownHealth = GameManager.Instance.playerHealth;
        }
    }

    void Update()
    {
        float currentSpeed = 5f;
        if (GameManager.Instance != null) currentSpeed = GameManager.Instance.currentMoveSpeed;

        CheckGround();
        CheckHealthChange();
        
        // Detectar cuando el jugador aterriza
        if (isGrounded && !wasGrounded)
        {
            OnLanding();
        }
        wasGrounded = isGrounded;

        AnimationManager();

        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && canJump && !isAttacking && !isDashing)
        {
            jumpRequest = true;
            jumpBufferCounter = 0f;
        }

        if (!isDashing) ApplyGravityScale();

        if (!isAttacking && !isDashing)
        {
            float targetSpeed = currentSpeed;
            if (playerRb.linearVelocity.y < 0 || !isGrounded) targetSpeed /= airSpeedDivisor;

            // Detectar pared para evitar que se pegue
            bool hitWall = false;
            if (wallCheck != null)
            {
                hitWall = Physics2D.Raycast(wallCheck.position, isFacingRight ? Vector2.right : Vector2.left, 
                    wallCheckDistance, wallLayer);
            }

            // Solo aplicar velocidad horizontal si no está tocando una pared o si se está moviendo en dirección contraria
            if (!hitWall || Mathf.Sign(moveInput.x) != (isFacingRight ? 1 : -1))
            {
                playerRb.linearVelocity = new Vector2(moveInput.x * targetSpeed, playerRb.linearVelocity.y);
            }
            else
            {
                // Si está contra la pared, eliminar la velocidad horizontal
                playerRb.linearVelocity = new Vector2(0, playerRb.linearVelocity.y);
            }

            if (moveInput.x > 0 && !isFacingRight) Flip();
            if (moveInput.x < 0 && isFacingRight) Flip();
        }
    }

    void FixedUpdate()
    {
        if (jumpRequest)
        {
            Jump();
            jumpRequest = false;
        }
    }

    void AnimationManager()
    {
        if (anim == null) return;

        if (isGrounded) anim.SetBool("Grounded", true);
        else anim.SetBool("Grounded", false);

        if (isMoving) anim.SetBool("Run", true);
        else anim.SetBool("Run", false);

        if (isJumping) anim.SetBool("Jump", true);
        else anim.SetBool("Jump", false);

        if (isFalling) anim.SetBool("Fall", true);
        else anim.SetBool("Fall", false);
    }

    void Jump()
    {
        float currentJumpForce = 15f;
        if (GameManager.Instance != null) currentJumpForce = GameManager.Instance.currentJumpForce;

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector3.up * currentJumpForce, ForceMode2D.Impulse);

        canJump = false;
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    void ResetJump()
    {
        canJump = true;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    void ApplyGravityScale()
    {
        if (playerRb.linearVelocity.y < 0)
            playerRb.gravityScale = defaultGravity * fallMultiplier;
        else if (playerRb.linearVelocity.y > 0 && !isJumpPressed)
            playerRb.gravityScale = defaultGravity * lowJumpMultiplier;
        else
            playerRb.gravityScale = defaultGravity;

        if (playerRb.linearVelocity.y < -maxFallSpeed)
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -maxFallSpeed);
    }

    void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer);
        }
        else if (groundCheckCollider != null)
        {
            isGrounded = groundCheckCollider.IsTouchingLayers(groundLayer);
        }
    }

    void OnLanding()
    {
        // Reproducir sonido de aterrizaje con volumen personalizado
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(landingSoundIndex, landingSoundVolume);
        }
    }

    void CheckHealthChange()
    {
        if (GameManager.Instance == null) return;

        int currentHealth = GameManager.Instance.playerHealth;

        // Detectar daño
        if (currentHealth < lastKnownHealth)
        {
            VisualDamage();
        }
        // Detectar curación
        else if (currentHealth > lastKnownHealth)
        {
            VisualHeal();
        }

        lastKnownHealth = currentHealth;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpBufferCounter = jumpBufferTime;
            isJumpPressed = true;
        }
        if (context.canceled) isJumpPressed = false;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && canAttack && !isDashing)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && !isAttacking)
        {
            if (interactCollider != null)
            {
                interactCollider.enabled = true;
                Invoke(nameof(DeactivateInteractCollider), interactActiveTime);
            }
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && !isDashing)
        {
            if (GameManager.Instance != null && GameManager.Instance.hasDashAbility)
            {
                StartCoroutine(DashRoutine());
            }
        }
    }

    void DeactivateInteractCollider()
    {
        if (interactCollider != null) interactCollider.enabled = false;
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
        canAttack = true;
    }

    IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        // Reproducir sonido de dash con volumen personalizado
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(dashSoundIndex, dashSoundVolume);
        }

        float originalGravity = playerRb.gravityScale;
        playerRb.gravityScale = 0;

        float dir = isFacingRight ? 1 : -1;
        if (moveInput.x != 0) dir = Mathf.Sign(moveInput.x);

        playerRb.linearVelocity = new Vector2(dir * dashSpeed, 0);

        PlayerDashEffect effect = GetComponent<PlayerDashEffect>();
        if (effect != null) effect.ShowDashTrail(dashDuration);

        yield return new WaitForSeconds(dashDuration);

        playerRb.gravityScale = originalGravity;
        playerRb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void VisualDamage()
    {
        if (spriteRenderer == null) return;

        if (currentFlashRoutine != null)
        {
            StopCoroutine(currentFlashRoutine);
        }

        currentFlashRoutine = StartCoroutine(FlashRoutine(damageColor));
    }

    public void VisualHeal()
    {
        if (spriteRenderer == null) return;

        if (currentFlashRoutine != null)
        {
            StopCoroutine(currentFlashRoutine);
        }

        currentFlashRoutine = StartCoroutine(FlashRoutine(healColor));
    }

    IEnumerator FlashRoutine(Color targetColor)
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = targetColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        // Asegurar que el sprite vuelva al color original
        spriteRenderer.color = originalColor;
        currentFlashRoutine = null;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Vector3 direction = isFacingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawRay(wallCheck.position, direction * wallCheckDistance);
        }
    }
}