using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    // NOTA: Velocidad y Salto se leen de GameManager

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
    [SerializeField] bool isGrounded;
    [SerializeField] bool isFacingRight;
    [SerializeField] LayerMask groundLayer;
    
    // Check points
    public BoxCollider2D groundCheckCollider; // Tu referencia actual
    [SerializeField] Transform groundCheck;   // Opcional si prefieres usar transform
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);

    [Header("INTERACT COLLIDER")]
    [SerializeField] Collider2D interactCollider;     
    [SerializeField] float interactActiveTime = 1f;   

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

    float coyoteTimeCounter;
    float jumpBufferCounter;
    float defaultGravity;

    // Getter para la IA
    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInput>();
        defaultGravity = playerRb.gravityScale;
        canAttack = true;

        if (interactCollider != null) interactCollider.enabled = false;
    }

    void Start()
    {
        isFacingRight = true;
    }

    void Update()
    {
        // LEER STATS DEL GAMEMANAGER
        float currentSpeed = 5f;
        if (GameManager.Instance != null) currentSpeed = GameManager.Instance.currentMoveSpeed;

        IsGrounded();
        
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
            // Movimiento
            float targetSpeed = currentSpeed;
            if (playerRb.linearVelocity.y < 0 || !isGrounded) targetSpeed /= airSpeedDivisor;
            
            playerRb.linearVelocity = new Vector2(moveInput.x * targetSpeed, playerRb.linearVelocity.y);

            if (moveInput.x > 0 && !isFacingRight) Flip();
            if (moveInput.x < 0 && isFacingRight) Flip();
            
            if(anim != null) anim.SetBool("IsRunning", moveInput.x != 0);
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

    void Jump()
    {
        // LEER STATS DEL GAMEMANAGER
        float currentJumpForce = 15f;
        if (GameManager.Instance != null) currentJumpForce = GameManager.Instance.currentJumpForce;

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector3.up * currentJumpForce, ForceMode2D.Impulse);

        canJump = false;
        Invoke(nameof(ResetJump), jumpCooldown);
        if(anim != null) anim.SetBool("Grounded", false);
    }

    void ResetJump() { canJump = true; }

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

    void IsGrounded()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer);
        else if (groundCheckCollider != null)
            isGrounded = groundCheckCollider.IsTouchingLayers(groundLayer);
            
        if(anim != null) anim.SetBool("Grounded", isGrounded);
    }

    // =========================
    // INPUT METHODS
    // =========================

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) { jumpBufferCounter = jumpBufferTime; isJumpPressed = true; }
        if (context.canceled) isJumpPressed = false;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && canAttack && !isDashing)
            StartCoroutine(Attack());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && !isAttacking)
        {
            if (interactCollider != null) 
            {
                interactCollider.enabled = true;
                Invoke(nameof(DeactivateInteractCollider), interactActiveTime);
                Debug.Log("Interactuando...");
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

    // =========================
    // CORRUTINAS
    // =========================

    void DeactivateInteractCollider()
    {
        if (interactCollider != null) interactCollider.enabled = false;
    }

    IEnumerator Attack()
    {
        canAttack = false; isAttacking = true;
        if(anim != null) anim.SetTrigger("Attack");
        
        playerRb.linearVelocity = Vector2.zero; // Frenar
        yield return new WaitForSeconds(0.5f); // Duración ataque
        
        isAttacking = false; canAttack = true;
    }

    IEnumerator DashRoutine()
    {
        canDash = false; isDashing = true;
        
        float originalGravity = playerRb.gravityScale;
        playerRb.gravityScale = 0;
        
        float dir = isFacingRight ? 1 : -1;
        if (moveInput.x != 0) dir = Mathf.Sign(moveInput.x);

        playerRb.linearVelocity = new Vector2(dir * dashSpeed, 0);

        // Efecto Visual (Debe estar el script PlayerDashEffect en el player)
        PlayerDashEffect effect = GetComponent<PlayerDashEffect>();
        if (effect != null) effect.ShowDashTrail(dashDuration);

        yield return new WaitForSeconds(dashDuration);

        playerRb.gravityScale = originalGravity;
        playerRb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}