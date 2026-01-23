using UnityEngine;

public class MovingPlataform2D : MonoBehaviour
{
    [Header("Waypoints & movement Configuration")]
    [SerializeField] float speed; //Velocidad de la plataforma
    [SerializeField] Transform[] points; //Array de puntos a persseguir por una plataforma (minimo 2)
    [SerializeField] int startingPoint; //velocidad inicial de la plataforma

    int i; //indice numerico = numero de punto a perseguir (punto actual +1, al llegar ak final 0)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        i = startingPoint; //Definir el primer punto a perseguir
        //setear la poscion inicial de la plataforma a la posicion del starting point
        transform.position = points[startingPoint].position;


    }

    // Update is called once per frame
    void Update()
    {


        PlataformMovement();

    }

    void PlataformMovement()
    {

        if (Vector2.Distance(transform.position, points[i].position) < 0.02f)
        {
            i++; //sumar i a i = definir un nuevo punt a alcanzar
            if (i == points.Length) //chequear si i vale lo que mide el array
            {
                i = 0; //Resetea el valor de i para resetear ek circuito            

            }
        }

        //Mueve la plataforma a la posicion que vale i actualmente
        //i define la "balda dentro de la estanteria" del array que contiene una posicion correcta
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

            //el transform del objeto tiene como padre NULL = ausencia de valor
            collision.transform.SetParent(null);


        }
    }
}
