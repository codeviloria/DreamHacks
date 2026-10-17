using System.Collections.Generic;
using UnityEngine;

namespace DreamHacks.Dialogue
{
    /// <summary>
    /// Diálogo como ScriptableObject: una lista ordenada de líneas.
    /// Permite que el equipo edite los diálogos sin tocar código.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "DreamHacks/Dialogue")]
    public class DialogueAsset : ScriptableObject
    {
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        public IReadOnlyList<DialogueLine> Lines => lines;
    }
}
