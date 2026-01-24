using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public int attackDamage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Intentar dañar al Boss (mantiene funcionalidad original)
        if (other.CompareTag("Boss"))
        {
            Boss boss = other.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(attackDamage);
                Debug.Log("Golpe al Boss detectado");
                return; // Retornamos para evitar doble daño si tiene varios scripts
            }
        }

        // 2. Intentar dañar al E_Shroom
        E_Shroom enemyShroom = other.GetComponent<E_Shroom>();
        if (enemyShroom != null)
        {
            enemyShroom.TakeDamage(attackDamage);
            Debug.Log("Golpe a E_Shroom detectado");
        }
    }
}