using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Enemigo que camina en línea recta en una dirección aleatoria durante
    /// unos segundos; cuando choca con algo o se acaba el tiempo, elige una
    /// nueva dirección. No persigue al player (jam MVP).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyWanderer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.6f;
        [SerializeField] private float minLegDuration = 1.0f;
        [SerializeField] private float maxLegDuration = 2.5f;

        private Rigidbody2D _rb;
        private Vector2 _direction;
        private float _legEndTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            PickNewDirection();
        }

        private void FixedUpdate()
        {
            if (Time.time >= _legEndTime) PickNewDirection();
            _rb.linearVelocity = _direction * moveSpeed;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Al chocar reflejamos la dirección contra la normal para no quedarnos
            // pegados a la pared, y re-sorteamos el tiempo de la próxima pierna.
            if (collision.contactCount > 0)
            {
                var n = collision.GetContact(0).normal;
                _direction = Vector2.Reflect(_direction, n).normalized;
            }
            else
            {
                PickNewDirection();
            }
            _legEndTime = Time.time + Random.Range(minLegDuration, maxLegDuration);
        }

        private void PickNewDirection()
        {
            // Random.insideUnitCircle garantiza magnitud ≤ 1; normalizamos.
            var d = Random.insideUnitCircle;
            if (d.sqrMagnitude < 0.01f) d = Vector2.right;
            _direction = d.normalized;
            _legEndTime = Time.time + Random.Range(minLegDuration, maxLegDuration);
        }
    }
}
