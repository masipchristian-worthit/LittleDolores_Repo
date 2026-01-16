using UnityEngine;

public class AttackSpotTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Buscamos todas las setas y damos la orden
            // Gracias a la IA, solo saltarán las que estén en estado "Flank" y en posición.
            GShroomEnemy[] enemies = FindObjectsByType<GShroomEnemy>(FindObjectsSortMode.None);
            
            foreach (var enemy in enemies)
            {
                enemy.OrderAmbushAttack();
            }
        }
    }
}