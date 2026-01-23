using UnityEngine;

public class ShowCanvas : MonoBehaviour
{
    [Header("Canvas a mostrar")]
    public GameObject canvasToShow; // Arrastra el Canvas que quieres mostrar

    private void Awake()
    {
        // Asegurarse que el canvas está apagado al inicio
        if (canvasToShow != null)
            canvasToShow.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica que el jugador entre al collider
        if (other.CompareTag("Player"))
        {
            if (canvasToShow != null)
            {
                canvasToShow.SetActive(true);
            }
        }
    }
}
