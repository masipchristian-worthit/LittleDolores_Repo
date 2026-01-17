using UnityEngine;
using System.Collections;

public class PlayerDashEffect : MonoBehaviour
{
    [Header("Configuración Rastro")]
    [SerializeField] float ghostInterval = 0.05f; // Cada cuánto sale una copia
    [SerializeField] float fadeTime = 0.5f;       // Cuánto tarda en desaparecer
    
    [Tooltip("1 = Opaco, 0 = Invisible. Ajusta qué tan transparente nace el fantasma.")]
    [Range(0f, 1f)] [SerializeField] float ghostInitialAlpha = 0.7f; // Nuevo: Transparencia inicial del sprite

    // Llamado por tu script de movimiento cuando pulsas Dash
    public void ShowDashTrail(float duration)
    {
        StartCoroutine(DashTrailRoutine(duration));
    }

    IEnumerator DashTrailRoutine(float duration)
    {
        float time = 0;
        while(time < duration)
        {
            CreateGhost();
            time += ghostInterval;
            yield return new WaitForSeconds(ghostInterval);
        }
    }

    void CreateGhost()
    {
        // Crear el objeto fantasma
        GameObject ghost = new GameObject("GhostTrail");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        SpriteRenderer mySr = GetComponent<SpriteRenderer>();
        
        if (mySr != null)
        {
            // Copiar el sprite exacto (frame de animación actual)
            sr.sprite = mySr.sprite;
            
            // Usar color blanco (1,1,1) para mantener los colores originales del sprite,
            // y aplicar la transparencia inicial deseada.
            sr.color = new Color(1f, 1f, 1f, ghostInitialAlpha);
            
            // Copiar orientación
            sr.flipX = mySr.flipX;
            
            // Asegurar que se dibuja detrás del jugador real
            sr.sortingLayerID = mySr.sortingLayerID;
            sr.sortingOrder = mySr.sortingOrder - 1; 
        }

        StartCoroutine(FadeOutGhost(sr, ghost));
    }

    IEnumerator FadeOutGhost(SpriteRenderer sr, GameObject obj)
    {
        float t = 0;
        // Guardamos el color con el que nació (que tiene los colores del sprite + el alfa inicial)
        Color startColor = sr.color;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            // Interpolamos solo el valor Alfa desde el inicial hasta 0
            float newAlpha = Mathf.Lerp(startColor.a, 0f, t / fadeTime);
            
            // Mantenemos los colores RGB originales y actualizamos el Alfa
            sr.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }
        // Destruir el fantasma al terminar
        Destroy(obj);
    }
}