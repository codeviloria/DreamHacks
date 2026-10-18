using UnityEngine;
using DreamHacks.Combate;
using DreamHacks.UI;

namespace DreamHacks.World
{
    // Trigger que al tocar al Player, agrega una pocion al inventario, dispara
    // un dialogo pequeño con la cara de Iker explicando el efecto, y se destruye.
    [RequireComponent(typeof(Collider2D))]
    public class PocionPickup : MonoBehaviour
    {
        [SerializeField] private TipoPocion tipo = TipoPocion.Roja;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Sprite portraitIker;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            var inv = other.GetComponent<Inventario>();
            if (inv == null) inv = other.GetComponentInParent<Inventario>();
            // Self-healing: si el Player no tiene el componente (puede faltar si no
            // se agregó al prefab/escena), lo creamos en runtime para que el pickup
            // nunca falle en silencio. Enventario es stateless: persiste en BattleState.
            if (inv == null) inv = other.gameObject.AddComponent<Inventario>();
            if (inv == null) return;
            inv.Agregar(tipo);
            DialogoPocion.Mostrar(tipo, portraitIker, 2.8f);
            Destroy(gameObject);
        }
    }
}
