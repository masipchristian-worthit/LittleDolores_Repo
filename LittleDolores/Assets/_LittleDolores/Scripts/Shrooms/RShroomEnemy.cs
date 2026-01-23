using System.Collections;
using UnityEngine;

public class RShroomEnemy : MonoBehaviour
{
    [Header("ESTADÍSTICAS (ROJO)")]
    [SerializeField] public int health = 3; 
    [SerializeField] int damageToPlayer = 2; 

    [Header("MOVIMIENTO")]
    [SerializeField] float walkSpeed = 3.5f; 
    [SerializeField] float runSpeed = 5.5f;
    [SerializeField] float jumpForce = 9.0f;
    [SerializeField] float maxJumpHeight = 4.0f;
    [SerializeField] float platformSearchRadius = 10f; // Restaurado

    [Header("IA DE COMBATE (AGRESIVA)")]
    [SerializeField] float baitDistance = 2.5f; 
    [SerializeField] float aggroInterval = 1.0f; 

    [Header("PROBABILIDADES")]
    [Range(0, 100)] public int flankChance = 50; 

    [Header("SENSORES")]
    [SerializeField] float detectionRange = 10f;
    [SerializeField] float attackRange = 1.5f;
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
    bool wasGroundedLastFrame;
    bool isDead;
    bool isUnhappy;
    bool isWaitingAtEdge;
    bool isInAmbushPosition;
    bool isAggressive = false;
    bool isBeingHit = false;
    bool isLanding = false;
    bool isJumping => rb.linearVelocity.y > 0.1f && !isGrounded;
    bool isFalling => rb.linearVelocity.y < -0.1f && !isGrounded && !isJumping;
    bool isMoving => Mathf.Abs(rb.linearVelocity.x) > 0.1f;

    float nextDecisionTime;
    float stuckCheckTimer;

    [Header("TIMERS (DEBUG)")]
    public float jumpCooldown = 0f;
    public float aggroTimer = 0f;
    public float evasionCooldown = 0f;
    
    [Header("ANIMATION DEBUG")]
    public bool debugMoving;
    public bool debugGrounded;
    public bool debugJumping;
    public bool debugFalling;
    public bool debugLanding;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();

        if (anim != null)
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    void Start()
    {
        walkSpeed += Random.Range(-0.5f, 0.5f);
        runSpeed += Random.Range(-0.5f, 0.5f);

        GameObject homeObj = GameObject.Find("protectHome");
        if (homeObj != null) homePoint = homeObj.transform;

        if (GameManager.Instance != null)
        {
            isUnhappy = GameManager.Instance.enemiesAreUnhappy;
            // GameManager.Instance.RegisterEnemy(this); // Descomentar si usas registro
            
            if (GameManager.Instance.playerTransform != null)
                playerScript = GameManager.Instance.playerTransform.GetComponent<PlayerController2D>();
        }

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        if (Random.value > 0.5f) Flip();
    }

    void OnDestroy()
    {
        // GameManager.Instance.UnregisterEnemy(this);
    }

    void Update()
    {
        if (isDead) return;

        wasGroundedLastFrame = isGrounded;
        CheckGround();
        
        if (!wasGroundedLastFrame && isGrounded && !isLanding && !isDead && !isBeingHit)
        {
            StartCoroutine(LandingRoutine());
        }

        CheckGlobalState();

        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        if (evasionCooldown > 0) evasionCooldown -= Time.deltaTime;

        bool canThink = !isWaitingAtEdge && !isBeingHit && !isLanding && currentState != State.Attack && currentState != State.AmbushFall;
        
        if (Time.time >= nextDecisionTime && canThink)
        {
            Think();
            nextDecisionTime = Time.time + 0.2f;
        }

        AnimationManager();
        
        if (!isBeingHit && !isLanding) CheckIfStuck();
    }

    void FixedUpdate()
    {
        if (isDead || isBeingHit || isLanding) return;
        ExecuteMovement();
    }

    // ====================================================
    // ANIMACIONES
    // ====================================================
    void AnimationManager()
    {
        if (anim == null) return;

        anim.SetBool("Unhappy", isUnhappy);
        if (isDead || isBeingHit || isLanding) return;

        anim.SetBool("Grounded", isGrounded);
        anim.SetBool("Moving", isMoving);
        anim.SetBool("Jump", isJumping);
        anim.SetBool("Falling", isFalling);

        debugMoving = isMoving;
        debugGrounded = isGrounded;
        debugJumping = isJumping;
        debugFalling = isFalling;
        debugLanding = isLanding;
    }

