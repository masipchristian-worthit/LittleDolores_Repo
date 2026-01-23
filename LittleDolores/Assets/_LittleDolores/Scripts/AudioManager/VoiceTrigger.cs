using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VoiceTrigger : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [Tooltip("El índice del audio en el array voicelinesLibrary del AudioManager")]
    public int voiceLineIndex = 0;
    
    [Tooltip("Si es true, el audio solo sonará la primera vez que toques el trigger")]
    public bool playOnlyOnce = true;

    [Header("Control de Movimiento")]
    [Tooltip("Activa esto para modificar la velocidad del jugador mientras habla")]
    public bool modifyPlayerSpeed = false;
    
    [Tooltip("La velocidad que tendrá el jugador mientras dura el audio (0 para congelarlo)")]
    public float temporarySpeed = 0f;

    // Hacemos pública esta variable también por si necesitas resetearla desde otro script
    [Header("Estado (Debug)")]
    public bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo reacciona al Player
        if (other.CompareTag("Player"))
        {
            // Si ya sonó y es de un solo uso, no hacemos nada
            if (playOnlyOnce && hasPlayed) return;

            PlaySequence();
        }
    }

    private void PlaySequence()
    {
        hasPlayed = true;

        // 1. Validar que el AudioManager existe
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("VoiceTrigger: No se encontró AudioManager en la escena.");
            return;
        }

        // 2. Reproducir el audio
        AudioManager.Instance.PlayVoicelines(voiceLineIndex);

        // 3. Si hay que modificar la velocidad, iniciamos la Corrutina
        if (modifyPlayerSpeed)
        {
            StartCoroutine(ControlSpeedRoutine());
        }
    }

    private IEnumerator ControlSpeedRoutine()
    {
        // A. Obtener duración del clip
        float clipDuration = 0f;
        
        // Accedemos a la librería pública del AudioManager
        if (voiceLineIndex >= 0 && voiceLineIndex < AudioManager.Instance.voicelinesLibrary.Length)
        {
            AudioClip clip = AudioManager.Instance.voicelinesLibrary[voiceLineIndex];
            if (clip != null)
            {
                clipDuration = clip.length;
            }
        }
        else
        {
            Debug.LogError("VoiceTrigger: Índice de audio fuera de rango.");
            yield break;
        }

        // B. MODIFICACIÓN DE VELOCIDAD
        float originalSpeed = 10f; 

        if (GameManager.Instance != null)
        {
            // Capturamos la velocidad ACTUAL del GameManager
            originalSpeed = GameManager.Instance.currentMoveSpeed; 
            
            // Aplicamos la velocidad temporal
            GameManager.Instance.currentMoveSpeed = temporarySpeed; 
        }

        // C. Esperar a que termine el audio (+ pequeño margen)
        yield return new WaitForSeconds(clipDuration + 0.1f);

        // D. Restaurar la velocidad original
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentMoveSpeed = originalSpeed;
        }
    }
}