using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. FILTRO DE SEGURIDAD: Solo atacamos a cosas marcadas como "Enemy"
        // Si la nueva seta no tiene este Tag, el código se detiene aquí.
        if (other.CompareTag("Enemy"))
        {
            // Diagnóstico: Si sale esto, la física y el tag están bien.
            Debug.Log($"[ARMA] Golpeó a: {other.name}");

            // 2. OBTENER DAÑO
            int damage = 1;
            if (GameManager.Instance != null) 
            {
                damage = GameManager.Instance.playerDamage;
            }

            // 3. ENVIAR DAÑO UNIVERSAL
            // Busca la función "TakeDamage" en el objeto golpeado O en sus padres.
            // Funciona con CUALQUIER script (Verde, Rojo, Morado, Boss) automáticamente.
            other.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }
}