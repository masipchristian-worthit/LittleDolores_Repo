using UnityEngine;

public class WeaponDebug : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Esto imprimirá CUALQUIER COSA que toque el arma (Suelo, Enemigo, Pared)
        Debug.Log($"[ARMA] He tocado: {other.gameObject.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)} | Tag: {other.tag}");
    }
}
