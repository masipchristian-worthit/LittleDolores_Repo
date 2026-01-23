using UnityEngine;

public class LeverSwitch : MonoBehaviour
{
    [Header("Lever Objects")]
    [SerializeField] GameObject leverOffObject;
    [SerializeField] GameObject leverOnObject;

    [Header("Objects To Control")]
    [SerializeField] GameObject objectToTurnOff;
    [SerializeField] GameObject objectToTurnOn;

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
    }

    void Toggle()
    {
        isOn = !isOn;
        ApplyState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // El collider de interacción del Player
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
