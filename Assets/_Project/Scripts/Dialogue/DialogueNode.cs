using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamHacks.Dialogue
{
    /// <summary>
    /// Una opción que puede aparecer al final de un nodo de diálogo.
    /// Si "next" es null, al elegir esta opción se cierra el diálogo.
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        [SerializeField] private string text;
        [SerializeField] private DialogueNode next;

        public string Text => text;
        public DialogueNode Next => next;
    }

    /// <summary>
    /// Nodo de diálogo con bifurcaciones.
    /// Si tiene "choices" → al terminar la línea se muestran como botones y el
    /// jugador elige por dónde continuar el lore.
    /// Si no tiene choices → al terminar la línea sigue con "next" (o cierra
    /// si next es null). Pensado para encadenar réplicas: NPC → Player → NPC.
    /// </summary>
    [CreateAssetMenu(fileName = "DlgNode", menuName = "DreamHacks/Dialogue Node")]
    public class DialogueNode : ScriptableObject
    {
        [SerializeField] private string speakerName;
        [SerializeField] private Sprite portrait;
        [TextArea(2, 6)] [SerializeField] private string text;

        [Tooltip("Si es true, la línea sale del Protagonista (retrato derecho); si no, de un NPC (retrato izquierdo).")]
        [SerializeField] private bool isPlayer;

        [Header("Flujo")]
        [Tooltip("Opciones de elección. Si está vacía se usa 'next' como continuación lineal.")]
        [SerializeField] private List<DialogueChoice> choices = new List<DialogueChoice>();

        [Tooltip("Siguiente nodo cuando no hay opciones. Dejar vacío para cerrar.")]
        [SerializeField] private DialogueNode next;

        public string SpeakerName => speakerName;
        public Sprite Portrait => portrait;
        public string Text => text;
        public bool IsPlayer => isPlayer;
        public IReadOnlyList<DialogueChoice> Choices => choices;
        public DialogueNode Next => next;
        public bool HasChoices => choices != null && choices.Count > 0;
    }
}
