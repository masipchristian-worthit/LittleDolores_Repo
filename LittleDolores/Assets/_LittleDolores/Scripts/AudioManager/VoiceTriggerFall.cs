using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VoiceTriggerFall : MonoBehaviour
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
    [Tooltip("Activa esto para modificar la velocidad del jugador mientras espera y mientras habla")]
    public bool modifyPlayerSpeed = false;

    [Tooltip("La velocidad que tendrá el jugador (0 para congelarlo)")]
    public float temporarySpeed = 0f;

    [Header("Teletransporte")]
    [Tooltip("Transform al que se teletransportará el jugador al terminar el audio")]
    public Transform teleportTarget;

    [Header("Estado (Debug)")]
    public bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playOnlyOnce && hasPlayed) return;
            StartCoroutine(PlayAndTeleportRoutine(other.transform));
        }
    }

    private IEnumerator PlayAndTeleportRoutine(Transform player)
    {
        hasPlayed = true;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("VoiceTriggerWithTeleport: No hay AudioManager.");
            yield break;
        }

        // 1. Obtener duración del clip
        float clipDuration = 0f;
        if (voiceLineIndex >= 0 && voiceLineIndex < AudioManager.Instance.voicelinesLibrary.Length)
        {
            AudioClip clip = AudioManager.Instance.voicelinesLibrary[voiceLineIndex];
            if (clip != null) clipDuration = clip.length;
        }
        else
        {
            Debug.LogError("VoiceTriggerWithTeleport: Índice fuera de rango.");
            yield break;
        }

        // 2. Guardar y aplicar velocidad temporal
        float originalSpeed = 10f;
        if (GameManager.Instance != null)
        {
            originalSpeed = GameManager.Instance.currentMoveSpeed;
            if (modifyPlayerSpeed)
            {
                GameManager.Instance.currentMoveSpeed = temporarySpeed;
            }
        }

        // 3. Esperar si hay prioridad
        while (AudioManager.Instance.isPriorityPlaying)
        {
            yield return null;
        }

        // 4. Bloquear canal si prioridad
        if (priority == 1)
            AudioManager.Instance.isPriorityPlaying = true;

        // 5. Reproducir voiceline
        AudioManager.Instance.PlayVoicelines(voiceLineIndex);

        // 6. Esperar duración del clip
        yield return new WaitForSeconds(clipDuration + 0.1f);

        // 7. Teletransportar al jugador si se asignó un target
        if (teleportTarget != null)
            player.position = teleportTarget.position;

        // 8. Desbloquear canal si prioridad
        if (priority == 1)
            AudioManager.Instance.isPriorityPlaying = false;

        // 9. Restaurar velocidad
        if (modifyPlayerSpeed && GameManager.Instance != null)
            GameManager.Instance.currentMoveSpeed = originalSpeed;
    }
}
