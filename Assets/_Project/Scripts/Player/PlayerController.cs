using UnityEngine;
using UnityEngine.InputSystem;
using DreamHacks.World;

namespace DreamHacks.PlayerNS
{
    /// <summary>
    /// Movimiento top-down en 8 direcciones con el nuevo Input System.
    /// Lee la acción "Move" del action map "Player" del asset del proyecto
    /// (Assets/Settings/InputSystem_Actions.inputactions) para no acoplar
    /// el prefab a un PlayerInput específico.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // Velocidad en unidades/segundo. 1 unidad = 1 tile = 16 px.
        [SerializeField] private float moveSpeed = 2.6f;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        // Última dirección mirada. Se expone a otros sistemas (interacción,
        // futuras animaciones) para saber hacia dónde apunta el player.
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private Rigidbody2D _rb;
        private InputAction _moveAction;
        private Vector2 _moveInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // El asset del proyecto es el default global en Unity 6+. Si está
            // asignado en Project Settings → Input System Package → Project-wide
            // Input Actions, aparece como InputSystem.actions.
            var asset = InputSystem.actions;
            if (asset != null)
            {
                _moveAction = asset.FindAction("Player/Move");
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
        }

        private void Update()
        {
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }
            else
            {
                Vector2 input = Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                }
                var gp = Gamepad.current;
                if (gp != null)
                {
                    Vector2 stick = gp.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.05f) input = stick;
                }
                _moveInput = input;
            }

            // Clamp a magnitud 1 para que la diagonal no sea más rápida.
            if (_moveInput.sqrMagnitude > 1f) _moveInput = _moveInput.normalized;

            // Guardar la última dirección mirada cuando hay input real.
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = _moveInput.normalized;
            }
        }

        private void FixedUpdate()
        {
            // Si algún sistema (cámara, diálogo) nos bloqueó, nos quedamos quietos.
            var v = GameSignals.InputBlocked ? Vector2.zero : _moveInput * moveSpeed;
            _rb.linearVelocity = v;
        }
    }
}
