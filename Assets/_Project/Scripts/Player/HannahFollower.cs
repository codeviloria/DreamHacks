using System.Collections.Generic;
using UnityEngine;

namespace DreamHacks.PlayerNS
{
    /// <summary>
    /// Controlador para Hannah que la hace seguir suavemente a Iker en el overworld
    /// actualizando sus sprites direccionales según el movimiento.
    /// </summary>
    public class HannahFollower : MonoBehaviour
    {
        [Header("Target & Distancia")]
        [SerializeField] private Transform target;
        [SerializeField] private float followDistance = 0.72f;
        [SerializeField] private float moveSpeed = 2.7f;
        [SerializeField] private float stopDistance = 0.55f;

        [Header("Sprites Direccionales")]
        [SerializeField] private Sprite spriteFrontal;
        [SerializeField] private Sprite spriteDetras;
        [SerializeField] private Sprite spriteIzquierda;
        [SerializeField] private Sprite spriteDerecha;
        [SerializeField] private Sprite spriteAfk;

        private SpriteRenderer _sr;
        private Rigidbody2D _rb;
        private Vector2 _lastMoveDir = Vector2.down;
        private bool _isMoving;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public void SetSprites(Sprite frontal, Sprite detras, Sprite izq, Sprite der, Sprite afk)
        {
            spriteFrontal = frontal;
            spriteDetras = detras;
            spriteIzquierda = izq;
            spriteDerecha = der;
            spriteAfk = afk;
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.gravityScale = 0f;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target == null || _sr == null) return;

            Vector2 currentPos = transform.position;
            Vector2 targetPos = target.position;
            float dist = Vector2.Distance(currentPos, targetPos);

            if (dist > followDistance)
            {
                _isMoving = true;
                Vector2 dir = (targetPos - currentPos).normalized;
                _lastMoveDir = dir;

                float step = moveSpeed * Time.deltaTime;
                Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, step);
                transform.position = new Vector3(newPos.x, newPos.y, 0f);

                UpdateDirectionSprite(dir);
            }
            else if (dist <= stopDistance)
            {
                _isMoving = false;
                if (spriteAfk != null)
                    _sr.sprite = spriteAfk;
                else if (spriteFrontal != null)
                    _sr.sprite = spriteFrontal;
            }
        }

        private void UpdateDirectionSprite(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                if (dir.x > 0.1f && spriteDerecha != null)
                    _sr.sprite = spriteDerecha;
                else if (dir.x < -0.1f && spriteIzquierda != null)
                    _sr.sprite = spriteIzquierda;
            }
            else
            {
                if (dir.y > 0.1f && spriteDetras != null)
                    _sr.sprite = spriteDetras;
                else if (dir.y < -0.1f && spriteFrontal != null)
                    _sr.sprite = spriteFrontal;
            }
        }
    }
}
