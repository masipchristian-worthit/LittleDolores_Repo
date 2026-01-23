using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image healthFill;  // Imagen que tiene Image Type = Filled, Horizontal

    void Update()
    {
        if (GameManager.Instance == null) return;

        // Convertimos a float para evitar división entera
        float fill = (float)GameManager.Instance.playerHealth / (float)GameManager.Instance.maxHealth;
        healthFill.fillAmount = Mathf.Clamp01(fill); // Entre 0 y 1
    }
}
