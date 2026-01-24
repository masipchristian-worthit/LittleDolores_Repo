using UnityEngine;
using TMPro; // Necesitas TextMeshPro para esto

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText; // Arrastra aquí tu texto de la UI
    [SerializeField] private GameObject timerContainer; // Opcional: El panel o fondo del timer

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.timeRemaining > 0)
        {
            // Mostramos el contenedor solo si el tiempo está corriendo
            if (timerContainer != null) timerContainer.SetActive(true);

            // Formateamos el tiempo (Minutos:Segundos)
            float t = GameManager.Instance.timeRemaining;
            int minutes = Mathf.FloorToInt(t / 60);
            int seconds = Mathf.FloorToInt(t % 60);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Opcional: Poner el texto en rojo cuando quede poco tiempo
            if (t <= 10f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
        else
        {
            // Si no hay timer activo, ocultamos el texto o el contenedor
            if (timerContainer != null) timerContainer.SetActive(false);
            else timerText.text = "";
        }
    }
}