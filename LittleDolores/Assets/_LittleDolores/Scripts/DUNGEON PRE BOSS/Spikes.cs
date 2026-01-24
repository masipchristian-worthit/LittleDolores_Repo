using UnityEngine;

public class Spikes : MonoBehaviour
{
    public Transform teleport;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = teleport.position;
        }
    }
}
