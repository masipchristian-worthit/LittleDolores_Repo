using UnityEngine;

public class AttackTp : MonoBehaviour
{
    public Transform destino;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (destino != null)
        {
            other.transform.position = destino.position;
        }
    }
}