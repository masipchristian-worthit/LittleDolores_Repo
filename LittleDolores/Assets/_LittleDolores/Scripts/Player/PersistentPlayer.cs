using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance;

    void Awake()
    {
        // Sistema Singleton para que no se destruya
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // 1. Nos suscribimos al evento de carga de escena al activarse
    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    // 2. Nos desuscribimos al desactivarse (buena pr�ctica)
    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    // 3. Esta funci�n se ejecuta SOLA en cuanto la nueva escena est� lista
    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Buscamos el objeto vac�o que pusiste en la escena
        GameObject puntoDeSpawn = GameObject.Find("PlayerSpawnPoint");

        if (puntoDeSpawn != null)
        {
            // Movemos al player a la posici�n del spawn
            transform.position = puntoDeSpawn.transform.position;

            // Si usas Rigidbody2D, reseteamos la velocidad para que no salga disparado
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            Debug.LogWarning("No encontr� un objeto llamado 'PlayerSpawnPoint' en esta escena.");
        }
    }
}