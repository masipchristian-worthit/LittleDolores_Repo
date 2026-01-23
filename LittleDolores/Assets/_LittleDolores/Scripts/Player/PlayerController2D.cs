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

    [Header("Attack Hitbox")]
    [SerializeField] BoxCollider2D attackHitbox;
    [Tooltip("Tag the attack hitbox GameObject as 'PlayerAttack' - Remove PlayerAttackHitbox and PlayerWeapon scripts!")]

    [Header("Interact Hitbox")]
    [SerializeField] Collider2D interactCollider;     
    [SerializeField] float interactActiveTime = 1f;   

    [Header("FEEDBACK VISUAL")]
    [SerializeField] SpriteRenderer spriteRenderer; 
    [SerializeField] Color damageColor = new Color(1f, 0f, 0f, 0.5f); // Rojo semi-transparente
    [SerializeField] Color healColor = new Color(0f, 1f, 0f, 0.5f);   // Verde semi-transparente
    [SerializeField] float flashDuration = 0.15f;
    [SerializeField] int flashCount = 3; // Cuántas veces parpadea

    private Coroutine currentFlashRoutine;

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

    float coyoteTimeCounter;
    float jumpBufferCounter;
    float attackBufferCounter; // NEW: Attack input buffer
    float defaultGravity;
    
    [Header("Attack Buffer")]
    [SerializeField] float attackBufferTime = 0.2f;

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
    }

    void Update()
    {
        float currentSpeed = 5f; 
        if (GameManager.Instance != null) currentSpeed = GameManager.Instance.currentMoveSpeed;

        CheckGround();
        
        AnimationManager();

        // Timers
        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;

        // Jump - Can't jump during attack or dash
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && canJump && !isAttacking && !isDashing)
        {
            jumpRequest = true;
            jumpBufferCounter = 0f;
        }

        if (!isDashing) ApplyGravityScale();

        // Movement - Blocked by attack AND dash
        if (!isAttacking && !isDashing)
        {
            float targetSpeed = currentSpeed;
            if (playerRb.linearVelocity.y < 0 || !isGrounded) targetSpeed /= airSpeedDivisor;
            
            playerRb.linearVelocity = new Vector2(moveInput.x * targetSpeed, playerRb.linearVelocity.y);

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

        // Run animation only when moving and not attacking/dashing
        bool isRunning = moveInput.x != 0 && !isAttacking && !isDashing;
        anim.SetBool("Run", isRunning);

        anim.SetBool("Grounded", isGrounded);
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
        if (playerRb.linearVelocity.y < 0) playerRb.gravityScale = defaultGravity * fallMultiplier;
        else if (playerRb.linearVelocity.y > 0 && !isJumpPressed) playerRb.gravityScale = defaultGravity * lowJumpMultiplier;
        else playerRb.gravityScale = defaultGravity;

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
        
        if(anim != null) anim.SetTrigger("Attack");

        // STOP MOVEMENT COMPLETELY
        playerRb.linearVelocity = new Vector2(0, playerRb.linearVelocity.y);
        // ENCENDER HITBOX
        if (attackHitbox != null) attackHitbox.enabled = true;

        yield return new WaitForSeconds(0.1f); 
        
        // === COMENTA ESTO PARA LA PRUEBA ===
        // if (attackHitbox != null) attackHitbox.enabled = false; 
        // ===================================

        yield return new WaitForSeconds(0.2f); 
        isAttacking = false; 
        canAttack = true;
    }

    IEnumerator DashRoutine()
    {
        canDash = false; 
        isDashing = true;
        
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

    //FEEDBACK VISUAL
    public void VisualDamage()
    {
        // 1. Si ya hay un parpadeo ocurriendo, detenemos SOLO ese.
        if (currentFlashRoutine != null)
        {
            StopCoroutine(currentFlashRoutine);
        }
        
        // 2. Iniciamos el nuevo y guardamos la referencia
        currentFlashRoutine = StartCoroutine(FlashRoutine(damageColor));
    }

    public void VisualHeal()
    {
        if (currentFlashRoutine != null)
        {
            StopCoroutine(currentFlashRoutine);
        }
        currentFlashRoutine = StartCoroutine(FlashRoutine(healColor));
    }

    IEnumerator FlashRoutine(Color targetColor)
    {
        // Asumimos blanco por defecto, si tu sprite tiene otro color base, cámbialo aquí
        Color originalColor = Color.white; 

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = targetColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        // Limpiamos la referencia al terminar
        currentFlashRoutine = null;
    }

    //Dibujado de Gizmos
    private void OnDrawGizmos()
    {
        if (groundCheck != null) 
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}