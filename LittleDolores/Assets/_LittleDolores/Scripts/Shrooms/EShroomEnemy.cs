using UnityEngine;
using System.Collections;

public class E_Shroom : MonoBehaviour
{
    [Header("STATS")]
    [SerializeField] public int health = 2; // público para poder cambiar desde inspector
    [SerializeField] float moveSpeed = 2.5f;

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

    Rigidbody2D rb;
    Animator anim;
    Transform player;

    float attackTimer;
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
    }

    void Update()
    {
        // Comprobación automática de vida
        if (!isDead && health <= 0)
            Die();

        if (isDead || player == null) return;

        attackTimer -= Time.deltaTime;

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
        if (isAttacking) return;

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

        // Los checks hijos giran automáticamente
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        rb.linearVelocity = Vector2.zero;

        // Mirar al player
        if ((player.position.x - transform.position.x) > 0 && !isFacingRight) Flip();
        if ((player.position.x - transform.position.x) < 0 && isFacingRight) Flip();

        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);

        if (!isDead && projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
            proj.GetComponent<Projectile_Shroom>().SetDirection(dir);
        }

        yield return new WaitForSeconds(0.4f);

        isAttacking = false;
    }

    void UpdateAnimations()
    {
        if (isDead) return;

        bool moving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim.SetBool("Run", moving);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("PlayerAttack"))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        health -= dmg;
        anim.SetTrigger("Hit");

        // Comprobación opcional extra (en caso de que TakeDamage haga morir)
        if (health <= 0)
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
