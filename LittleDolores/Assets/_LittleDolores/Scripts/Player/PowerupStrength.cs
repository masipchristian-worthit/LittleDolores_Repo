using UnityEngine;

public class PowerupStrength : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] int damageIncreaseAmount = 1; // Cuánto daño extra añade
    [SerializeField] GameObject visualEffect; // (Opcional)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpgradeDamage(damageIncreaseAmount);
                Debug.Log("¡Fuerza aumentada!");
            }

            if (visualEffect != null) 
                Instantiate(visualEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}