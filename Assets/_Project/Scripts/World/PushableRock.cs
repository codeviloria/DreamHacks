using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamHacks.World
{
    /// <summary>
    /// Roca mágica que bloquea el sendero en el bosque.
    /// Al interactuar con ella [E / Espacio], se desliza hacia un lado desbloqueando el camino
    /// y completa la tarea 1 del bosque.
    /// </summary>
    public class PushableRock : MonoBehaviour
    {
        [Header("Configuración de Desplazamiento")]
        [SerializeField] private Vector3 pushOffset = new Vector3(-1.8f, 0f, 0f);
        [SerializeField] private float slideDuration = 1.0f;
        [SerializeField] private float interactDistance = 1.35f;

        [Header("Referencias")]
        [SerializeField] private Collider2D blockingCollider;

        public event Action OnRockPushed;

        private bool _isPushed;
        private bool _isMoving;
        private Transform _playerTransform;

        public bool IsPushed => _isPushed;

        private void Start()
        {
            if (blockingCollider == null)
                blockingCollider = GetComponent<Collider2D>();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void Update()
        {
            if (_isPushed || _isMoving) return;

            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
                if (_playerTransform == null) return;
            }

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist <= interactDistance)
            {
                var kb = Keyboard.current;
                bool interactPressed = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                                    || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

                if (interactPressed)
                {
                    StartCoroutine(SlideRoutine());
                }
            }
        }

        private IEnumerator SlideRoutine()
        {
            _isMoving = true;
            _isPushed = true;

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + pushOffset;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            transform.position = endPos;
            _isMoving = false;

            if (blockingCollider != null)
                blockingCollider.enabled = false;

            OnRockPushed?.Invoke();
        }
    }
}
