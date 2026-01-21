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
    [SerializeField] bool isGrounded;
    [SerializeField] bool isFacingRight;
    [SerializeField] LayerMask groundLayer;

    [Header("Attack Hitbox")]
    [SerializeField] BoxCollider2D attackHitbox;

    [Header("Interact Hitbox")]
    [SerializeField] BoxCollider2D interactionCollider; // Collider que se activa al interactuar

    public BoxCollider2D groundCheckCollider;
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);

    Rigidbody2D playerRb;
    Animator anim;
    Vector2 moveInput;

    bool canAttack = true;
    bool canJump = true;
    bool isJumpPressed;
    bool jumpRequest;
    bool isAttacking;
    bool isDashing;
    bool canDash = true;

    float coyoteTimeCounter;
    float jumpBufferCounter;
    float defaultGravity;

    //IA DE ENEMIGOS
    public bool IsAttacking => isAttacking;

    void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        defaultGravity = playerRb.gravityScale;

        if (interactionCollider != null)
            interactionCollider.enabled = false; // Asegurarse que empieza apagado
    }

    void Start()
    {
        isFacingRight = true;
    }

    void Update()
    {
        float currentSpeed = 5f;
        if (GameManager.Instance != null)
            currentSpeed = GameManager.Instance.currentMoveSpeed;

        CheckGround();

        coyoteTimeCounter = isGrounded ? coyoteTime : coyoteTimeCounter - Time.deltaTime;
        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && canJump && !isAttacking && !isDashing)
        {
            jumpRequest = true;
            jumpBufferCounter = 0;
        }

        if (!isDashing)
            ApplyGravityScale();

        if (!isAttacking && !isDashing)
        {
            float targetSpeed = currentSpeed;
            if (!isGrounded)
                targetSpeed /= airSpeedDivisor;

            playerRb.linearVelocity = new Vector2(moveInput.x * targetSpeed, playerRb.linearVelocity.y);

            if (moveInput.x > 0 && !isFacingRight) Flip();
            if (moveInput.x < 0 && isFacingRight) Flip();
        }

        // ===== ANIMACIONES =====
        if (anim != null)
        {
            anim.SetBool("Grounded", isGrounded);
            anim.SetBool("IsRunning", moveInput.x != 0 && isGrounded);
            anim.SetBool("IsAttacking", isAttacking);
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
        float currentJumpForce = 15f;
        if (GameManager.Instance != null)
            currentJumpForce = GameManager.Instance.currentJumpForce;

        coyoteTimeCounter = 0;
        jumpBufferCounter = 0;

        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector2.up * currentJumpForce, ForceMode2D.Impulse);

        canJump = false;
        Invoke(nameof(ResetJump), jumpCooldown);

        if (anim != null)
            anim.SetTrigger("Jump");
    }

    void ResetJump() => canJump = true;

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
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer);
        else if (groundCheckCollider != null)
            isGrounded = groundCheckCollider.IsTouchingLayers(groundLayer);
    }

    // ===== INPUTS =====

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
        if (context.canceled)
            isJumpPressed = false;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && canAttack && !isDashing)
            StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        canAttack = false;
        isAttacking = true;

        if (anim != null)
            anim.SetTrigger("Attack");

        playerRb.linearVelocity = Vector2.zero;

        // ACTIVAR HITBOX
        if (attackHitbox != null)
            attackHitbox.enabled = true;

        yield return new WaitForSeconds(0.2f); // ventana de golpe

        // DESACTIVAR HITBOX
        if (attackHitbox != null)
            attackHitbox.enabled = false;

        yield return new WaitForSeconds(0.3f); // resto animación

        isAttacking = false;
        canAttack = true;
    }

    // ===== INTERACT =====
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && interactionCollider != null && !interactionCollider.enabled)
        {
            StartCoroutine(ActivateColliderTemporarily());
        }
    }

    IEnumerator ActivateColliderTemporarily()
    {
        interactionCollider.enabled = true;
        yield return new WaitForSeconds(1f);
        interactionCollider.enabled = false;
    }

    // Para ver el collider de interacción en la escena (opcional)
    void OnDrawGizmosSelected()
    {
        if (interactionCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(interactionCollider.transform.position, interactionCollider.size);
        }
    }
}
