using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    [Header("Configuración Flotación")]
    [SerializeField] float amplitude = 0.25f; // Cuánto sube y baja (Distancia)
    [SerializeField] float frequency = 2f;    // Qué tan rápido se mueve (Velocidad)

    Vector3 startPos;
    float randomOffset;

    void Start()
    {
        // Guardamos la posición inicial para pivotar alrededor de ella
        startPos = transform.position;
        
        // Añadimos un desfase aleatorio basado en la posición X
        // Esto hace que si pones varias monedas juntas, hagan una "ola" en lugar de moverse a la vez
        randomOffset = transform.position.x; 
    }

    void Update()
    {
        // Fórmula matemática para el movimiento suave (Onda Senoidal)
        // Y = Y_Inicial + Seno(Tiempo * Velocidad + Desfase) * Altura
        float newY = startPos.y + Mathf.Sin((Time.time + randomOffset) * frequency) * amplitude;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}