using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Proyectil 2D: vuela recto, anima entre dos frames mientras viaja y al
    /// chocar instancia un prefab de impacto (p. ej. explosión) antes de
    /// autodestruirse. Pensado para el ataque de Iker en el metroidvania.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile2D : MonoBehaviour
    {
        [Header("Animación en vuelo")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField, Min(0.01f)] private float frameSeconds = 0.08f;

        [Header("Impacto")]
        [SerializeField] private GameObject impactPrefab;
        [Tooltip("Capas con las que el proyectil impacta además del default. Vacío = cualquier capa.")]
        [SerializeField] private LayerMask impactLayers = ~0;

        [Header("Vida")]
        [SerializeField, Min(0.1f)] private float maxLifetime = 3f;

        private Rigidbody2D _rb;
        private float _timer;
        private int _index;
        private float _spawnTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            // El proyectil no debe caer con la gravedad del metroidvania.
            _rb.gravityScale = 0f;
            _rb.linearDamping = 0f;
            _spawnTime = Time.time;
        }

        public void Launch(Vector2 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
            _rb.linearVelocity = direction.normalized * speed;
        }

        private void Update()
        {
            // Animación de vuelo (loop entre frames).
            if (flightFrames != null && flightFrames.Length > 0 && spriteRenderer != null)
            {
                _timer += Time.deltaTime;
                if (_timer >= frameSeconds)
                {
                    _timer = 0f;
                    _index = (_index + 1) % flightFrames.Length;
                    if (flightFrames[_index] != null) spriteRenderer.sprite = flightFrames[_index];
                }
            }
            // Timeout: si no choca con nada tras maxLifetime, se destruye silencioso.
            if (Time.time - _spawnTime > maxLifetime) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other) { TryImpact(other); }
        private void OnCollisionEnter2D(Collision2D collision) { TryImpact(collision.collider); }

        private void TryImpact(Collider2D other)
        {
            // Ignora al propio Player (que suele ser quien lo disparó).
            if (other.CompareTag("Player")) return;
            if (((1 << other.gameObject.layer) & impactLayers) == 0) return;

            if (impactPrefab != null)
            {
                Instantiate(impactPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
