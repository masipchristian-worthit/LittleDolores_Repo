using UnityEngine;
using UnityEngine.EventSystems;

public class DVDLogoUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public RectTransform uiImage;
    public RectTransform container;

    [Header("Movimiento")]
    public float moveSpeed = 300f; // Velocidad en unidades por segundo

    [Header("Audio")]
    public int clickSFXIndex = 0;

    private Vector2 direction;

    private void Start()
    {
        if (uiImage == null || container == null)
        {
            Debug.LogError("Asigna uiImage y container en el inspector!");
            return;
        }

        // Dirección inicial aleatoria normalizada
        direction = Random.insideUnitCircle.normalized;

        // Asegurarse de que la posición inicial esté dentro del contenedor
        Vector2 startPos = new Vector2(
            Random.Range(uiImage.rect.width / 2f, container.rect.width - uiImage.rect.width / 2f),
            Random.Range(uiImage.rect.height / 2f, container.rect.height - uiImage.rect.height / 2f)
        );
        uiImage.anchoredPosition = startPos;
    }

    private void Update()
    {
        if (uiImage == null) return;

        // Mover imagen
        Vector2 newPos = uiImage.anchoredPosition + direction * moveSpeed * Time.deltaTime;

        float halfWidth = uiImage.rect.width / 2f;
        float halfHeight = uiImage.rect.height / 2f;

        float leftBound = halfWidth;
        float rightBound = container.rect.width - halfWidth;
        float bottomBound = halfHeight;
        float topBound = container.rect.height - halfHeight;

        // Rebote horizontal
        if (newPos.x < leftBound)
        {
            newPos.x = leftBound;
            direction.x *= -1;
        }
        else if (newPos.x > rightBound)
        {
            newPos.x = rightBound;
            direction.x *= -1;
        }

        // Rebote vertical
        if (newPos.y < bottomBound)
        {
            newPos.y = bottomBound;
            direction.y *= -1;
        }
        else if (newPos.y > topBound)
        {
            newPos.y = topBound;
            direction.y *= -1;
        }

        uiImage.anchoredPosition = newPos;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSFXIndex);
        }
    }
}
