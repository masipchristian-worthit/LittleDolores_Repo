using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] int maxLife = 3;
    int currentLife;

    [Header("Attack Reference")]
    [SerializeField] Collider2D playerAttackCollider;

    bool hit;

    void Awake()
    {
        currentLife = maxLife;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Debug cuando el collider del ataque está tocando al boss
        if (other == playerAttackCollider)
        {
            Debug.Log("Hitbox del jugador detectada");

            if (!hit)
            {
                hit = true;
                TakeDamage(1);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == playerAttackCollider)
        {
            hit = false;
        }
    }

    void TakeDamage(int damage)
    {
        currentLife -= damage;
        Debug.Log("Boss vida: " + currentLife);

        if (currentLife <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Boss derrotado");
        Destroy(gameObject);
    }
}
