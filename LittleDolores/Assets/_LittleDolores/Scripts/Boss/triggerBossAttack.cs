using UnityEngine;
using System.Collections;

public class triggerBossAttack : MonoBehaviour
{
    [Header("Prefabs de ataques")]
    public GameObject action1Prefab; // se mueve de derecha a izquierda
    public GameObject action2Prefab; // cae sobre el jugador
    public GameObject action3Prefab; // spawn fijo (teletransporte)

    [Header("Spawn y referencias")]
    public Transform spawn3;             // Spawn fijo para acción 3
    public Transform playerTransform;    // Referencia al jugador
    public Transform teleportDestino;    // Destino de teletransporte

    [Header("Velocidades y altura")]
    public float attack1Speed = 10f;
    public float attack2Speed = 5f;
    public float alturaSobreJugador = 5f;

    [Header("UI")]
    public GameObject textoAttack1;
    public GameObject textoAttack2;
    public GameObject textoAttack3;
    public float tiempoTexto = 1f;

    [Header("Daño de ataques")]
    public int dañoAttack1 = 1; // Ataque 1 y 3 comparten el mismo daño

    private bool inside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || inside) return;

        inside = true;

        // 50% de que no pase nada
        if (Random.Range(0, 2) == 0)
        {
            Debug.Log("No pasa nada");
            return;
        }

        int choice = Random.Range(0, 3);
        Debug.Log("Ocurre acción: " + choice);

        if (choice == 0) Attack1();
        else if (choice == 1) Attack2();
        else if (choice == 2) Attack3();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inside = false;
    }

    // ===================== ATAQUES =====================

    // ATAQUE 1: derecha del jugador, va a la izquierda
    void Attack1()
    {
        if (playerTransform == null) return;

        MostrarTexto(textoAttack1);

        Vector3 spawnPos = playerTransform.position + Vector3.right * 5f;
        GameObject obj = Instantiate(action1Prefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.left * attack1Speed; // corregido
        }

        // Asignar daño y tiempo de apagado
        Attack1Damage ad = obj.GetComponent<Attack1Damage>();
        if (ad != null)
        {
            ad.damage = dañoAttack1;
            ad.autoDeactivateTime = 2f; // se apaga solo si no colisiona
        }
    }

    // ATAQUE 2: cae sobre el jugador
    void Attack2()
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
            rb.linearVelocity = Vector2.down * attack2Speed; // corregido
        }

        StartCoroutine(DesactivarDespues(obj, 1f));
    }

    // ATAQUE 3: spawn fijo con delay + teletransporte, ahora también hace daño
    void Attack3()
    {
        if (spawn3 == null || action3Prefab == null) return;

        MostrarTexto(textoAttack3);
        StartCoroutine(Attack3ConDelay());
    }

    IEnumerator Attack3ConDelay()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject obj = Instantiate(action3Prefab, spawn3.position, Quaternion.identity);

        // Asignar destino de teletransporte
        AttackTp tp = obj.GetComponent<AttackTp>();
        if (tp != null)
            tp.destino = teleportDestino;
        else
            Debug.LogWarning("El prefab del ataque 3 no tiene el script AttackTp");

        // Asignar daño (igual que ataque 1)
        Attack1Damage ad = obj.GetComponent<Attack1Damage>();
        if (ad != null)
        {
            ad.damage = dañoAttack1;
            ad.autoDeactivateTime = 0.5f; // dura poco, como antes
        }

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

    IEnumerator MostrarTextoCoroutine(GameObject texto)
    {
        texto.SetActive(true);
        yield return new WaitForSeconds(tiempoTexto);
        texto.SetActive(false);
    }

    // ===================== UTILIDADES =====================
    IEnumerator DesactivarDespues(GameObject obj, float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        if (obj != null) obj.SetActive(false);
    }
}
