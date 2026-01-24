using UnityEngine;

public class Spikes : MonoBehaviour
{
    public Transform teleport;
    public int damageAmount = 1; // Cantidad de daño que hará al jugador

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Quitar vida usando GameManager
            GameManager.Instance.TakeDamage(damageAmount);

            // Teletransportar al jugador
            other.transform.position = teleport.position;
        }
    }
}
