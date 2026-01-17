using UnityEngine;

public class PowerupJump : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] float jumpIncreaseAmount = 2.0f; // Cuánto aumenta el salto
    [SerializeField] GameObject visualEffect; // (Opcional) Arrastra un prefab de partículas si quieres

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpgradeJump(jumpIncreaseAmount);
                Debug.Log("¡Salto aumentado!");
            }

            // Efecto visual al recoger
            if (visualEffect != null) 
                Instantiate(visualEffect, transform.position, Quaternion.identity);

            // Destruir el objeto
            Destroy(gameObject);
        }
    }
}