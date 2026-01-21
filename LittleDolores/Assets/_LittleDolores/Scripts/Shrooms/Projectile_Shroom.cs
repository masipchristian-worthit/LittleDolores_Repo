using UnityEngine;

public class Projectile_Shroom : MonoBehaviour
{
    [SerializeField] float speed = 6f;
    [SerializeField] float lifeTime = 2f;

    Vector2 direction;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.isKinematic = true;
        rb.gravityScale = 0;

        // Se destruye solo a los 2 segundos si no golpea al Player
        Destroy(gameObject, lifeTime);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;

        // Girar sprite
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir.x >= 0 ? 1 : -1);
        transform.localScale = scale;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
