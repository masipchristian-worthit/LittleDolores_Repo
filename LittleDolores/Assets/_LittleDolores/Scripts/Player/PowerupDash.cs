using UnityEngine;

public class PowerupDash : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] GameObject visualEffect; // (Opcional)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnlockDash();

                // Añade automáticamente el script visual al Player si no lo tiene
                if (GameManager.Instance.playerTransform != null)
                {
                    if (GameManager.Instance.playerTransform.GetComponent<PlayerDashEffect>() == null)
                    {
                        GameManager.Instance.playerTransform.gameObject.AddComponent<PlayerDashEffect>();
                    }
                }
                
                Debug.Log("¡Dash Desbloqueado!");
            }

            if (visualEffect != null) 
                Instantiate(visualEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
