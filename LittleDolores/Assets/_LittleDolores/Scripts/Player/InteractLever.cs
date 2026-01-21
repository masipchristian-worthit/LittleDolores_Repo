using UnityEngine;

public class SwitchObjects : MonoBehaviour
{
    [Header("OBJECTS TO SWITCH")]
    [SerializeField] GameObject objectOn;
    [SerializeField] GameObject objectOff;

    [Header("EXTRA OBJECT TO DISABLE")]
    [SerializeField] GameObject extraObject;

    bool hasActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerInteract") && !hasActivated)
        {
            hasActivated = true;
            Debug.Log("Trigger detectado por: " + other.name);
            Switch();
        }
    }

    void Switch()
    {
        Debug.Log("Intentando apagar ON y encender OFF");

        if (objectOn != null)
        {
            Debug.Log("Apagando: " + objectOn.name);
            objectOn.SetActive(false);
        }
        else
        {
            Debug.LogError("objectOn NO está asignado");
        }

        if (objectOff != null)
        {
            Debug.Log("Encendiendo: " + objectOff.name);
            objectOff.SetActive(true);
        }
        else
        {
            Debug.LogError("objectOff NO está asignado");
        }

        if (extraObject != null)
        {
            Debug.Log("Apagando extra: " + extraObject.name);
            extraObject.SetActive(false);
        }
    }
}
