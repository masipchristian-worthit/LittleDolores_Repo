using UnityEngine;

public class CollisionSniffer : MonoBehaviour
{
    // Escuchamos AMBOS tipos de entrada para descartar errores de configuración
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[SNIFFER TRIGGER] ¡ALGO ENTRÓ! Objeto: {other.name} | Tag: {other.tag} | Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[SNIFFER COLLISION] ¡CHOQUE FÍSICO! Objeto: {collision.gameObject.name} | Tag: {collision.gameObject.tag}");
    }
}
