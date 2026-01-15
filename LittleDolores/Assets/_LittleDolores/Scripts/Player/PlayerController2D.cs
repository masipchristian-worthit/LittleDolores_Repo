using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("MOVEMENT CONFIG")]
    [SerializeField] float speed = 10f;
    [SerializeField] float airSpeedDivisor = 2f;

    [Header("JUMP CONFIG")]
    [SerializeField] float jumpForce = 15f;
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

    [Header("INTERACT COLLIDER")]
    [SerializeField] Collider2D interactCollider;     // Collider apagado por defecto
    [SerializeField] float interactActiveTime = 1f;   // Activo durante 1 segundo

    Rigidbody2D playerRb;
    Animator anim;
    PlayerInput input;
    Vector2 moveInput;
    public BoxCollider2D groundCheckCollider;

    bool canAttack;
    bool canJump = true;
    bool isJumpPressed;
    bool jumpRequest;
    bool isAttacking;

    float coyoteTimeCounter;
    float jumpBufferCounter;
    float defaultGravity;

    private void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInput>();
        defaultGravity = playerRb.gravityScale;
        canAttack = true;

        // Aseguramos que el collider de interacción empieza apagado
        if (interactCollider != null)
            interactCollider.enabled = false;
    }

    void Start()
    {
        isFacingRight = true;
    }

    void Update()
    {
        IsGrounded();
        ApplyGravityScale();

        if (!isAttacking || !isGrounded)
        {
            if (moveInput.x > 0 && !isFacingRight) Flip();
            if (moveInput.x < 0 && isFacingRight) Flip();
        }

        CoyoteTime();
    }

    void FixedUpdate()
    {
        Movement();

        if (jumpRequest)
        {
            Jump();
            jumpRequest = false;
        }
    }

    void Flip()
    {
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
        isFacingRight = !isFacingRight;
    }

    void Jump()
    {
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);

        canJump = false;
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    IEnumerator Attack()
    {
        canAttack = false;
        isAttacking = true;

        yield return new WaitForSeconds(0.8f);

        isAttacking = false;
        canAttack = true;
        yield return null;
    }

    void IsGrounded()
    {
        isGrounded = groundCheckCollider.IsTouchingLayers(groundLayer);
    }

    void Movement()
    {
        if (isAttacking)
        {
            playerRb.linearVelocity = new Vector2(0, playerRb.linearVelocity.y);
            return;
        }

        float currentFrameSpeed = speed;

        if (playerRb.linearVelocity.y < 0)
        {
            currentFrameSpeed = speed / airSpeedDivisor;
        }

        playerRb.linearVelocity = new Vector2(moveInput.x * currentFrameSpeed, playerRb.linearVelocity.y);
    }

    void ResetJump()
    {
        canJump = true;
    }

    void CoyoteTime()
    {
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && canJump && !isAttacking)
        {
            jumpRequest = true;
            jumpBufferCounter = 0f;
        }
    }

    void ApplyGravityScale()
    {
        if (playerRb.linearVelocity.y < 0)
        {
            playerRb.gravityScale = fallMultiplier;
        }
        else if (playerRb.linearVelocity.y > 0 && !isJumpPressed)
        {
            playerRb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            playerRb.gravityScale = defaultGravity;
        }

        if (playerRb.linearVelocity.y < -maxFallSpeed)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -maxFallSpeed);
        }
    }

    // =========================
    // INTERACTION
    // =========================

    void ActivateInteractCollider()
    {
        if (interactCollider == null) return;

        interactCollider.enabled = true;
        Invoke(nameof(DeactivateInteractCollider), interactActiveTime);
    }

    void DeactivateInteractCollider()
    {
        if (interactCollider == null) return;

        interactCollider.enabled = false;
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
        if (context.started)
        {
            jumpBufferCounter = jumpBufferTime;
            isJumpPressed = true;
        }

        if (context.canceled)
        {
            isJumpPressed = false;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && canAttack)
            StartCoroutine(Attack());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && !isAttacking)
        {
            ActivateInteractCollider();
            Debug.Log("Collider de interacción ACTIVADO durante 1 segundo");
        }
    }
}
