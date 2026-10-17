using UnityEngine;
using DreamHacks.Dialogue;

namespace DreamHacks.World
{
    /// <summary>
    /// NPC básico de caja blanca: tiene un diálogo asignado y expone un método
    /// Interact() que lo lanza en la DialogueUI. El collider trigger lo detecta
    /// el PlayerInteractor del jugador.
    /// </summary>
    public class NPC : MonoBehaviour
    {
        [SerializeField] private DialogueAsset dialogue;

        // Nombre interno para debug / futura máquina de estados.
        [SerializeField] private string npcName = "NPC";

        public string NpcName => npcName;

        public void Interact()
        {
            if (dialogue == null)
            {
                Debug.LogWarning($"NPC '{npcName}' sin diálogo asignado");
                return;
            }
            if (DialogueUI.Instance != null) DialogueUI.Instance.Show(dialogue);
            else Debug.LogWarning("No hay DialogueUI en la escena");
        }
    }
}
