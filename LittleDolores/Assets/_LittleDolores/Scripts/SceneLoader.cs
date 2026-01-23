using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Nombre de la escena a cargar")]
    public string sceneName; // Nombre exacto de la escena que quieres cargar

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica que sea el jugador
        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("No se ha asignado el nombre de la escena en ChangeSceneOnTrigger.");
            }
        }
    }
}
