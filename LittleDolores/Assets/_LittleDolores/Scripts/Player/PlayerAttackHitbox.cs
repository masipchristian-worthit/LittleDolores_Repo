using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Diagnóstico visual
        Debug.Log($"[HITBOX] Contacto con: {other.gameObject.name} (Tag: {other.tag}) (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // 2. Obtener daño
        int damageToDeal = 1;
        if (GameManager.Instance != null)
        {
            damageToDeal = GameManager.Instance.playerDamage;
        }

        // === CORRECCIÓN IMPORTANTE ===
        // Usamos GetComponentInParent porque a veces golpeamos el "WallCheck" 
        // o un collider hijo del enemigo, y el script de vida está en el padre.
        
        // A. Intento con GShroomEnemy
        GShroomEnemy gEnemy = other.GetComponentInParent<GShroomEnemy>();
        if (gEnemy != null)
        {
            Debug.Log($"[HITBOX] -> Enemigo GShroom encontrado. Aplicando daño.");
            gEnemy.TakeDamage(damageToDeal);
            return; // Ya golpeamos, salimos para no golpear dos veces
        }

        // B. Intento con E_Shroom
        E_Shroom eEnemy = other.GetComponentInParent<E_Shroom>();
        if (eEnemy != null)
        {
            Debug.Log($"[HITBOX] -> Enemigo E_Shroom encontrado. Aplicando daño.");
            eEnemy.TakeDamage(damageToDeal);
            return;
        }
    }
}