using UnityEngine;
using UnityEngine.InputSystem;
using DreamHacks.World;
using DreamHacks.Dialogue;

namespace DreamHacks.PlayerNS
{
    /// <summary>
    /// Detecta NPCs cercanos (los que entran en el trigger de este collider)
    /// y, al pulsar la acción Player/Interact, llama Interact() sobre el último
    /// NPC en rango. Pensado como child del Player con un CircleCollider2D
    /// isTrigger un poco más grande que el player.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        private NPC _nearby;
        private InputAction _interactAction;

        private void Awake()
        {
            var actions = InputSystem.actions;
            if (actions != null) _interactAction = actions.FindAction("Player/Interact");
        }

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.started += OnInteract;
                _interactAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null) _interactAction.started -= OnInteract;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var npc = other.GetComponentInParent<NPC>();
            if (npc != null) _nearby = npc;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var npc = other.GetComponentInParent<NPC>();
            if (npc != null && npc == _nearby) _nearby = null;
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (_nearby == null) return;
            // Si el diálogo ya está abierto, dejamos que la UI maneje el avance.
            if (DialogueUI.Instance != null && DialogueUI.Instance.IsActive) return;
            _nearby.Interact();
        }
    }
}
