using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VoiceTrigger : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [Tooltip("El índice del audio en el array voicelinesLibrary del AudioManager")]
    public int voiceLineIndex = 0;
    
    [Tooltip("0 = Normal (puede esperar). 1 = Prioritario (Bloquea a los siguientes hasta terminar).")]
    [Range(0, 1)] 
    public int priority = 0;

    [Tooltip("Si es true, el audio solo sonará la primera vez que toques el trigger")]
    public bool playOnlyOnce = true;

    [Header("Control de Movimiento")]
    [Tooltip("Activa esto para modificar la velocidad del jugador MIENTRAS espera y mientras habla")]
    public bool modifyPlayerSpeed = false;
    
    [Tooltip("La velocidad que tendrá el jugador (0 para congelarlo)")]
    public float temporarySpeed = 0f;

    [Header("Estado (Debug)")]
    public bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playOnlyOnce && hasPlayed) return;
            StartCoroutine(PlaySequenceRoutine());
        }
    }

    private IEnumerator PlaySequenceRoutine()
    {
        hasPlayed = true;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("VoiceTrigger: No hay AudioManager.");
            yield break;
        }

        // 1. CÁLCULO DE DURACIÓN (Antes de nada)
        float clipDuration = 0f;
        if (voiceLineIndex >= 0 && voiceLineIndex < AudioManager.Instance.voicelinesLibrary.Length)
        {
            AudioClip clip = AudioManager.Instance.voicelinesLibrary[voiceLineIndex];
            if (clip != null) clipDuration = clip.length;
        }
        else
        {
            Debug.LogError("VoiceTrigger: Índice fuera de rango.");
            yield break;
        }

        // 2. APLICAR FRENO INMEDIATO (Requisito: Frenar aunque tenga que esperar)
        float originalSpeed = 10f;
        if (GameManager.Instance != null)
        {
            originalSpeed = GameManager.Instance.currentMoveSpeed; // Guardar velocidad actual
            if (modifyPlayerSpeed)
            {
                GameManager.Instance.currentMoveSpeed = temporarySpeed; // Aplicar freno
            }
        }

        // 3. ESPERAR TURNO (Cola de prioridad)
        // Mientras haya OTRO audio prioritario sonando, esperamos aquí.
        // El jugador ya está frenado si modifyPlayerSpeed estaba activo.
        while (AudioManager.Instance.isPriorityPlaying)
        {
            yield return null;
        }

        // 4. BLOQUEAR CANAL (Si somos Prioridad 1)
        if (priority == 1)
        {
            AudioManager.Instance.isPriorityPlaying = true;
        }

        // 5. REPRODUCIR SONIDO
        AudioManager.Instance.PlayVoicelines(voiceLineIndex);

        // 6. ESPERAR DURACIÓN DEL AUDIO
        yield return new WaitForSeconds(clipDuration + 0.1f);

        // 7. DESBLOQUEAR CANAL (Si éramos Prioridad 1)
        if (priority == 1)
        {
            AudioManager.Instance.isPriorityPlaying = false;
        }

        // 8. RESTAURAR VELOCIDAD
        if (modifyPlayerSpeed && GameManager.Instance != null)
        {
            GameManager.Instance.currentMoveSpeed = originalSpeed;
        }
    }
}