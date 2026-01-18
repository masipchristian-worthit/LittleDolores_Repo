using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] int damage = 1;

    public void OnTriggerEnter2D(Collider2D other)
    {
        // Comprobar si es un enemigo
        GShroomEnemy enemy = other.GetComponent<GShroomEnemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
    }
}