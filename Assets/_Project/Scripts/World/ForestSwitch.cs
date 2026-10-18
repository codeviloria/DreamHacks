using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamHacks.World
{
    /// <summary>
    /// Interruptor interactivo en el bosque que abre la salida.
    /// Cambia de rojo (apagado) a cian luminoso (encendido) al interactuar o pisarlo.
    /// Completa la tarea 2 del bosque y desactiva la barrera de salida.
    /// </summary>
    public class ForestSwitch : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite switchOffSprite;
        [SerializeField] private Sprite switchOnSprite;

        [Header("Objetos de Salida")]
        [SerializeField] private GameObject exitBarrierObject;
        [SerializeField] private float interactDistance = 1.2f;

        public event Action OnSwitchActivated;

        private SpriteRenderer _sr;
        private bool _isActivated;
        private Transform _playerTransform;

        public bool IsActivated => _isActivated;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && switchOffSprite != null)
                _sr.sprite = switchOffSprite;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void Update()
        {
            if (_isActivated) return;

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
                bool pressed = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                            || dist <= 0.65f; // También se activa si se pisa directamente

                if (pressed)
                {
                    ActivateSwitch();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isActivated) return;
            if (other.CompareTag("Player"))
            {
                ActivateSwitch();
            }
        }

        public void ActivateSwitch()
        {
            if (_isActivated) return;
            _isActivated = true;

            if (_sr != null && switchOnSprite != null)
                _sr.sprite = switchOnSprite;

            if (exitBarrierObject != null)
                exitBarrierObject.SetActive(false);

            OnSwitchActivated?.Invoke();
        }
    }
}
