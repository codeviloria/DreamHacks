using System;
using UnityEngine;

namespace DreamHacks.Dialogue
{
    /// <summary>
    /// Una línea de diálogo serializable.
    /// Se usa como elemento de la lista en DialogueScriptableObject.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        // Nombre que aparece arriba de la caja de diálogo.
        [SerializeField] private string speakerName;

        // Retrato placeholder. Puede ser null; en ese caso se muestra el
        // recuadro vacío.
        [SerializeField] private Sprite portrait;

        // Texto a mostrar con efecto máquina de escribir.
        [TextArea(2, 6)]
        [SerializeField] private string text;

        public string SpeakerName => speakerName;
        public Sprite Portrait => portrait;
        public string Text => text;
    }
}
