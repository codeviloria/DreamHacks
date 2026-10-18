using UnityEngine;

namespace DreamHacks.Boss
{
    /// <summary>
    /// Proyectil de fuego del Final Boss estilo Bowser (Super Mario Bros 1).
    /// Se desplaza horizontalmente hacia el jugador con una ligera oscilación ondulatoria.
    /// Al colisionar con el jugador o contra una pared se disipa con efecto de impacto.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class BossFireball : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float speed = 7.0f;
        [SerializeField] private bool waveMotion = true;
        [SerializeField] private float waveFrequency = 10f;
        [SerializeField] private float waveAmplitude = 0.25f;
        [SerializeField] private float lifetime = 6.0f;

        [Header("Daño")]
        [SerializeField] private int damage = 20;
        [SerializeField] private float knockbackForce = 6f;

        private Vector2 _direction = Vector2.left;
        private float _spawnTime;
        private SpriteRenderer _sr;

        public void Initialize(Vector2 direction)
        {
            _direction = direction.normalized;
            _spawnTime = Time.time;

            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && _direction.x > 0)
            {
                _sr.flipX = true;
            }
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        private void Start()
        {
            _spawnTime = Time.time;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;
            Vector2 displacement = _direction * (speed * Time.deltaTime);

            if (waveMotion)
            {
                // Movimiento ondulatorio perpendicular
                Vector2 perp = new Vector2(-_direction.y, _direction.x);
                float waveOffset = Mathf.Sin(elapsed * waveFrequency) * waveAmplitude;
                displacement += perp * (waveOffset * Time.deltaTime * waveFrequency);
            }

            transform.position += (Vector3)displacement;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Ignorar el propio boss y otros proyectiles
            if (other.GetComponent<FinalBossJorge>() != null || other.GetComponent<BossFireball>() != null)
            {
                return;
            }

            // Impacto con el Player
            if (other.CompareTag("Player") || other.gameObject.name.ToLower().Contains("player"))
            {
                ApplyDamageToPlayer(other.gameObject);
                Dissipate();
                return;
            }

            // Impacto con terreno o paredes
            if (!other.isTrigger)
            {
                Dissipate();
            }
        }

        private void ApplyDamageToPlayer(GameObject player)
        {
            // Empuje si tiene Rigidbody2D
            var playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 pushDir = (player.transform.position - transform.position).normalized;
                playerRb.linearVelocity = pushDir * knockbackForce;
            }

            Debug.Log($"[BossFireball] ¡Impactó al jugador causando {damage} de daño!");
        }

        private void Dissipate()
        {
            // Efecto visual rápido de disipación o destruir
            Destroy(gameObject);
        }
    }
}
