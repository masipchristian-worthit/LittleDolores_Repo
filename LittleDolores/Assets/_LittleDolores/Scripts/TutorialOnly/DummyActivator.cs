using UnityEngine;

public class DummyActivator : MonoBehaviour
{
    [Header("Activación Visual")]
    [Tooltip("Arrastra aquí el GameObject que quieres encender (ej: partículas, texto)")]
    [SerializeField] private GameObject objectToActivate;

    [Tooltip("Si es TRUE, el objeto solo se activa la primera vez. Si es FALSE, se reactiva siempre.")]
    [SerializeField] private bool activateOnce = true;

    [Header("Easter Eggs / Audio")]
    [Tooltip("Índice del audio en AudioManager para el PRIMER golpe")]
    [SerializeField] private int firstHitSoundIndex = 0;

    [Tooltip("Índice del audio en AudioManager para el golpe número 50")]
    [SerializeField] private int fiftyHitSoundIndex = 1;

    // Estado interno
    private bool hasActivated = false;
    private int hitCount = 0;

    // ---------------------------------------------------------
    // MÉTODO 1: Detección por el sistema de daño (SendMessage)
    // ---------------------------------------------------------
    public void TakeDamage(int damage)
    {
        ProcessHit();
    }

    // ---------------------------------------------------------
    // MÉTODO 2: Detección por física directa (Trigger)
    // ---------------------------------------------------------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack"))
        {
            ProcessHit();
        }
    }

    // Lógica central
    private void ProcessHit()
    {
        // 1. AUMENTAR CONTADOR
        hitCount++;
        // Debug.Log($"[DUMMY] Golpes recibidos: {hitCount}");

        // 2. COMPROBAR AUDIOS (Easter Eggs)
        if (AudioManager.Instance != null)
        {
            if (hitCount == 1)
            {
                // Primer golpe
                AudioManager.Instance.PlayVoicelines(firstHitSoundIndex);
            }
            else if (hitCount == 50)
            {
                // Golpe 50
                AudioManager.Instance.PlayVoicelines(fiftyHitSoundIndex);
                Debug.Log("[DUMMY] ¡Logro desbloqueado! 50 golpes.");
            }
        }

        // 3. ACTIVAR OBJETO (Lógica original)
        // Si está en modo "solo una vez" y ya se activó, paramos aquí la parte visual.
        // (Pero el contador de golpes sigue funcionando para llegar a 50).
        if (activateOnce && hasActivated) return;

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }

        hasActivated = true;
    }
}