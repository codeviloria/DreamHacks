using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamHacks.UI
{
    /// <summary>
    /// Pantalla "How To Play" que se muestra al iniciar la escena.
    /// Se cierra con cualquier tecla / botón del gamepad / clic.
    /// Se puede volver a mostrar presionando F1 (Help).
    /// </summary>
    public class HowToPlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private bool showOnStart = true;

        private InputAction _toggleAction;

        private void Awake()
        {
            if (panel != null) panel.SetActive(showOnStart);
            // Toggle manual con H (Help). F1 chocaba con el atajo de ayuda de GNOME.
            // Añadimos también Select del gamepad por accesibilidad desde el mando.
            _toggleAction = new InputAction("ToggleHelp");
            _toggleAction.AddBinding("<Keyboard>/h");
            _toggleAction.AddBinding("<Gamepad>/select");
            _toggleAction.performed += ctx => Toggle();
            _toggleAction.Enable();
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;

            // Cierra con cualquier interacción.
            var kb = Keyboard.current;
            var ms = Mouse.current;
            var gp = Gamepad.current;
            bool anyKey = (kb != null && kb.anyKey.wasPressedThisFrame)
                       || (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame))
                       || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                       || gp.buttonEast.wasPressedThisFrame
                                       || gp.startButton.wasPressedThisFrame));
            if (anyKey) panel.SetActive(false);
        }

        public void Toggle()
        {
            if (panel != null) panel.SetActive(!panel.activeSelf);
        }

        private void OnDestroy()
        {
            if (_toggleAction != null) _toggleAction.Dispose();
        }
    }
}
