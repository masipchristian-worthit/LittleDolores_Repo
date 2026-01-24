using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image healthFill;  // Imagen que tiene Image Type = Filled, Horizontal

    [Header("Smooth Fill")]
    [SerializeField] private float smoothSpeed = 5f; // Para suavizar la barra

    void Update()
    {
        if (GameManager.Instance == null || healthFill == null) return;

        // Calcula el porcentaje de vida
        float targetFill = (float)GameManager.Instance.playerHealth / (float)GameManager.Instance.maxHealth;

        // Actualiza la barra suavemente
        healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
    }
}
