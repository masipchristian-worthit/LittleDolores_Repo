using System.Collections;
using UnityEngine;

public class GShroomEnemy : MonoBehaviour
{
    [Header("ESTADÍSTICAS")]
    [SerializeField] int health = 2;
    [SerializeField] int damageToPlayer = 1;
    
    [Header("COMPORTAMIENTO DE MOVIMIENTO")]
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float runSpeed = 4.5f;
    [SerializeField] float jumpForce = 8.5f; // Aumentado ligeramente para mejor escalada
    [SerializeField] float maxJumpHeight = 4.0f; // Altura máxima estimada que puede saltar
    [SerializeField] float platformSearchRadius = 10f; // Radio para buscar plataformas voladoras

    [Header("IA DE COMBATE (BAITING)")]
    [SerializeField] float baitDistance = 3.5f;
    [SerializeField] float aggroInterval = 3.0f; 

    [Header("PROBABILIDADES")]
    [Range(0, 100)] public int flankChance = 30;

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

    // REFERENCIAS
    Rigidbody2D rb;
    Animator anim;
    Collider2D myCollider;
    Transform currentWeakSpot;
    Transform homePoint;
    PlayerController2D playerScript; 

    // ESTADOS
    enum State { Patrol, Chase, Attack, Flee, Flank, Defend, AmbushFall }
    [Header("DEBUG ESTADO")]
    [SerializeField] State currentState = State.Patrol;

    // FLAGS & TIMERS
    bool isFacingRight = true;
    bool isGrounded;
    bool isDead;
    bool isUnhappy;
    bool isWaitingAtEdge;
    bool isInAmbushPosition;
    bool isAggressive = false; 
    float nextDecisionTime;

    [Header("TIMERS")]
    public float jumpCooldown = 0f;
    public float aggroTimer = 0f;
    public float evasionCooldown = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        
        if (anim) anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    void Start()
    {
        walkSpeed += Random.Range(-0.5f, 0.5f);
        runSpeed += Random.Range(-0.5f, 0.5f);

        GameObject homeObj = GameObject.Find("protectHome");
        if (homeObj) homePoint = homeObj.transform;

        if (GameManager.Instance != null)
        {
            isUnhappy = GameManager.Instance.enemiesAreUnhappy;
            GameManager.Instance.RegisterEnemy(this);
            if (GameManager.Instance.playerTransform != null)
                playerScript = GameManager.Instance.playerTransform.GetComponent<PlayerController2D>();
        }

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        if (Random.value > 0.5f) Flip();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy(this);
    }

    void Update()
    {
        if (isDead) return;

        CheckGround();
        CheckGlobalState();

        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        if (evasionCooldown > 0) evasionCooldown -= Time.deltaTime;

        if (Time.time >= nextDecisionTime && !isWaitingAtEdge && currentState != State.Attack && currentState != State.AmbushFall)
        {
            Think();
            nextDecisionTime = Time.time + 0.2f; 
        }

        HandleEnvironmentAnimations();
    }

    void FixedUpdate()
    {
        if (isDead) return;
        ExecuteMovement();
    }

    // ====================================================
    // LÓGICA DE MOVIMIENTO PRINCIPAL
    // ====================================================
    void ExecuteMovement()
    {
        // Estados estáticos
        if (isWaitingAtEdge || currentState == State.Attack || (currentState == State.Flank && isInAmbushPosition))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Estado: Caída de Emboscada
        if (currentState == State.AmbushFall)
        {
             if (GameManager.Instance.playerTransform != null)
             {
                 float dir = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x);
                 rb.linearVelocity = new Vector2(dir * (runSpeed * 0.8f), rb.linearVelocity.y);
             }
             return;
        }

