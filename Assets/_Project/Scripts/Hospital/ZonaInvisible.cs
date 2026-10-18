using UnityEngine;

// Utilidad: oculta el sprite (los Square blancos) en Play y garantiza un BoxCollider2D.
public abstract class ZonaInvisible : MonoBehaviour
{
    public string tagPlayer = "Player";
    protected BoxCollider2D col;

    protected virtual void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = false; // invisible en juego, visible en el editor para ubicarlo
        col = GetComponent<BoxCollider2D>();
        if (!col) col = gameObject.AddComponent<BoxCollider2D>(); // toma el tamaño del sprite
    }

    protected bool EsPlayer(Collider2D c) => c.CompareTag(tagPlayer) || c.attachedRigidbody && c.attachedRigidbody.CompareTag(tagPlayer);
}
