using System.Collections;
using UnityEngine;

public class GShroomEnemy : MonoBehaviour
{
    [Header("ESTADÍSTICAS & MOVIMIENTO")]
    [SerializeField] int health = 2;
    [SerializeField] int damageToPlayer = 1;
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float runSpeed = 4.5f;
    [SerializeField] float jumpForce = 7f; // Fuerza para saltar
    [SerializeField] float decisionInterval = 0.5f;

    [Header("PROBABILIDADES")]
    [Range(0, 100)] public int aggroChance = 40;
    [Range(0, 100)] public int flankChance = 30;
    [Range(0, 100)] public int defendChance = 20;
    [Range(0, 100)] public int fleeChance = 10;

    [Header("SENSORES")]
    [SerializeField] float detectionRange = 8f;
    [SerializeField] float attackRange = 1.2f;
    [SerializeField] Transform wallCheck;
    [SerializeField] Transform edgeCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Vector2 wallCheckSize = new Vector2(0.5f, 0.5f);
    [SerializeField] float edgeCheckRadius = 0.1f;

    [Header("AUDIO")]
    public int sfxAttackIdx = -1;
    public int sfxHitIdx = -1;
    public int sfxDeathIdx = -1;

    // REFERENCIAS INTERNAS
    Rigidbody2D rb;
    Animator anim;
    Collider2D myCollider; // Necesario para atravesar plataformas
    Transform currentWeakSpot;
    Transform homePoint;

    // ESTADOS
    enum State { Patrol, Chase, Attack, Flee, Flank, Defend, AmbushFall }
    [Header("DEBUG ESTADO")]
    [SerializeField] State currentState = State.Patrol;

    // FLAGS DE CONTROL
    bool isFacingRight = true;
    bool isGrounded;
    bool isDead;
    bool isUnhappy;
    bool isWaitingAtEdge;
    bool isInAmbushPosition;
    float nextDecisionTime;
    
    // COOLDOWNS
    float jumpCooldown = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();

        // OPTIMIZACIÓN AUTOMÁTICA (Lo que pediste)
        // Esto hace que la animación visual se congele fuera de cámara, 
        // pero la lógica (transiciones, estados) siga funcionando.
        if(anim) anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    void Start()
    {
        // Variación aleatoria de velocidad para que no parezcan robots
        walkSpeed += Random.Range(-0.5f, 0.5f);
        runSpeed += Random.Range(-0.5f, 0.5f);

        GameObject homeObj = GameObject.Find("protectHome");
        if (homeObj) homePoint = homeObj.transform;

        if (GameManager.Instance != null)
            isUnhappy = GameManager.Instance.enemiesAreUnhappy;

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        if (Random.value > 0.5f) Flip();
    }

    void Update()
    {
        if (isDead) return;

        CheckGround();
        CheckGlobalState();

        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;

        // IA: Piensa cada X tiempo (si no está atacando ni cayendo en emboscada)
        if (Time.time >= nextDecisionTime && !isWaitingAtEdge && currentState != State.Attack && currentState != State.AmbushFall)
        {
            Think();
            nextDecisionTime = Time.time + decisionInterval;
        }

        HandleEnvironmentAnimations();
    }

    void FixedUpdate()
    {
        if (isDead) return;
        ExecuteMovement();
    }

    // ====================================================
    // 1. SISTEMA DE MOVIMIENTO AVANZADO
    // ====================================================
    void ExecuteMovement()
    {
        // Estados estáticos (Esperando, Atacando o "Listo para saltar")
        if (isWaitingAtEdge || currentState == State.Attack || (currentState == State.Flank && isInAmbushPosition))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        // Estado Especial: CAÍDA DE EMBOSCADA
        // Permite un ligero control aéreo para asegurar que caiga sobre el jugador
        if (currentState == State.AmbushFall)
        {
             if (GameManager.Instance.playerTransform != null)
             {
                 float dir = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x);
                 rb.linearVelocity = new Vector2(dir * (runSpeed * 0.5f), rb.linearVelocity.y);
             }
             return;
        }

