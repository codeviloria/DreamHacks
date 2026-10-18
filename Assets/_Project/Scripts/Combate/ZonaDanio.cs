using UnityEngine;

namespace DreamHacks.Combate
{
    // Trigger que inflige danio en cuartos al Player cuando entra.
    [RequireComponent(typeof(Collider2D))]
    public class ZonaDanio : MonoBehaviour
    {
        [SerializeField, Min(1)] private int cuartos = 1;
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            var vida = other.GetComponent<VidaJugador>();
            if (vida == null) vida = other.GetComponentInParent<VidaJugador>();
            if (vida != null) vida.RecibirDanio(cuartos);
        }
    }
}
