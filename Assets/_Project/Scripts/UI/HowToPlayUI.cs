using UnityEngine;
using UnityEngine.InputSystem;
using DreamHacks.World;
using DreamHacks.Dialogue;

namespace DreamHacks.UI
{
    /// <summary>
    /// Menú de pausa minimalista: muestra los controles y detiene el juego.
    /// Se abre/cierra con P (teclado) o Start (mando). Pausa real: pone
    /// Time.timeScale = 0 y bloquea el input del player con GameSignals.
    /// No se abre si hay un diálogo activo para no interferir con la lectura.
    /// </summary>
    public class HowToPlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private bool showOnStart = false;

        private InputAction _toggleAction;
        private bool _paused;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            _toggleAction = new InputAction("TogglePause");
            _toggleAction.AddBinding("<Keyboard>/p");
            _toggleAction.AddBinding("<Gamepad>/start");
            _toggleAction.performed += ctx => Toggle();
            _toggleAction.Enable();
        }

        private void Start()
        {
            if (showOnStart) SetPaused(true);
        }

        private void Update()
        {
            if (!_paused) return;

            // Cerrar con cualquier interacción (excepto el toggle, para no cerrar
            // y re-abrir en el mismo frame).
            var kb = Keyboard.current;
            var ms = Mouse.current;
            var gp = Gamepad.current;
            bool pressedToggle = (kb != null && (kb.pKey.wasPressedThisFrame))
                              || (gp != null && gp.startButton.wasPressedThisFrame);
            if (pressedToggle) return;

            bool anyKey = (kb != null && kb.anyKey.wasPressedThisFrame)
                       || (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame))
                       || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                       || gp.buttonEast.wasPressedThisFrame
                                       || gp.buttonNorth.wasPressedThisFrame
                                       || gp.buttonWest.wasPressedThisFrame));
            if (anyKey) SetPaused(false);
        }

        public void Toggle()
        {
            // No abrir pausa si hay un diálogo abierto; sería confuso.
            if (!_paused && DialogueUI.Instance != null && DialogueUI.Instance.IsActive) return;
            SetPaused(!_paused);
        }

        private void SetPaused(bool value)
        {
            _paused = value;
            if (panel != null) panel.SetActive(value);
            Time.timeScale = value ? 0f : 1f;
            GameSignals.InputBlocked = value;
        }

        private void OnDestroy()
        {
            if (_toggleAction != null) _toggleAction.Dispose();
            // Si nos destruyen con el juego pausado, dejar el timescale sano.
            Time.timeScale = 1f;
        }
    }
}
