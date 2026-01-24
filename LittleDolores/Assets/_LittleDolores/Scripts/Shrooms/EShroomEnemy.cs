using UnityEngine;
using System.Collections;

public class E_Shroom : MonoBehaviour
{
    [Header("STATS")]
    [SerializeField] public int health = 2;
    [SerializeField] float moveSpeed = 2.5f;

    [Header("DAMAGE")]
    [SerializeField] public int damageToPlayer = 1;

    [Header("ATTACK")]
    [SerializeField] float attackDistance = 8f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform firePoint;

    [Header("CHECKS")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    [SerializeField] Transform wallCheck;
    [SerializeField] float wallCheckRadius = 0.1f;

    [SerializeField] Transform lowerWallCheck;
    [SerializeField] float lowerWallCheckRadius = 0.1f;

    [Header("AUDIO")]
    [SerializeField] int idleSoundIndex = 0;
    [Tooltip("Índice del sonido idle en el sfxLibrary del AudioManager")]
    [SerializeField] float minIdleSoundInterval = 10f;
    [SerializeField] float maxIdleSoundInterval = 20f;

    Rigidbody2D rb;
    Animator anim;
    Transform player;

    float attackTimer;
    float idleSoundTimer;
    bool isDead;
    bool isAttacking;
    bool isFacingRight = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (GameManager.Instance && GameManager.Instance.playerTransform)
            player = GameManager.Instance.playerTransform;

        // Inicializar el timer con un valor aleatorio
        ResetIdleSoundTimer();
    }

    void Update()
    {
        if (!isDead && health <= 0)
            Die();

        if (isDead || player == null) return;

        attackTimer -= Time.deltaTime;

        // Timer para el sonido idle
        idleSoundTimer -= Time.deltaTime;
        if (idleSoundTimer <= 0f)
        {
            PlayIdleSound();
            ResetIdleSoundTimer();
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackDistance && attackTimer <= 0f && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
        else if (!isAttacking)
        {
            Patrol();
        }

        UpdateAnimations();
    }

    void Patrol()
    {
        float dir = isFacingRight ? 1f : -1f;

        bool groundAhead = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool wallAhead = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, groundLayer);
        bool lowerWallAhead = Physics2D.OverlapCircle(lowerWallCheck.position, lowerWallCheckRadius, groundLayer);

        if (!groundAhead || wallAhead || lowerWallAhead)
        {
            Flip();
            dir = isFacingRight ? 1f : -1f;
        }

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1 : -1);
        transform.localScale = scale;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;

        if ((player.position.x - transform.position.x) > 0 && !isFacingRight) Flip();
        if ((player.position.x - transform.position.x) < 0 && isFacingRight) Flip();

        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);

        if (!isDead && projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile_Shroom p = proj.GetComponent<Projectile_Shroom>();

            Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
            p.SetDirection(dir);
            p.SetDamage(damageToPlayer);
        }

        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    void UpdateAnimations()
    {
        if (isDead) return;
        anim.SetBool("Run", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
    }

    void PlayIdleSound()
    {
        if (AudioManager.Instance != null && !isDead)
        {
            AudioManager.Instance.PlaySFX(idleSoundIndex);
        }
    }

    void ResetIdleSoundTimer()
    {
        // Establecer un tiempo aleatorio entre min y max
        idleSoundTimer = Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        health -= dmg;

        if (health > 0)
            anim.SetTrigger("Hit");
        else
            Die();
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GetComponent<Collider2D>().enabled = false;

        anim.SetTrigger("Death");
        Destroy(gameObject, 1.5f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        if (wallCheck) Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        if (lowerWallCheck) Gizmos.DrawWireSphere(lowerWallCheck.position, lowerWallCheckRadius);
    }
}