        // Lógica Normal
        switch (currentState)
        {
            case State.Patrol:
                HandlePatrolMovement();
                break;

            case State.Chase:
                if (GameManager.Instance.playerTransform != null)
                    MoveSmart(GameManager.Instance.playerTransform.position, runSpeed);
                break;

            case State.Flee:
                if (GameManager.Instance.playerTransform != null)
                {
                    float dir = Mathf.Sign(transform.position.x - GameManager.Instance.playerTransform.position.x);
                    rb.linearVelocity = new Vector2(dir * runSpeed * 1.2f, rb.linearVelocity.y);
                    CheckFlip(dir);
                }
                break;

            case State.Flank:
                if (currentWeakSpot != null)
                {
                    // Si estamos muy cerca del punto de emboscada, paramos y esperamos
                    if (Vector2.Distance(transform.position, currentWeakSpot.position) < 0.5f)
                    {
                        isInAmbushPosition = true;
                        rb.linearVelocity = Vector2.zero;
                        FacePlayer(); // Mirar hacia donde aparecerá el jugador
                    }
                    else
                    {
                        MoveSmart(currentWeakSpot.position, runSpeed);
                    }
                }
                break;

            case State.Defend:
                if (homePoint != null) MoveSmart(homePoint.position, runSpeed);
                break;
        }
    }

    // --- CEREBRO DE NAVEGACIÓN (Platformer AI) ---
    void MoveSmart(Vector2 targetPos, float speed)
    {
        float xDist = targetPos.x - transform.position.x;
        float yDist = targetPos.y - transform.position.y;
        float dir = Mathf.Sign(xDist);

        // A) Movimiento Horizontal
        if (Mathf.Abs(xDist) > 0.2f)
        {
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            CheckFlip(dir);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // B) Lógica Vertical (Saltar o Bajar)
        bool wallAhead = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer);
        
        if (isGrounded && jumpCooldown <= 0)
        {
            // 1. SALTAR: Si hay pared bloqueando O el objetivo está alto
            // (yDist > 1.5f significa que el objetivo está más de un bloque arriba)
            if ((wallAhead && wallCheck.gameObject != gameObject) || (yDist > 1.5f && Mathf.Abs(xDist) < 3f))
            {
                Jump();
            }
            // 2. BAJAR: Si el objetivo está DEBAJO y estamos sobre una plataforma
            // (yDist < -1.5f significa que el objetivo está abajo)
            else if (yDist < -1.5f && Mathf.Abs(xDist) < 1.5f)
            {
                StartCoroutine(DisableCollisionRoutine());
            }
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpCooldown = 0.5f; // Evitar saltos dobles accidentales
        isGrounded = false;
        anim.SetBool("Grounded", false);
    }

    void HandlePatrolMovement()
    {
        Collider2D wallHit = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer);
        bool floorHit = Physics2D.OverlapCircle(edgeCheck.position, edgeCheckRadius, groundLayer);
        
        // Fix: Ignorarnos a nosotros mismos
        bool hitRealWall = wallHit != null && wallHit.gameObject != gameObject;

        if (hitRealWall || !floorHit)
        {
            if (!isWaitingAtEdge) StartCoroutine(WaitAndTurnRoutine());
        }
        else
        {
            float direction = isFacingRight ? 1 : -1;
            rb.linearVelocity = new Vector2(direction * walkSpeed, rb.linearVelocity.y);
        }
    }

    // --- CORRUTINAS DE ACCIÓN ---

    IEnumerator WaitAndTurnRoutine()
    {
        isWaitingAtEdge = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
        Flip();
        isWaitingAtEdge = false;
    }

    // Permite "atravesar" el suelo temporalmente para bajar pisos
    IEnumerator DisableCollisionRoutine()
    {
        // Detectamos el suelo justo debajo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
        if (hit.collider != null)
        {
            Physics2D.IgnoreCollision(myCollider, hit.collider, true);
            yield return new WaitForSeconds(0.4f); // Tiempo suficiente para caer
            Physics2D.IgnoreCollision(myCollider, hit.collider, false);
        }
    }

    // ====================================================
    // 2. CEREBRO (DECISIONES)
    // ====================================================
    void Think()
    {
        if (GameManager.Instance.playerTransform == null) return;
        float distToPlayer = Vector2.Distance(transform.position, GameManager.Instance.playerTransform.position);

        // 1. ATACAR (Máxima prioridad)
        if (isUnhappy && distToPlayer <= attackRange)
        {
            ChangeState(State.Attack);
            return;
        }

        // 2. MANTENER FLANQUEO (Si ya estamos yendo a una emboscada)
        if (currentState == State.Flank && currentWeakSpot != null)
        {
             // Si el jugador nos pilla in fraganti (muy cerca), cancelamos y peleamos
             if (distToPlayer < 3f && !isInAmbushPosition) 
             {
                 ReleaseCurrentSpot();
                 ChangeState(State.Chase);
             }
             return;
        }

        // 3. COMPORTAMIENTO GENERAL
        if (isUnhappy)
        {
            // Probabilidad de intentar buscar un WeakSpot para emboscar
            if (currentState != State.Flank && Random.Range(0, 100) < flankChance)
            {
                Transform spot = FindBestWeakSpot();
                if (spot != null && GameManager.Instance.TryClaimWeakSpot(spot))
                {
                    currentWeakSpot = spot;
                    ChangeState(State.Flank);
                    return;
                }
            }

            // Si no flanqueamos, perseguimos o patrullamos
            if (distToPlayer < detectionRange) ChangeState(State.Chase);
            else ChangeState(State.Patrol);
        }
        else
        {
            ChangeState(State.Patrol);
        }
    }

    // ====================================================
    // 3. COMBATE Y EMBOSCADA
    // ====================================================
    
    // Llamado por el script "AttackSpotTrigger"
    public void OrderAmbushAttack()
    {
        if(currentState == State.Flank && isInAmbushPosition) 
        {
            StopAllCoroutines(); 
            isWaitingAtEdge = false;
            
            float dirToPlayer = 0;
            if(GameManager.Instance.playerTransform != null)
                 dirToPlayer = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x);
            
            // Empujón agresivo hacia abajo y hacia el jugador
            rb.linearVelocity = new Vector2(dirToPlayer * runSpeed * 1.5f, -6f); 
            
            ChangeState(State.AmbushFall); // Estado de caída libre
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Detectar si tocamos suelo
        if (groundLayer == (groundLayer | (1 << collision.gameObject.layer)))
        {
            isGrounded = true;
            // SI ESTÁBAMOS CAYENDO EN EMBOSCADA -> ATAQUE INSTANTÁNEO
            if (currentState == State.AmbushFall) StartCoroutine(AttackRoutine());
        }
        
        // Daño al jugador por contacto
        if (collision.gameObject.CompareTag("Player") && !isDead) {
            // GameManager.Instance.DamagePlayer(damageToPlayer);
            
            // Rebote físico simple
            float dir = Mathf.Sign(transform.position.x - collision.transform.position.x);
            rb.AddForce(new Vector2(dir * 3, 2), ForceMode2D.Impulse);
        }
    }

    IEnumerator AttackRoutine()
    {
        currentState = State.Attack;
        rb.linearVelocity = Vector2.zero; // Frenar en seco
        
        anim.SetTrigger("Gas");
        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxAttackIdx);

        yield return new WaitForSeconds(0.8f); // Ajustar a la duración de tu clip "Attack"

        if (!isDead)
        {
            ReleaseCurrentSpot();
            ChangeState(State.Chase); // Seguir peleando tras el ataque
        }
    }

    // ====================================================
    // 4. UTILIDADES
    // ====================================================
    void HandleEnvironmentAnimations()
    {
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim.SetBool("Moving", isMoving);
        anim.SetBool("Grounded", isGrounded);
        anim.SetBool("Unhappy", isUnhappy);
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }

    void CheckGround() { isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundLayer); }
    void CheckGlobalState() { if (!isUnhappy && GameManager.Instance.enemiesAreUnhappy) isUnhappy = true; }

    void CheckFlip(float direction)
    {
        if (direction > 0 && !isFacingRight) Flip();
        else if (direction < 0 && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    void FacePlayer()
    {
        if(GameManager.Instance.playerTransform != null)
        {
            float dir = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x);
            CheckFlip(dir);
        }
    }

    Transform FindBestWeakSpot()
    {
        GameObject[] spots = GameObject.FindGameObjectsWithTag("WeakSpot");
        if (spots == null) return null;
        
        Transform best = null; 
        float minDist = 50f;
        
        foreach (GameObject go in spots) {
            float d = Vector2.Distance(transform.position, go.transform.position);
            // Priorizamos spots que estén ARRIBA (y > position.y)
            if (d < minDist && go.transform.position.y >= transform.position.y - 0.5f) { 
                minDist = d; 
                best = go.transform; 
            }
        }
        return best;
    }

    void ReleaseCurrentSpot()
    {
        if (currentWeakSpot != null)
        {
            GameManager.Instance.ReleaseWeakSpot(currentWeakSpot);
            currentWeakSpot = null;
        }
        isInAmbushPosition = false;
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        health -= damage;
        anim.SetTrigger("Hit");
        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxHitIdx);
        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Death");
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        GetComponent<Collider2D>().enabled = false;
        ReleaseCurrentSpot();
        GameManager.Instance.NotifyEnemyDeath();
        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxDeathIdx);
        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmos()
    {
        if (wallCheck) { Gizmos.color = Color.red; Gizmos.DrawWireCube(wallCheck.position, new Vector3(wallCheckSize.x, wallCheckSize.y, 1)); }
        if (edgeCheck) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(edgeCheck.position, edgeCheckRadius); }
    }
}