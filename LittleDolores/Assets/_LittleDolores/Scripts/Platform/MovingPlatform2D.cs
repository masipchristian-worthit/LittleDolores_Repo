using UnityEngine;

public class MovingPlatform32D : MonoBehaviour
{
    [Header("Waypoints & Movement Configuration")]
    [SerializeField] float speed; //Velocidad de la plataforma
    [SerializeField] Transform[] points; //Array de puntos a perseguir por la platafroma (minimo 2)
    [SerializeField] int startingPoint; //Define la posición inicial de la plataforma

    int i; //Indice numérico = número de punto a perseguir (pto actual +1 , al llegar al final 0)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        i = startingPoint; //Definir el primer punto a seguir
        //Setear la posicion inicial de la plataforma a la posición del starting point
        transform.position = points[startingPoint].position;
    }

    // Update is called once per frame
    void Update()
    {
        PlatformMovement();
    }

    void PlatformMovement()
    {
        if (Vector2.Distance(transform.position, points[i].position) < 0.02f)
        {
            i++; //Sumar 1 a 1 = definir un nuevo punto a aclanzar
            if (i == points.Length) //Chequear si i vale lo que mide el array
            {
                i = 0; //Resetea el valor de i para resetear el circuito
            }
        }
        //Mueve la plataforma a la posición que vale 1 actualmente
        //i define la balda dentro de la estanteria dentro del array que contiene una posición completa
        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            if (transform.position.y < collision.transform.position.y)
            {
                //El transform del objeto se hace hijo de la plataforma
                collision.transform.SetParent(transform);
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            //El transform del objeto tiene como padre NULL = ausencia de valor
            collision.transform.SetParent(null);
        }
    }
}