        switch (currentState)
        {
            case State.Patrol:
                HandlePatrolMovement();
                break;

            case State.Chase:
                if (GameManager.Instance.playerTransform != null)
                {
                    BaitAndChaseLogic();
                }
                break;

            case State.Flee:
                if (GameManager.Instance.playerTransform != null)
                {
                    float dir = Mathf.Sign(transform.position.x - GameManager.Instance.playerTransform.position.x);
                    // Huir ignorando bordes
                    MoveSmartTarget(transform.position + Vector3.right * dir * 5f, runSpeed * 1.2f, true); 
                }
                break;

            case State.Flank:
                if (currentWeakSpot != null)
                {
                    // Comprobar si hemos llegado
                    if (Vector2.Distance(transform.position, currentWeakSpot.position) < 0.5f)
                    {
                        isInAmbushPosition = true;
                        rb.linearVelocity = Vector2.zero;
                        FacePlayer();
                    }
                    else 
                    {
                        // === LÓGICA DE PATHFINDING DE PLATAFORMAS ===
                        Vector2 target = currentWeakSpot.position;
                        
                        // Si el objetivo está muy alto, buscamos una plataforma intermedia
                        if (target.y > transform.position.y + maxJumpHeight)
                        {
                            Collider2D nextPlat = FindBestPlatformStep(target);
                            if (nextPlat != null)
                            {
                                // Calculamos un punto objetivo sobre esa plataforma.
                                // Intentamos alinear X con el objetivo final, pero dentro de los límites de la plataforma encontrada.
                                float targetX = Mathf.Clamp(target.x, nextPlat.bounds.min.x, nextPlat.bounds.max.x);
                                
                                // Apuntamos a la parte superior de la plataforma
                                target = new Vector2(targetX, nextPlat.bounds.max.y + 0.1f);
                            }
                        }

                        // Moverse hacia el objetivo calculado (final o intermedio)
                        MoveSmartTarget(target, runSpeed, false);
                    }
                }
                break;
                
            case State.Defend:
                 if (homePoint != null) 
                 {
                     // Defend ignora precipicios para llegar a casa rápido
                     MoveSmartTarget(homePoint.position, runSpeed, true);
                 }
                 break;
        }
    }

    // --- LÓGICA DE PERSECUCIÓN Y EVASIÓN ---
    void BaitAndChaseLogic()
    {
        Vector2 playerPos = GameManager.Instance.playerTransform.position;
        float dist = Vector2.Distance(transform.position, playerPos);
        
        aggroTimer += Time.deltaTime;
        bool playerIsAttacking = playerScript != null && playerScript.IsAttacking;
        
        // Decidir si ponerse agresivo
        if (!isAggressive)
        {
            if (aggroTimer > aggroInterval || dist < attackRange || (!playerIsAttacking && dist < baitDistance && aggroTimer > 1f))
            {
                isAggressive = true;
            }
        }

        if (isAggressive)
        {
            // EVASIÓN: 1 de cada 3 veces, si está cerca, salta hacia atrás
            if (isGrounded && dist < 2.5f && evasionCooldown <= 0)
            {
                if (Random.Range(0, 3) == 0) // 33% probabilidad
                {
                    float dirToPlayer = Mathf.Sign(playerPos.x - transform.position.x);
                    rb.linearVelocity = new Vector2(-dirToPlayer * runSpeed, jumpForce * 0.8f);
                    CheckFlip(-dirToPlayer);
                    evasionCooldown = 2.0f;
                    return; 
                }
            }

            // Persecución normal
            MoveSmartTarget(playerPos, runSpeed * 1.2f, false);
            
            if (dist <= attackRange)
            {
                StartCoroutine(AttackRoutine());
                isAggressive = false;
                aggroTimer = 0f;
            }
        }
        else
        {
            // Modo Bait (Mantener distancia)
            float dir = Mathf.Sign(playerPos.x - transform.position.x);
            
            if (dist > baitDistance + 1f) MoveSmartTarget(playerPos, runSpeed * 0.7f, false);
            else if (dist < baitDistance - 1f)
            {
                // Retirada
                rb.linearVelocity = new Vector2(-dir * runSpeed, rb.linearVelocity.y);
                CheckFlip(-dir);
                
                // Si nos acorralan, atacar
                if (Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer)) isAggressive = true;
            }
            else
            {
                // Idle tenso
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                FacePlayer();
            }
        }
    }

    // ====================================================
    // PATHFINDING & NAVEGACIÓN
    // ====================================================
    
    // Busca la mejor plataforma intermedia para "escalar" hacia un objetivo alto
    Collider2D FindBestPlatformStep(Vector2 finalTarget)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, platformSearchRadius, groundLayer);
        Collider2D best = null;
        float minDistance = Mathf.Infinity;
        
        // Detectar suelo actual para ignorarlo
        RaycastHit2D groundInfo = Physics2D.Raycast(transform.position, Vector2.down, 1.0f, groundLayer);
        Collider2D currentGround = groundInfo.collider;

        foreach (var hit in hits)
        {
            if (hit == currentGround) continue; // No saltar sobre lo que ya piso
            if (hit.gameObject == gameObject) continue; // Ignorarme a mí mismo

            // Altura relativa: Debe estar por encima de mis pies y al alcance de mi salto
            float heightDiff = hit.bounds.max.y - transform.position.y;

            // Filtro: Plataforma más alta que yo, pero alcanzable
            if (heightDiff > 0.5f && heightDiff <= maxJumpHeight)
            {
                // Puntuación: Cuánto me acerca esta plataforma al objetivo final
                // Usamos el punto de la plataforma más cercano al objetivo final para calcular la distancia
                float dist = Vector2.Distance(hit.bounds.ClosestPoint(finalTarget), finalTarget);
                
                if (dist < minDistance)
                {
                    minDistance = dist;
                    best = hit;
                }
            }
        }
        return best;
    }

    void MoveSmartTarget(Vector2 targetPos, float speed, bool ignoreLedges)
    {
        float xDist = targetPos.x - transform.position.x;
        float yDist = targetPos.y - transform.position.y;
        float dir = Mathf.Sign(xDist);

        // Movimiento Horizontal
        if (Mathf.Abs(xDist) > 0.2f)
        {
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            CheckFlip(dir);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // --- LÓGICA DE SALTOS (Paredes y Plataformas Voladoras) ---
        Collider2D wallHit = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer);
        bool wallAhead = wallHit != null && !wallHit.CompareTag("Player") && wallHit.gameObject != gameObject;
        bool ledgeAhead = !Physics2D.OverlapCircle(edgeCheck.position, edgeCheckRadius, groundLayer);

        if (isGrounded && jumpCooldown <= 0)
        {
            // A) SALTO POR OBSTÁCULO (PARED)
            if (wallAhead)
            {
                Jump();
            }
            // B) SALTO POR OBJETIVO ALTO (Plataforma Voladora o Escalada)
            // Si el objetivo está arriba (> 1.0f) y NO hay pared, pero estamos alineados en X (debajo de la plataforma)
            else if (yDist > 1.0f)
            {
                bool isAlignedUnderTarget = Mathf.Abs(xDist) < 0.8f;
                
                // Saltamos si hay precipicio (para cruzar) O si hay pared O si estamos justo debajo del objetivo
                if (ledgeAhead || wallAhead || isAlignedUnderTarget) 
                {
                    Jump();
                }
            }
            // C) BAJAR PLATAFORMA (Atravesar hacia abajo)
            else if (yDist < -1.5f && Mathf.Abs(xDist) < 1.5f)
            {
                StartCoroutine(DisableCollisionRoutine());
            }

            // --- PROTECCIÓN DE CAÍDAS (Frenar en borde) ---
            if (ledgeAhead && !ignoreLedges && yDist < 0.5f) // Solo frena si el objetivo no está abajo
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void HandlePatrolMovement()
    {
        Collider2D wallHit = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer);
        bool floorHit = Physics2D.OverlapCircle(edgeCheck.position, edgeCheckRadius, groundLayer);
        bool hitRealWall = wallHit != null && !wallHit.CompareTag("Player") && wallHit.gameObject != gameObject;

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

    // ====================================================
    // COLISIONES & COMBATE
    // ====================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (groundLayer == (groundLayer | (1 << collision.gameObject.layer)))
        {
            isGrounded = true;
            if (currentState == State.AmbushFall) StartCoroutine(AttackRoutine());
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            bool playerStompsEnemy = false;
            bool enemyStompsPlayer = false;

            // Definición de hitboxes físicas para el pisotón
            float myTop = myCollider.bounds.max.y;
            float myHeight = myCollider.bounds.size.y;
            float myThreshold = myCollider.bounds.min.y + (myHeight * 0.7f); // 70% altura

            Collider2D playerCol = collision.collider;
            float playerThreshold = playerCol.bounds.min.y + (playerCol.bounds.size.y * 0.75f); // 75% altura player

            foreach (ContactPoint2D point in collision.contacts)
            {
                if (point.point.y > myThreshold && point.normal.y < -0.5f) { playerStompsEnemy = true; break; }
                if (transform.position.y > playerCol.transform.position.y && point.point.y > playerThreshold) { enemyStompsPlayer = true; }
            }

            if (playerStompsEnemy) 
            { 
                Die(); 
            }
            else if (enemyStompsPlayer) 
            {
                // DAÑO SOLO SI CAE ENCIMA (EMBOSCADA)
                GameManager.Instance.TakeDamage(damageToPlayer);
                StartCoroutine(AttackRoutine());
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 5f); // Rebote
            }
            else
            {
                // CHOQUE LATERAL: NO DAÑO, EMPUJE Y AGRESIVIDAD
                float dir = Mathf.Sign(transform.position.x - collision.transform.position.x);
                rb.AddForce(new Vector2(dir * 5, 2), ForceMode2D.Impulse);
                isAggressive = true;
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        currentState = State.Attack;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Gas");
        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxAttackIdx);
        
        yield return new WaitForSeconds(1.0f);
        
        if (!isDead)
        {
            ReleaseCurrentSpot();
            ChangeState(State.Chase);
            isAggressive = false; 
            aggroTimer = 0f;
        }
    }

    // ====================================================
    // UTILIDADES
    // ====================================================
    public void OrderAmbushAttack() 
    { 
        if(currentState == State.Flank && isInAmbushPosition) 
        { 
            StopAllCoroutines(); 
            isWaitingAtEdge = false; 
            float dir = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x); 
            rb.linearVelocity = new Vector2(dir * runSpeed * 1.5f, -6f); 
            ChangeState(State.AmbushFall); 
        } 
    }

    void Jump() 
    { 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); 
        jumpCooldown = 0.5f; 
        isGrounded = false; 
        anim.SetBool("Grounded", false); 
    }

    IEnumerator WaitAndTurnRoutine() 
    { 
        isWaitingAtEdge = true; 
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f)); 
        Flip(); 
        isWaitingAtEdge = false; 
    }

    IEnumerator DisableCollisionRoutine() 
    { 
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer); 
        if (hit.collider != null) 
        { 
            Physics2D.IgnoreCollision(myCollider, hit.collider, true); 
            yield return new WaitForSeconds(0.4f); 
            Physics2D.IgnoreCollision(myCollider, hit.collider, false); 
        } 
    }

    void CheckGround() { isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundLayer); }
    void CheckGlobalState() { if (!isUnhappy && GameManager.Instance.enemiesAreUnhappy) isUnhappy = true; }
    void CheckFlip(float direction) { if (direction > 0 && !isFacingRight) Flip(); else if (direction < 0 && isFacingRight) Flip(); }
    void Flip() { isFacingRight = !isFacingRight; Vector3 scale = transform.localScale; scale.x *= -1; transform.localScale = scale; }
    void FacePlayer() { if(GameManager.Instance.playerTransform != null) { float dir = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x); CheckFlip(dir); } }
    
    Transform FindBestWeakSpot() 
    { 
        GameObject[] spots = GameObject.FindGameObjectsWithTag("WeakSpot"); 
        if (spots == null) return null; 
        Transform best = null; 
        float minDist = 50f; 
        foreach (GameObject go in spots) { 
            float d = Vector2.Distance(transform.position, go.transform.position); 
            // Buscar spots altos
            if (d < minDist && go.transform.position.y >= transform.position.y - 0.5f) { 
                minDist = d; 
                best = go.transform; 
            } 
        } 
        return best; 
    }

    void ReleaseCurrentSpot() { if (currentWeakSpot != null) { GameManager.Instance.ReleaseWeakSpot(currentWeakSpot); currentWeakSpot = null; } isInAmbushPosition = false; }
    void ChangeState(State newState) { if (currentState == newState) return; currentState = newState; }
    void HandleEnvironmentAnimations() { bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f; anim.SetBool("Moving", isMoving); anim.SetBool("Grounded", isGrounded); anim.SetBool("Unhappy", isUnhappy); }
    public void TakeDamage(int damage) { if (isDead) return; health -= damage; anim.SetTrigger("Hit"); if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxHitIdx); if (health <= 0) Die(); }
    
    void Die() 
    { 
        if (isDead) return; 
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

    void Think() 
    { 
        if (GameManager.Instance.playerTransform == null) return; 
        float dist = Vector2.Distance(transform.position, GameManager.Instance.playerTransform.position); 
        
        if (isUnhappy && dist <= attackRange) { ChangeState(State.Attack); return; } 
        
        if (currentState == State.Flank && currentWeakSpot != null) { 
            if (dist < 3f && !isInAmbushPosition) { ReleaseCurrentSpot(); ChangeState(State.Chase); } 
            return; 
        } 
        
        if (isUnhappy) { 
            if (currentState != State.Flank && Random.Range(0, 100) < flankChance) { 
                Transform spot = FindBestWeakSpot(); 
                if (spot != null && GameManager.Instance.TryClaimWeakSpot(spot)) { 
                    currentWeakSpot = spot; 
                    ChangeState(State.Flank); 
                    return; 
                } 
            } 
            if (dist < detectionRange) ChangeState(State.Chase); 
            else ChangeState(State.Patrol); 
        } else { 
            ChangeState(State.Patrol); 
        } 
    }

    private void OnDrawGizmos() 
    { 
        if (wallCheck) { Gizmos.color = Color.red; Gizmos.DrawWireCube(wallCheck.position, new Vector3(wallCheckSize.x, wallCheckSize.y, 1)); } 
        if (edgeCheck) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(edgeCheck.position, edgeCheckRadius); }
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, platformSearchRadius);
    }
}