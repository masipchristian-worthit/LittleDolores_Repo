using UnityEngine;

public class SpikeAttack : MonoBehaviour
{
    [SerializeField] int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Accedemos al GameManager para hacer daño
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeDamage(damage);
                Debug.Log("El player ha recibido daño de los pinchos");
            }
        }
    }
}