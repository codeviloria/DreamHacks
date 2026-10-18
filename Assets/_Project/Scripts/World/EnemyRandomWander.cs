using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Movimiento aleatorio sencillo para enemigos placeholder: cada
    /// <see cref="intervaloSegundos"/> elige una dirección 2D random y se
    /// mueve a <see cref="velocidad"/>. Si choca con una pared, invierte
    /// la dirección en el próximo tick. Opcionalmente se mantiene dentro
    /// de un bounds en coordenadas de mundo para no "escaparse" del mapa.
    /// Requiere Rigidbody2D (Dynamic, gravity 0) + Collider2D en el GO.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyRandomWander : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float velocidad = 1.2f;
        [SerializeField, Min(0.1f)] private float velocidadChase = 2.4f;
        [SerializeField, Min(0.2f)] private float intervaloSegundos = 1.5f;

        [Header("Límites del mapa (mundo)")]
        [SerializeField] private Vector2 minBounds = new Vector2(-60f, -22f);
        [SerializeField] private Vector2 maxBounds = new Vector2(60f, 22f);
        [SerializeField] private bool usarBounds = true;

        private Rigidbody2D _rb;
        private Vector2 _dir;
        private float _timer;
        private bool _chasing;
        private Transform _chaseTarget;

        public void StartChase(Transform t)
        {
            _chaseTarget = t;
            _chasing = t != null;
        }

        public void StopChase()
        {
            _chasing = false;
            _chaseTarget = null;
            NuevaDireccion();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            // Continuous evita tunelar las paredes EdgeCollider2D del mapa.
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            NuevaDireccion();
        }

        private void FixedUpdate()
        {
            float velActual = velocidad;
            if (_chasing && _chaseTarget != null)
            {
                _dir = ((Vector2)_chaseTarget.position - _rb.position).normalized;
                velActual = velocidadChase;
            }
            else
            {
                _timer += Time.fixedDeltaTime;
                if (_timer >= intervaloSegundos)
                {
                    _timer = 0f;
                    NuevaDireccion();
                }
            }

            Vector2 vel = _dir * velActual;

            if (usarBounds)
            {
                // Si está cerca del borde del mapa, forzar la dirección hacia
                // el centro antes de mover para no salirse.
                Vector2 p = _rb.position;
                if (p.x < minBounds.x && vel.x < 0f) vel.x = Mathf.Abs(vel.x);
                if (p.x > maxBounds.x && vel.x > 0f) vel.x = -Mathf.Abs(vel.x);
                if (p.y < minBounds.y && vel.y < 0f) vel.y = Mathf.Abs(vel.y);
                if (p.y > maxBounds.y && vel.y > 0f) vel.y = -Mathf.Abs(vel.y);
                _dir = vel.normalized;
            }

            _rb.linearVelocity = vel;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Al chocar con pared/mueble, elegir nueva dirección random.
            NuevaDireccion();
        }

        private void NuevaDireccion()
        {
            float ang = Random.Range(0f, 2f * Mathf.PI);
            _dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }
    }
}
