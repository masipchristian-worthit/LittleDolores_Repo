using UnityEngine;

public class InteractButton : MonoBehaviour
{
    [Header("ACCIÓN (MODULAR)")]
    [Tooltip("Arrastra aquí el objeto único que este botón debe destruir/desactivar")]
    [SerializeField] GameObject objectToDestroy; 

    [Header("FEEDBACK AUDIO (AUDIO MANAGER)")]
    [Tooltip("El número del sonido en la lista sfxLibrary del AudioManager. (-1 para silencio)")]
    [SerializeField] int sfxIndex = -1;   
    
    [Tooltip("El número de la voz en la lista voicelinesLibrary del AudioManager. (-1 para silencio)")]
    [SerializeField] int voiceLineIndex = -1;  

    [Header("FEEDBACK CÁMARA")]
    [SerializeField] float shakeIntensity = 2f;
    [SerializeField] float shakeDuration = 0.3f;

    private bool hasActivated = false; // Candado de un solo uso

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detecta el collider de interacción ("PlayerInteract")
        if (other.CompareTag("PlayerInteract") && !hasActivated)
        {
            ActivateButton();
        }
    }

    void ActivateButton()
    {
        hasActivated = true; // Bloquear para que no se use más veces

        // 1. Destruir o Desactivar el objeto objetivo
        if (objectToDestroy != null)
        {
            Destroy(objectToDestroy); 
        }

        // 2. Reproducir Sonidos a través del AudioManager
        if (AudioManager.Instance != null)
        {
            // Solo intentamos reproducir si el índice es válido (mayor o igual a 0)
            if (sfxIndex >= 0) 
                AudioManager.Instance.PlaySFX(sfxIndex);
            
            if (voiceLineIndex >= 0) 
                AudioManager.Instance.PlayVoicelines(voiceLineIndex);
        }

        // 3. Activar Camera Shake
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeCamera(shakeIntensity, shakeDuration);
        }

        Debug.Log($"Botón {gameObject.name} activado.");
    }
}