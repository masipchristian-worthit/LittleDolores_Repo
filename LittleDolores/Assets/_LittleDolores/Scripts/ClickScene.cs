using UnityEngine;
using UnityEngine.SceneManagement; // Muy importante

public class ClickScene : MonoBehaviour
{
    [Header("Nombre de la escena a cargar")]
    public string sceneName;

    // Función pública para llamar desde el botón
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el nombre de la escena.");
        }
    }
}
