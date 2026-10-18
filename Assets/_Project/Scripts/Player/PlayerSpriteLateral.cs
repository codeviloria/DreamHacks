using UnityEngine;

namespace DreamHacks.PlayerNS
{
    /// <summary>
    /// Cambia el sprite del Player en el metroidvania según la dirección
    /// horizontal del movimiento. Usa dos pares de frames (izquierda/derecha)
    /// y los alterna para una animación simple de caminar en el suelo.
    /// Lee la velocidad del Rigidbody2D del padre (si existe) o del propio
    /// GameObject, para funcionar tanto en el root como en un hijo "Visual".
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerSpriteLateral : MonoBehaviour
    {
        [System.Serializable]
        public class DirectionFrames
        {
            public Sprite frame0;
            public Sprite frame1;
        }

        [SerializeField] private DirectionFrames left;
        [SerializeField] private DirectionFrames right;

        [SerializeField] private float walkFrameSeconds = 0.14f;
        [SerializeField] private float moveThreshold = 0.1f;

        private SpriteRenderer _sr;
        private Rigidbody2D _rb;
        private PlayerAttack2D _attack;
        private DirectionFrames _lastFrames;
        private float _timer;
        private int _frameIndex;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponentInParent<Rigidbody2D>();
            // PlayerAttack2D vive en el mismo GameObject (Visual).
            _attack = GetComponent<PlayerAttack2D>();
            _lastFrames = right;
        }

        private void Update()
        {
            // Mientras hay un ataque en curso, PlayerAttack2D es el dueño del
            // sprite y del flipX; no debemos pisarlo o se ve un frame del
            // sprite lateral en medio de la animación de disparo.
            if (_attack != null && _attack.IsAttacking) return;

            float vx = _rb != null ? _rb.linearVelocity.x : 0f;
            bool moving = Mathf.Abs(vx) > moveThreshold;

            if (moving)
            {
                _lastFrames = vx > 0f ? right : left;
                _timer += Time.deltaTime;
                if (_timer >= walkFrameSeconds)
                {
                    _timer = 0f;
                    _frameIndex = 1 - _frameIndex;
                }
            }
            else
            {
                // Quieto: fijamos frame 0 para que no "pise en el aire".
                _frameIndex = 0;
                _timer = 0f;
            }

            if (_lastFrames == null) return;
            var sprite = (_frameIndex == 0) ? _lastFrames.frame0 : (_lastFrames.frame1 != null ? _lastFrames.frame1 : _lastFrames.frame0);
            if (sprite != null) _sr.sprite = sprite;
        }
    }
}
