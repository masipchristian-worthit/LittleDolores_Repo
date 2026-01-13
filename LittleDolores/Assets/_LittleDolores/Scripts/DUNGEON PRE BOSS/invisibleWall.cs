using UnityEngine;
using UnityEngine.Tilemaps;

public class invisibleWall : MonoBehaviour

{
    private Tilemap tilemap;
    public float alphaInvisible = 0.3f;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Alpha(alphaInvisible);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Alpha(1f);
        }
    }

    private void Alpha(float alpha)
    {
        Color c = tilemap.color;
        c.a = alpha;
        tilemap.color = c;
    }
}
