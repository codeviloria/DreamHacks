using UnityEngine;
using DreamHacks.Dialogue;

namespace DreamHacks.World
{
    /// <summary>
    /// NPC básico de caja blanca. Puede llevar:
    ///   - dialogueNode: diálogo con bifurcaciones (prioridad si está asignado).
    ///   - dialogue:     diálogo lineal clásico (fallback).
    /// Interact() lanza el que corresponda.
    /// </summary>
    public class NPC : MonoBehaviour
    {
        [SerializeField] private DialogueNode dialogueNode;
        [SerializeField] private DialogueAsset dialogue;

        [SerializeField] private string npcName = "NPC";

        public string NpcName => npcName;

        public void Interact()
        {
            if (DialogueUI.Instance == null)
            {
                Debug.LogWarning("No hay DialogueUI en la escena");
                return;
            }
            if (dialogueNode != null)
            {
                DialogueUI.Instance.Show(dialogueNode);
                return;
            }
            if (dialogue != null)
            {
                DialogueUI.Instance.Show(dialogue);
                return;
            }
            Debug.LogWarning($"NPC '{npcName}' sin diálogo asignado");
        }
    }
}
