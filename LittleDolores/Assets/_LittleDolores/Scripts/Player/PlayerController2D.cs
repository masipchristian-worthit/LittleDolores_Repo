using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("MOVEMENT CONFIG")]
    [SerializeField] float speed = 10f;
    [SerializeField] float airSpeedDivisor = 2f; // Por cuánto se divide la velocidad en el aire

    [Header("JUMP CONFIG")]
    [SerializeField] float jumpForce = 15f;
    [SerializeField] float jumpCooldown = 0.2f;    // Tiempo de espera entre saltos
    [SerializeField] float coyoteTime = 0.2f;      // Tiempo para saltar tras caer del borde
    [SerializeField] float jumpBufferTime = 0.2f;  // Tiempo que se recuerda el botón pulsado antes de tocar suelo

    [Header("BETTER JUMP")]
    [SerializeField] float fallMultiplier = 2.5f;     // Gravedad al caer (caída rápida)
    [SerializeField] float lowJumpMultiplier = 2f;    // Gravedad al soltar botón (salto corto)
    [SerializeField] float maxFallSpeed = 20f;       // Velocidad máxima de caída

    [Header("CHECKS")]
    [SerializeField] bool isGrounded;
    [SerializeField] bool isFacingRight;
    [SerializeField] LayerMask groundLayer;


    //Variables de referencia general
    Rigidbody2D playerRb; //Almacén del rigidbody del player
    Animator anim; //Almacén del controlador de animaciones del player
    PlayerInput input; //Almacén del controlador de inputs del player
    Vector2 moveInput; //Almacén del valor de los botones de movimiento
    public BoxCollider2D groundCheckCollider; //Almacén del collider del groundCheck

    // Bools de control de estado
    bool canAttack; //Bool de seguridad que define si se puede atacar o no
    bool canJump = true; // Para el Cooldown
    bool isJumpPressed; // Para el salto variable
    bool jumpRequest; // Variable para comunicar Update con FixedUpdate
    bool isAttacking; // Bool que define si el player está atacando


    // Contadores para Game Feel
    float coyoteTimeCounter;
    float jumpBufferCounter;
    float defaultGravity;



    private void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>(); //Autoreferenciar un componente propio
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInput>();
        defaultGravity = playerRb.gravityScale; // Guardamos la gravedad inicial
        canAttack = true;
    }
  
    void Start()
    {
        isFacingRight = true;
    }

    void Update()
    {
        //Logica de detección del suelo
        IsGrounded();

        //Logica de las animaciones
        //AnimationManagement();


        //Gravedad dinámica
        ApplyGravityScale();

        //Logica del flip del personaje (Solo giramos si NO estamos atacando)
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

        //Si hay una solicitud de salto
        if (jumpRequest)
        {
            Jump();
            jumpRequest = false; // y bajamos la bandera.
        }
    }

    void Flip()
    {
        Vector3 currentScale = transform.localScale; //Almacén temporal de la escala del objeto
        currentScale.x *= -1; //Invertur el valor en X
        transform.localScale = currentScale; //Le devolvemos la escala al objeto con el valor en X inverso
        isFacingRight = !isFacingRight; //Decirle al bool que cambie al valor contrario
    }

    void Jump()
    {
        // Resetea contadores para evitar dobles saltos accidentales
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        // Aplicar fuerza (reseteando la velocidad y para consistencia)
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0); 
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);

        // Activar Cooldown
        canJump = false;
        Invoke(nameof(ResetJump), jumpCooldown);

        // AudioManager.Instance.PlaySFX(3);
    }


    IEnumerator Attack()
    {
        // CORREGIDO: Usamos bandera en lugar de speed = 0
        canAttack = false; //Quitar la posibilidad de atacar
        isAttacking = true; // Bloqueamos movimiento

        //anim.SetTrigger("Attack");
        
        yield return new WaitForSeconds(0.8f);
        
        isAttacking = false; // Devolvemos movimiento
        canAttack = true;
        //Devolvemos vel y capacidad al jugador, acaba la corrrutina
        yield return null;
    }


    //void AnimationManagement()
    //{
        //Accion para gestionar los cambios de animacion
        //anim.SetBool("Jump", !isGrounded && playerRb.linearVelocity.y > 0);
       // if (moveInput.x != 0 && isGrounded) anim.SetBool("Run", true);
       // else anim.SetBool("Run", false);
    //}

    void IsGrounded()
    {
        //Chequeo de suelo usando OverlapCircle
        isGrounded = groundCheckCollider.IsTouchingLayers(groundLayer); 
        if (isGrounded)
        {
            Debug.Log("Player is grounded");
        }
    }   

    void Movement()
    {
        // CORREGIDO: Si atacamos, frenamos en seco en X y salimos
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
        // COYOTE TIME
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime; // Si estamos en suelo, reseteamos el contador
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime; // Si no, empieza la cuenta atrás
        }

        // Lapso de tiempo en el que se puede saltar tras caer del borde
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Petición de salto (Añadido !isAttacking para no saltar atacando)
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && canJump && !isAttacking)
        {
            jumpRequest = true; // "¡Oye FixedUpdate, quiero saltar!"
            jumpBufferCounter = 0f; // Consumimos el buffer para que no se repita
        }
    }

    void ApplyGravityScale()
    {
        if (playerRb.linearVelocity.y < 0) // Si estamos cayendo
        {
            playerRb.gravityScale = fallMultiplier; // Aumentamos la gravedad
        }
        else if (playerRb.linearVelocity.y > 0 && !isJumpPressed) // Si estamos subiendo pero soltamos el botón
        {
            playerRb.gravityScale = lowJumpMultiplier; // Aumentamos la gravedad para un salto más corto
        }
        else // En cualquier otro caso (subiendo con botón o en suelo)
        {
            playerRb.gravityScale = defaultGravity; // Gravedad normal
        }
        // Si estamos cayendo más rápido que el límite
        if (playerRb.linearVelocity.y < -maxFallSpeed)
        {
            // Forzamos la velocidad a ser exactamente la máxima permitida
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -maxFallSpeed);
        }
    }



    #region Input Methods

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Cuando pulsamos el botón
        if (context.started)
        {
            jumpBufferCounter = jumpBufferTime; // Activamos la "memoria" del salto
            isJumpPressed = true;               // Registramos que el botón está apretado
        }

        // Cuando soltamos el botón
        if (context.canceled)
        {
            isJumpPressed = false;              // Registramos que se soltó (para cortar el salto)
        }

        Debug.Log("Jump input received");   
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && canAttack) StartCoroutine(Attack());
    }

    #endregion

}