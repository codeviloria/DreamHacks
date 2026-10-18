using UnityEngine;
using DreamHacks.World;

namespace DreamHacks.Combate
{
    // Esfera trigger hija del enemigo. Al entrar el Player, pone al enemigo en
    // modo chase (acelera hacia el). Al salir, vuelve a wander.
    [RequireComponent(typeof(CircleCollider2D))]
    public class ZonaDeteccion : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float radio = 3.5f;
        [SerializeField] private string playerTag = "Player";

        private EnemyRandomWander _wander;

        private void Reset()
        {
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = radio;
            }
        }

        private void Awake()
        {
            _wander = GetComponentInParent<EnemyRandomWander>();
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = radio;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_wander == null) return;
            if (!other.CompareTag(playerTag)) return;
            _wander.StartChase(other.transform);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_wander == null) return;
            if (!other.CompareTag(playerTag)) return;
            _wander.StopChase();
        }
    }
}
