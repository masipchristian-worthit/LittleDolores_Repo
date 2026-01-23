using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private int maxLife = 3;
    private int currentLife;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite originalSprite;
    public Sprite damageSprite;
    public float damageSpriteDuration = 2f;

    [Header("Teleport")]
    public Transform teleportTarget;

    [Header("Attack Reference")]
    public Collider2D playerAttackCollider;

    [Header("UI & Pickup")]
    public GameObject healthBarUI;        // Panel con barra de vida
    public GameObject pickupImage;        // Imagen que aparece al morir (desactivada al inicio)

    private bool takingDamage;
    private int hitCount = 0; // Contador de golpes recibidos

    // ================= PROPIEDADES PÚBLICAS =================
    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;

    // =========================================================
    private void Awake()
    {
        currentLife = maxLife;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Desactivar pickup al inicio
        if (pickupImage != null)
            pickupImage.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack") && !takingDamage)
        {
            Debug.Log("Hitbox del jugador detectada");
            StartCoroutine(TakeDamageRoutine());
        }
    }

    IEnumerator TakeDamageRoutine()
    {
        takingDamage = true;

        // Cambiar al sprite de daño
        if (spriteRenderer != null && damageSprite != null)
            spriteRenderer.sprite = damageSprite;

        // Esperar tiempo de feedback
        yield return new WaitForSeconds(damageSpriteDuration);

        // Volver al sprite original
        if (spriteRenderer != null && originalSprite != null)
            spriteRenderer.sprite = originalSprite;

        // Aplicar daño
        currentLife -= 1;
        hitCount++;
        Debug.Log("Boss vida: " + currentLife + " | golpe " + hitCount);

        // Si es el último golpe, poner velocidad del player a 0
        bool isLastHit = currentLife <= 0;
        if (isLastHit && GameManager.Instance != null)
        {
            GameManager.Instance.currentMoveSpeed = 0f;
        }

        // Teletransportar solo si NO es el golpe final y aún quedan golpes para TP
        if (teleportTarget != null && currentLife > 0 && hitCount <= 2)
        {
            if (GameManager.Instance != null && GameManager.Instance.playerTransform != null)
                GameManager.Instance.playerTransform.position = teleportTarget.position;
        }

        // Revisar si murió
        if (isLastHit)
        {
            Die();
        }

        takingDamage = false;
    }

    private void Die()
    {
        Debug.Log("Boss derrotado");

        // Desactivar barra de vida
        if (healthBarUI != null)
            healthBarUI.SetActive(false);

        // Activar la imagen/pickup en el mundo
        if (pickupImage != null)
            pickupImage.SetActive(true);

        // Destruir el boss
        Destroy(gameObject);
    }
}
