using UnityEngine;

public class GasAttack : MonoBehaviour
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
                Debug.Log("Player ha respirado gas tóxico.");
            }
        }
    }
}