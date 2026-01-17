using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica si golpeamos a una Seta Enemiga
        if (other.TryGetComponent(out GShroomEnemy enemy))
        {
            int totalDamage = 1; // Seguridad por si GM es nulo

            if (GameManager.Instance != null)
            {
                // Usamos DIRECTAMENTE el valor del GameManager
                totalDamage = GameManager.Instance.playerDamage;
            }

            enemy.TakeDamage(totalDamage);
            Debug.Log($"¡ZAS! Daño aplicado: {totalDamage}");
        }
        
        // Cuando tengas el Boss:
        /*
        else if (other.TryGetComponent(out BossEnemy boss))
        {
             int totalDamage = GameManager.Instance.playerDamage;
             GameManager.Instance.DamageBoss(totalDamage);
        }
        */
    }
}