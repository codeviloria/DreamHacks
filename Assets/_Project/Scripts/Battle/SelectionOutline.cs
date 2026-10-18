using UnityEngine;
using UnityEngine.EventSystems;

namespace DreamHacks.Battle
{
    // Activa un GameObject (outline) cuando este Selectable está seleccionado
    // por el EventSystem. Pensado para darle feedback visual claro al jugador.
    public class SelectionOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private GameObject _outline;

        public void Setup(GameObject outline)
        {
            _outline = outline;
            if (_outline != null) _outline.SetActive(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_outline != null) _outline.SetActive(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (_outline != null) _outline.SetActive(false);
        }
    }
}
