using UnityEngine;

public class PlayerAnimEvents : MonoBehaviour
{
    // Esta función aparecerá en el Animation Event.
    // Acepta un 'int', así que puedes escribir el número del sonido directamente en la animación.
    public void PlaySFX(int soundIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundIndex);
        }
    }
    public void PlayVoiceline(int voiceLineIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoicelines(voiceLineIndex);
        }
    }
}
