using UnityEngine;

public class PowerupSpeed : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] float speedIncreaseAmount = 1.5f; // Cuánto aumenta la velocidad
    [SerializeField] GameObject visualEffect; // (Opcional)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpgradeSpeed(speedIncreaseAmount);
                Debug.Log("¡Velocidad aumentada!");
            }

            if (visualEffect != null) 
                Instantiate(visualEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
