using UnityEngine;

public class triggerBossAttack : MonoBehaviour
{
    // Prefabs de los ataques
    public GameObject action1Prefab; // se mueve de derecha a izquierda
    public GameObject action2Prefab; // cae sobre el jugador
    public GameObject action3Prefab; // spawn fijo (teletransporte)

    // Spawn fijo para acción 3
    public Transform spawn3;

    // Referencia al jugador
    public Transform playerTransform;

    // Destino del teletransporte
    public Transform teleportDestino;

    // Velocidad de la acción 1
    public float attack1Speed = 10f;

    // Variables para Attack2
    public float alturaSobreJugador = 5f;  // altura sobre el jugador
    public float attack2Speed = 5f;        // velocidad de caída

    // Textos de UI para cada ataque
    public GameObject textoAttack1;
    public GameObject textoAttack2;
    public GameObject textoAttack3;

    // Tiempo que se muestra el texto
    public float tiempoTexto = 1f;

    // Controla que solo se active 1 vez por entrada
    private bool inside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (inside) return;

        inside = true;

        // 50% de que no pase nada
        if (Random.Range(0, 2) == 0)
        {
            Debug.Log("No pasa nada");
            return;
        }

        // Elegir ataque (0,1,2)
        int choice = Random.Range(0, 3);
        Debug.Log("Ocurre acción: " + choice);

        if (choice == 0) Attack1();
        else if (choice == 1) Attack2();
        else if (choice == 2) Attack3();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inside = false; // permite activar de nuevo al volver a entrar
    }

    // ===================== ATAQUES =====================

    // ATAQUE 1: derecha del jugador y va a la izquierda
    void Attack1()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("playerTransform no asignado");
            return;
        }

        MostrarTexto(textoAttack1);

        Vector3 spawnPos = playerTransform.position + Vector3.right * 5f;
        GameObject obj = Instantiate(action1Prefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.left * attack1Speed;
        }

        // Apagar después de 1 segundo
        StartCoroutine(DesactivarDespues(obj, 1f));
    }

    // ATAQUE 2: cae sobre el jugador
    public void Attack2()
    {
        if (action2Prefab == null || playerTransform == null) return;

        MostrarTexto(textoAttack2);

        Vector3 spawnPos = new Vector3(
            playerTransform.position.x,
            playerTransform.position.y + alturaSobreJugador,
            0f
        );

        GameObject obj = Instantiate(action2Prefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.down * attack2Speed;
        }

        // Apagar después de 1 segundo
        StartCoroutine(DesactivarDespues(obj, 1f));
    }

    // ATAQUE 3: spawn fijo con delay + teletransporte
    void Attack3()
    {
        if (spawn3 == null || action3Prefab == null) return;

        MostrarTexto(textoAttack3);
        StartCoroutine(Attack3ConDelay());
    }

    System.Collections.IEnumerator Attack3ConDelay()
    {
        // Espera medio segundo antes de aparecer
        yield return new WaitForSeconds(0.5f);

        GameObject obj = Instantiate(action3Prefab, spawn3.position, Quaternion.identity);

        // Asignar destino de teletransporte al prefab
        AttackTp tp = obj.GetComponent<AttackTp>();
        if (tp != null)
        {
            tp.destino = teleportDestino;
        }
        else
        {
            Debug.LogWarning("El prefab del ataque 3 no tiene el script TeleportOnTrigger");
        }

        // Se mantiene activo medio segundo
        yield return new WaitForSeconds(0.5f);

        if (obj != null)
            obj.SetActive(false);
    }

    // ===================== TEXTO UI =====================

    void MostrarTexto(GameObject texto)
    {
        if (texto == null) return;
        StartCoroutine(MostrarTextoCoroutine(texto));
    }

    System.Collections.IEnumerator MostrarTextoCoroutine(GameObject texto)
    {
        texto.SetActive(true);
        yield return new WaitForSeconds(tiempoTexto);
        texto.SetActive(false);
    }

    // ===================== UTILIDADES =====================

    // Coroutine para apagar un objeto después de un tiempo
    System.Collections.IEnumerator DesactivarDespues(GameObject obj, float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        if (obj != null) obj.SetActive(false);
    }
}
