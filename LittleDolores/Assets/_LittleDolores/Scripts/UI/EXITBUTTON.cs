using UnityEngine;

public class EXITBUTTON : MonoBehaviour
{
    // Este método lo asignas al OnClick del botón
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Si estás en el Editor de Unity, esto detiene la reproducción
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Si estás en build, cierra el juego
        Application.Quit();
#endif
    }
}
