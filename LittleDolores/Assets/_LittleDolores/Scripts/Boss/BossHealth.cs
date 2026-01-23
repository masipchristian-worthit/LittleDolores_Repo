using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image healthFill;  // Image tipo Filled, Horizontal

    [Header("Boss Reference")]
    public Boss boss; // Arrastra tu Boss desde la escena

    [Header("Smooth Fill")]
    public float smoothSpeed = 5f; // Para que la barra baje suavemente

    void Update()
    {
        if (boss == null || healthFill == null) return;

        float targetFill = (float)boss.CurrentLife / (float)boss.MaxLife;
        healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
    }
}
