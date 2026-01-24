using UnityEngine;
using System.Collections;

[System.Serializable]
public class HitAudio
{
    [Tooltip("Número de golpe del boss (1 = primer golpe, 2 = segundo, etc.)")]
    public int hitNumber;

    [Tooltip("Índice del clip en AudioManager.voicelinesLibrary que se reproducirá en este golpe.")]
    public int voicelineIndex;
}

public class Boss : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxLife = 3;
    public int currentLife;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite originalSprite;
    public Sprite damageSprite;
    public float damageSpriteDuration = 0.2f;

    [Header("Teleport")]
    public Transform teleportTarget;

    [Header("UI & Pickup")]
    public GameObject healthBarUI;
    public GameObject pickupImage;

    [Header("Voicelines")]
    public HitAudio[] hitVoicelines;

    [Header("Golpes")]
    public int hitCount = 0;

    private bool takingDamage = false;

    void Awake()
    {
        currentLife = maxLife;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (pickupImage != null)
            pickupImage.SetActive(false);
    }

    // ====================== MÉTODO PÚBLICO ======================
    public void TakeDamage(int damage)
    {
        if (!takingDamage)
            StartCoroutine(TakeDamageRoutine(damage));
    }

    // ====================== COROUTINE DE DAÑO ======================
    private IEnumerator TakeDamageRoutine(int damage)
    {
        takingDamage = true;

        // Cambiar sprite a daño
        if (spriteRenderer != null && damageSprite != null)
            spriteRenderer.sprite = damageSprite;

        // Espera visual del daño
        yield return new WaitForSeconds(damageSpriteDuration);

        // Volver a sprite normal
        if (spriteRenderer != null && originalSprite != null)
            spriteRenderer.sprite = originalSprite;

        // Aplicar daño y contar golpes
        currentLife -= damage;
        hitCount++;
        Debug.Log($"Boss vida: {currentLife} | golpe {hitCount}");

        // Reproducir voiceline correspondiente
        if (hitVoicelines != null)
        {
            foreach (HitAudio ha in hitVoicelines)
            {
                if (ha.hitNumber == hitCount)
                {
                    AudioManager.Instance.PlayVoicelines(ha.voicelineIndex);
                    break;
                }
            }
        }

        // Teletransportar al player si corresponde
        if (teleportTarget != null && currentLife > 0 && hitCount <= 2)
        {
            if (GameManager.Instance != null && GameManager.Instance.playerTransform != null)
                GameManager.Instance.playerTransform.position = teleportTarget.position;
        }

        // Último golpe: parar al jugador y morir
        if (currentLife <= 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.currentMoveSpeed = 0f;

            Die();
        }

        takingDamage = false;
    }

    // ====================== MUERTE ======================
    private void Die()
    {
        Debug.Log("Boss derrotado");

        // Desactivar barra de vida
        if (healthBarUI != null)
            healthBarUI.SetActive(false);

        // Activar pickup o imagen
        if (pickupImage != null)
            pickupImage.SetActive(true);

        Destroy(gameObject);
    }
}
