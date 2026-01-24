using UnityEngine;

public class LeverSwitch : MonoBehaviour
{
    [Header("Lever Objects")]
    [SerializeField] GameObject leverOffObject;
    [SerializeField] GameObject leverOnObject;

    [Header("Objects To Control")]
    [SerializeField] GameObject objectToTurnOff;
    [SerializeField] GameObject objectToTurnOn;

    [Header("Additional Objects To Activate")]
    [SerializeField] GameObject additionalObject1;
    [SerializeField] GameObject additionalObject2;

    [Header("State")]
    [SerializeField] bool isOn = false;

    bool canInteract = true;

    void Start()
    {
        ApplyState();
    }

    void ApplyState()
    {
        if (leverOffObject)
            leverOffObject.SetActive(!isOn);

        if (leverOnObject)
            leverOnObject.SetActive(isOn);

        if (objectToTurnOff)
            objectToTurnOff.SetActive(!isOn);

        if (objectToTurnOn)
            objectToTurnOn.SetActive(isOn);

        // Activar los objetos adicionales cuando la palanca está ON
        if (additionalObject1 != null)
            additionalObject1.SetActive(isOn);

        if (additionalObject2 != null)
            additionalObject2.SetActive(isOn);
    }

    void Toggle()
    {
        isOn = !isOn;
        ApplyState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canInteract) return;

        if (other.CompareTag("PlayerInteract"))
        {
            Toggle();
            canInteract = false;
            Invoke(nameof(ResetInteract), 0.3f); // evita spam
        }
    }

    void ResetInteract()
    {
        canInteract = true;
    }
}
