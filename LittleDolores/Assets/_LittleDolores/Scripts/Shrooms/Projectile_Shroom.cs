using UnityEngine;

public class Projectile_Shroom : MonoBehaviour
{
    [Header("STATS")]
    [SerializeField] float speed = 6f;
    [SerializeField] float lifeTime = 2f;
    [SerializeField] int damage = 1;

    Vector2 direction;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!rb)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.isKinematic = true;
        rb.gravityScale = 0;

        Destroy(gameObject, lifeTime);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir.x >= 0 ? 1 : -1);
        transform.localScale = scale;
    }

    public void SetDamage(int dmg)
    {
        damage = dmg;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ataque del Player se rompe sin daño
        if (other.CompareTag("PlayerAttack"))
        {
            Destroy(gameObject);
            return;
        }

        // Player hace daño
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
