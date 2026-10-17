using UnityEngine;

namespace DreamHacks.PlayerNS
{
    /// <summary>
    /// Cambia el sprite del Player según la dirección de movimiento en top-down
    /// (8 direcciones) y usa el idle AFK cuando está quieto. Lee la velocidad
    /// real del Rigidbody2D para no sincronizarse mal con bloqueos de input.
    /// Cada "slot" acepta dos frames que se alternan para una animación simple.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerSpriteDirection : MonoBehaviour
    {
        [System.Serializable]
        public class DirectionFrames
        {
            public Sprite frame0;
            public Sprite frame1;
        }

        [Header("Cardinales")]
        [SerializeField] private DirectionFrames up;
        [SerializeField] private DirectionFrames down;
        [SerializeField] private DirectionFrames left;
        [SerializeField] private DirectionFrames right;

        [Header("Diagonales frontales (abajo-der/izq)")]
        [SerializeField] private DirectionFrames downRight;
        [SerializeField] private DirectionFrames downLeft;

        [Header("Diagonales de espalda (arriba-der/izq)")]
        [SerializeField] private DirectionFrames upRight;
        [SerializeField] private DirectionFrames upLeft;

        [Header("Idle")]
        [SerializeField] private DirectionFrames afk;

        [Header("Timing")]
        [SerializeField] private float walkFrameSeconds = 0.18f;
        [SerializeField] private float idleFrameSeconds = 0.60f;
        [SerializeField] private float moveThreshold = 0.1f;
        // Un componente de la diagonal debe ser al menos este fracción del
        // dominante para contar como diagonal; evita diagonales por ruido.
        [SerializeField, Range(0.1f, 0.9f)] private float diagonalThreshold = 0.4f;

        private SpriteRenderer _sr;
        private Rigidbody2D _rb;
        private float _timer;
        private int _frameIndex;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            var vel = _rb.linearVelocity;
            bool moving = vel.sqrMagnitude > moveThreshold * moveThreshold;

            var frames = moving ? PickFromDirection(vel) : afk;
            float period = moving ? walkFrameSeconds : idleFrameSeconds;

            _timer += Time.deltaTime;
            if (_timer >= period)
            {
                _timer = 0f;
                _frameIndex = 1 - _frameIndex;
            }

            if (frames == null) return;
            var sprite = (_frameIndex == 0) ? frames.frame0 : (frames.frame1 != null ? frames.frame1 : frames.frame0);
            if (sprite != null) _sr.sprite = sprite;
        }

        private DirectionFrames PickFromDirection(Vector2 d)
        {
            float ax = Mathf.Abs(d.x);
            float ay = Mathf.Abs(d.y);
            float max = Mathf.Max(ax, ay);
            if (max <= 0.0001f) return afk;
            bool isDiagonal = Mathf.Min(ax, ay) / max >= diagonalThreshold;

            if (isDiagonal)
            {
                if (d.x > 0 && d.y > 0) return upRight;
                if (d.x < 0 && d.y > 0) return upLeft;
                if (d.x > 0 && d.y < 0) return downRight;
                return downLeft;
            }
            if (ax > ay) return d.x > 0 ? right : left;
            return d.y > 0 ? up : down;
        }
    }
}
