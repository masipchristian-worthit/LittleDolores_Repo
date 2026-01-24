using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ButtonSceneChangeWithVoice : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneName;                // Escena a cargar
    public int voicelineIndex = 0;          // Índice del voiceline en AudioManager

    [Header("Timing")]
    public bool waitForVoiceline = true;    // Esperar a que termine el audio antes de cambiar

    public void OnButtonClick()
    {
        // Reproducir voiceline desde AudioManager
        if (AudioManager.Instance != null)
        {
            if (voicelineIndex >= 0 && voicelineIndex < AudioManager.Instance.voicelinesLibrary.Length)
            {
                AudioClip clip = AudioManager.Instance.voicelinesLibrary[voicelineIndex];
                AudioManager.Instance.voicelinesSource.PlayOneShot(clip);

                // Cambiar escena
                if (waitForVoiceline)
                    StartCoroutine(LoadSceneAfterDelay(clip.length));
                else
                    SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("Índice de voiceline fuera de rango.");
                SceneManager.LoadScene(sceneName);
            }
        }
        else
        {
            Debug.LogWarning("No se encontró AudioManager.");
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
