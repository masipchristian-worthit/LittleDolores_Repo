using UnityEngine;

public class InteractButton : MonoBehaviour
{
    [Header("OBJECTS TO SWITCH")]
    [SerializeField] GameObject objectOn;
    [SerializeField] GameObject objectOff;

    [Header("EXTRA OBJECT TO DISABLE")]
    [SerializeField] GameObject extraObjectToDisable;

    [Header("FEEDBACK AUDIO (AUDIO MANAGER)")]
    [Tooltip("Índice del SFX en AudioManager (-1 para silencio)")]
    [SerializeField] int sfxIndex = -1;

    [Tooltip("Índice de la voz en AudioManager (-1 para silencio)")]
    [SerializeField] int voiceLineIndex = -1;

    [Tooltip("Delay antes de reproducir la voice line")]
    [SerializeField] float voiceLineDelay = 1f;

    [Header("FEEDBACK CÁMARA")]
    [SerializeField] float shakeIntensity = 2f;
    [SerializeField] float shakeDuration = 0.3f;

    private bool hasActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerInteract") && !hasActivated)
        {
            ActivateButton();
        }
    }

    void ActivateButton()
    {
        hasActivated = true;

        // 1. Switch de objetos
        if (objectOn != null)
            objectOn.SetActive(false);

        if (objectOff != null)
            objectOff.SetActive(true);

        // 2. Objeto extra
        if (extraObjectToDisable != null)
            extraObjectToDisable.SetActive(false);

        // 3. Audio
        if (AudioManager.Instance != null)
        {
            if (sfxIndex >= 0)
                AudioManager.Instance.PlaySFX(sfxIndex);

            if (voiceLineIndex >= 0)
                Invoke(nameof(PlayVoiceLine), voiceLineDelay);
        }

        // 4. Camera Shake
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeCamera(shakeIntensity, shakeDuration);
        }

        Debug.Log($"InteractButton {gameObject.name} activado.");
    }

    void PlayVoiceLine()
    {
        if (AudioManager.Instance != null && voiceLineIndex >= 0)
        {
            AudioManager.Instance.PlayVoicelines(voiceLineIndex);
        }
    }
}
