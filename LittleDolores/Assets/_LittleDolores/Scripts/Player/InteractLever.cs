using UnityEngine;

public class InteractLever : MonoBehaviour
{
    [Header("OBJECTS TO SWITCH")]
    [SerializeField] GameObject LEVERON;   // El que empieza ENCENDIDO
    [SerializeField] GameObject LEVEROFF;  // El que empieza APAGADO

    [Header("EXTRA OBJECT TO DISABLE")]
    [SerializeField] GameObject TABLE; // Objeto en otra zona que se apaga

    bool hasActivated = false; // Para que solo se active una vez (opcional)

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detecta solo el collider de interacción del player
        if (other.CompareTag("PlayerInteract") && !hasActivated)
        {
            hasActivated = true;
            Switch();
        }
    }

    void Switch()
    {
        if (LEVERON != null)
            LEVERON.SetActive(false);   // Apagar el que estaba encendido

        if (LEVEROFF != null)
            LEVEROFF.SetActive(true);   // Encender el que estaba apagado

        if (TABLE != null)
            TABLE.SetActive(false); // Apagar el objeto extra

        Debug.Log("Objetos intercambiados");
    }
}