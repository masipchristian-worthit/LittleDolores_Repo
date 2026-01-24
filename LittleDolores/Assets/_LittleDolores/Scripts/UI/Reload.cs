using UnityEngine;
using UnityEngine.SceneManagement;

public class Reload : MonoBehaviour
{
    public void RestartScene()
    {
        // 1. Restaurar vida
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerHealth = GameManager.Instance.maxHealth;
        }

        // 2. FUNDAMENTAL: Resetear el tiempo antes de cargar
        Time.timeScale = 1f;

        // 3. Opcional: Ocultar el cursor si tu juego lo requiere al empezar
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 4. Recargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}