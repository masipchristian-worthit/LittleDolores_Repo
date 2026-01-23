using UnityEngine;

public class EnemyHurtboxPurple : MonoBehaviour
{
    // Referencia al script principal que controla la vida
    [SerializeField] PShroomEnemy mainScript;

    private void Awake()
    {
        // Intenta encontrarse a sí mismo en el padre si no lo asignas manual
        if (mainScript == null)
        {
            mainScript = GetComponentInParent<PShroomEnemy>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo reacciona si lo que entra es el ataque del jugador
        if (other.gameObject.CompareTag("PlayerAttack"))
        {
            // 1. Obtener daño del GameManager
            int dmg = 1;
            if (GameManager.Instance != null)
            {
                dmg = GameManager.Instance.playerDamage;
            }

            // 2. Aplicar daño al padre
            if (mainScript != null)
            {
                // TakeDamage ya contiene la lógica de restar vida y anim.SetTrigger("Hit")
                mainScript.TakeDamage(dmg);
                
                // Debug para confirmar
                Debug.Log($"[HURTBOX] Golpe recibido en {gameObject.name}. Vida restante padre: {mainScript.health}");
            }
        }
    }
}