    IEnumerator LandingRoutine()
    {
        isLanding = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        anim.SetTrigger("Landing");
        yield return new WaitForSeconds(0.3f);
        isLanding = false;
        jumpCooldown = 0f;
    }

    // ====================================================
    // COLISIONES
    // ====================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (groundLayer == (groundLayer | (1 << collision.gameObject.layer)))
        {
            isGrounded = true;
            if (currentState == State.AmbushFall)
            {
                StartCoroutine(SpikesAttackRoutine());
            }
        }

        if (collision.gameObject.GetComponent<RShroomEnemy>() != null || 
            collision.gameObject.GetComponent<GShroomEnemy>() != null)
        {
            Flip();
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            bool playerStompsEnemy = false;
            bool enemyStompsPlayer = false;

            float myThreshold = myCollider.bounds.min.y + (myCollider.bounds.size.y * 0.7f);
            Collider2D playerCol = collision.collider;
            float playerThreshold = playerCol.bounds.min.y + (playerCol.bounds.size.y * 0.75f);

            foreach (ContactPoint2D point in collision.contacts)
            {
                if (point.point.y > myThreshold && point.normal.y < -0.5f)
                {
                    playerStompsEnemy = true;
                    break;
                }
                if (transform.position.y > playerCol.transform.position.y && point.point.y > playerThreshold)
                {
                    enemyStompsPlayer = true;
                }
            }

            if (playerStompsEnemy)
            {
                Die();
            }
            else if (enemyStompsPlayer)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TakeDamage(damageToPlayer);
                
                StartCoroutine(SpikesAttackRoutine());
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 5f);
            }
            else
            {
                float dir = Mathf.Sign(transform.position.x - collision.transform.position.x);
                rb.AddForce(new Vector2(dir * 5, 2), ForceMode2D.Impulse);
                isAggressive = true;
            }
        }
    }

    // ====================================================
    // MOVIMIENTO (LÓGICA COMPLETA RESTAURADA)
    // ====================================================
    void ExecuteMovement()
    {
        bool isBusy = isWaitingAtEdge || currentState == State.Attack || (currentState == State.Flank && isInAmbushPosition);
        
        if (isBusy)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

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
                if (GameManager.Instance.playerTransform != null) BaitAndChaseLogic();
                break;
            case State.Flee:
                if (GameManager.Instance.playerTransform != null)
                {
                    float dir = Mathf.Sign(transform.position.x - GameManager.Instance.playerTransform.position.x);
                    MoveSmartTarget(transform.position + Vector3.right * dir * 5f, runSpeed * 1.2f, true);
                }
                break;
            case State.Flank:
                // LÓGICA DE FLANQUEO COMPLEJA (RESTAURADA)
                if (currentWeakSpot != null)
                {
                    if (Vector2.Distance(transform.position, currentWeakSpot.position) < 0.5f)
                    {
                        isInAmbushPosition = true;
                        rb.linearVelocity = Vector2.zero;
                        FacePlayer();
                    }
                    else
                    {
                        Vector2 target = currentWeakSpot.position;
                        // Cálculo de plataforma intermedia
                        if (target.y > transform.position.y + maxJumpHeight)
                        {
                            Collider2D nextPlat = FindBestPlatformStep(target);
                            if (nextPlat != null)
                            {
                                float targetX = Mathf.Clamp(target.x, nextPlat.bounds.min.x, nextPlat.bounds.max.x);
                                target = new Vector2(targetX, nextPlat.bounds.max.y + 0.1f);
                            }
                        }
                        MoveSmartTarget(target, runSpeed, false);
                    }
                }
                break;
            case State.Defend:
                if (homePoint != null) MoveSmartTarget(homePoint.position, runSpeed, true);
                break;
        }
    }

    void BaitAndChaseLogic()
    {
        if (GameManager.Instance.playerTransform == null) return;
        Vector2 playerPos = GameManager.Instance.playerTransform.position;
        float dist = Vector2.Distance(transform.position, playerPos);
        aggroTimer += Time.deltaTime;

        bool playerIsAttacking = (playerScript != null && playerScript.IsAttacking);

        if (!isAggressive)
        {
            bool timeOut = aggroTimer > aggroInterval;
            bool tooClose = dist < attackRange;
            bool tacticalOpportunity = !playerIsAttacking && dist < baitDistance; 

            if (timeOut || tooClose || tacticalOpportunity)
            {
                isAggressive = true;
            }
        }

        if (isAggressive)
        {
            MoveSmartTarget(playerPos, runSpeed * 1.2f, false);

            if (dist <= attackRange)
            {
                StartCoroutine(SpikesAttackRoutine());
                isAggressive = false;
                aggroTimer = 0f;
            }
        }
        else
        {
            float dir = Mathf.Sign(playerPos.x - transform.position.x);
            if (dist > baitDistance + 1f)
            {
                MoveSmartTarget(playerPos, runSpeed * 0.8f, false);
            }
            else if (dist < baitDistance - 1f)
            {
                rb.linearVelocity = new Vector2(-dir * runSpeed, rb.linearVelocity.y);
                CheckFlip(-dir);
                if (Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer)) isAggressive = true;
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                FacePlayer();
            }
        }
    }

    void MoveSmartTarget(Vector2 targetPos, float speed, bool ignoreLedges)
    {
        float xDist = targetPos.x - transform.position.x;
        float yDist = targetPos.y - transform.position.y;
        float dir = Mathf.Sign(xDist);

        if (Mathf.Abs(xDist) > 0.2f)
        {
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            CheckFlip(dir);
        }
        else if (Mathf.Abs(yDist) < 1.0f) 
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        bool wallAhead = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer);
        bool ledgeAhead = !Physics2D.OverlapCircle(edgeCheck.position, edgeCheckRadius, groundLayer);

        if (isGrounded && jumpCooldown <= 0 && !isLanding)
        {
            if (wallAhead) Jump();
            else if (yDist > 1.0f && (ledgeAhead || wallAhead || Mathf.Abs(xDist) < 0.8f)) Jump();
            else if (yDist < -1.5f && Mathf.Abs(xDist) < 1.5f) StartCoroutine(DisableCollisionRoutine());

            if (ledgeAhead && !ignoreLedges && yDist < 0.5f) rb.linearVelocity = Vector2.zero;
        }
    }

    void HandlePatrolMovement()
    {
        bool wallHit = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0, groundLayer);
        bool floorHit = Physics2D.OverlapCircle(edgeCheck.position, edgeCheckRadius, groundLayer);
        
        if (wallHit || !floorHit)
        {
            if (!isWaitingAtEdge) StartCoroutine(WaitAndTurnRoutine());
        }
        else
        {
            float dir = isFacingRight ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);
        }
    }

    void CheckIfStuck()
    {
        bool shouldBeMoving = currentState == State.Patrol || currentState == State.Chase;
        if (shouldBeMoving && !isWaitingAtEdge && !isAggressive)
        {
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f && isGrounded)
            {
                stuckCheckTimer += Time.deltaTime;
                if (stuckCheckTimer > 0.5f)
                {
                    if (Random.value > 0.5f && !isLanding) Jump(); else Flip();
                    stuckCheckTimer = 0f;
                }
            }
            else stuckCheckTimer = 0f;
        }
    }

    // ====================================================
    // DAÑO Y MUERTE
    // ====================================================
    public void TakeDamage(int damage)
    {
        if (isDead || isBeingHit) return;

        health -= damage;

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitRoutine());
        }
    }

    IEnumerator HitRoutine()
    {
        isBeingHit = true;
        isLanding = false;
        
        rb.linearVelocity = Vector2.zero;
        
        anim.SetBool("Moving", false); 
        anim.SetBool("Jump", false);
        anim.SetBool("Falling", false);
        anim.SetBool("Grounded", true);

        anim.SetTrigger("Hit");
        if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxHitIdx);

        yield return new WaitForSeconds(0.2f); 

        isBeingHit = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        anim.SetBool("Moving", false);
        anim.SetBool("Jump", false);
        anim.SetBool("Falling", false);
        anim.SetBool("Grounded", true);
        anim.SetBool("Unhappy", false);
        anim.SetTrigger("Death");

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        myCollider.enabled = false;

        ReleaseCurrentSpot();
        if (GameManager.Instance != null) GameManager.Instance.NotifyEnemyDeath();
        if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxDeathIdx);

        Destroy(gameObject, 2f);
    }

    // ====================================================
    // ATAQUE (SPIKES)
    // ====================================================
    IEnumerator SpikesAttackRoutine()
    {
        currentState = State.Attack;
        rb.linearVelocity = Vector2.zero;
        
        anim.SetBool("Moving", false);
        anim.SetTrigger("Spikes"); // Cambio a Spikes
        
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxAttackIdx);

        yield return new WaitForSeconds(0.8f); 

        if (!isDead)
        {
            ReleaseCurrentSpot();
            ChangeState(State.Chase);
            isAggressive = false;
            aggroTimer = 0f;
        }
    }

    // ====================================================
    // UTILS & LÓGICA RESTAURADA
    // ====================================================
    
    // ESTA ES LA FUNCIÓN QUE FALTABA (RESTAURADA)
    Collider2D FindBestPlatformStep(Vector2 finalTarget)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, platformSearchRadius, groundLayer);
        Collider2D best = null;
        float minDistance = Mathf.Infinity;

        RaycastHit2D groundInfo = Physics2D.Raycast(transform.position, Vector2.down, 1.0f, groundLayer);
        Collider2D currentGround = groundInfo.collider;

        foreach (var hit in hits)
        {
            if (hit == currentGround) continue;
            if (hit.gameObject == gameObject) continue;

            float heightDiff = hit.bounds.max.y - transform.position.y;
            if (heightDiff > 0.5f && heightDiff <= maxJumpHeight)
            {
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

    // MÉTODO DE EMBOSCADA MANUAL RESTAURADO
    public void OrderAmbushAttack()
    {
        if (currentState == State.Flank && isInAmbushPosition)
        {
            StopAllCoroutines();
            isWaitingAtEdge = false;
            
            float dir = 0;
            if (GameManager.Instance.playerTransform != null)
                dir = Mathf.Sign(GameManager.Instance.playerTransform.position.x - transform.position.x);
            
            rb.linearVelocity = new Vector2(dir * runSpeed * 1.5f, -6f);
            ChangeState(State.AmbushFall);
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpCooldown = 1.0f;
        isGrounded = false;
        isLanding = false;
    }

    IEnumerator WaitAndTurnRoutine()
    {
        isWaitingAtEdge = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        yield return new WaitForSeconds(Random.Range(0.5f, 1.0f)); 
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
    void CheckGlobalState() { if (!isUnhappy && GameManager.Instance != null && GameManager.Instance.enemiesAreUnhappy) isUnhappy = true; }
    void CheckFlip(float direction) { if (direction > 0 && !isFacingRight) Flip(); else if (direction < 0 && isFacingRight) Flip(); }
    void Flip() { isFacingRight = !isFacingRight; Vector3 scale = transform.localScale; scale.x *= -1; transform.localScale = scale; }
    
    void FacePlayer() 
    { 
        if (GameManager.Instance.playerTransform != null) 
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
        foreach (GameObject go in spots)
        {
            float d = Vector2.Distance(transform.position, go.transform.position);
            if (d < minDist && go.transform.position.y >= transform.position.y - 0.5f) { minDist = d; best = go.transform; }
        }
        return best;
    }

    void ReleaseCurrentSpot() { if (currentWeakSpot != null && GameManager.Instance != null) GameManager.Instance.ReleaseWeakSpot(currentWeakSpot); currentWeakSpot = null; isInAmbushPosition = false; }
    void ChangeState(State newState) { if (currentState != newState) currentState = newState; }

    void Think()
    {
        if (GameManager.Instance.playerTransform == null) return;
        float dist = Vector2.Distance(transform.position, GameManager.Instance.playerTransform.position);

        if (isUnhappy && dist <= attackRange) { ChangeState(State.Attack); return; }
        if (currentState == State.Flank && currentWeakSpot != null) { if (dist < 3f && !isInAmbushPosition) { ReleaseCurrentSpot(); ChangeState(State.Chase); } return; }

        if (isUnhappy)
        {
            if (currentState != State.Flank && Random.Range(0, 100) < flankChance)
            {
                Transform spot = FindBestWeakSpot();
                if (spot != null && GameManager.Instance.TryClaimWeakSpot(spot)) { currentWeakSpot = spot; ChangeState(State.Flank); return; }
            }
            ChangeState(dist < detectionRange ? State.Chase : State.Patrol);
        }
        else ChangeState(State.Patrol);
    }

    private void OnDrawGizmos()
    {
        if (wallCheck) { Gizmos.color = Color.red; Gizmos.DrawWireCube(wallCheck.position, new Vector3(wallCheckSize.x, wallCheckSize.y, 1)); }
        if (edgeCheck) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(edgeCheck.position, edgeCheckRadius); }
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, platformSearchRadius);
    }
}