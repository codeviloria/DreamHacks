using System;
using UnityEngine;
using UnityEngine.UI;
using DreamHacks.Combate;

namespace DreamHacks.Battle
{
    // Submenu de pociones durante la batalla. Lo construye BattleUI por codigo.
    // Se muestra/oculta via SetVisible. Al elegir una pocion dispara onSeleccion.
    public class PocionMenu : MonoBehaviour
    {
        private RectTransform _root;
        private Button _rojaBtn;
        private Button _azulBtn;
        private Button _rosaBtn;
        private Button _volverBtn;
        private Text _rojaTxt;
        private Text _azulTxt;
        private Text _rosaTxt;

        public event Action<TipoPocion> onSeleccion;
        public event Action onVolver;

        public Button VolverButton => _volverBtn;
        public Button RojaButton => _rojaBtn;
        public Button AzulButton => _azulBtn;
        public Button RosaButton => _rosaBtn;

        public Button PrimerBotonUsable()
        {
            if (_rojaBtn != null && _rojaBtn.interactable) return _rojaBtn;
            if (_azulBtn != null && _azulBtn.interactable) return _azulBtn;
            if (_rosaBtn != null && _rosaBtn.interactable) return _rosaBtn;
            return _volverBtn;
        }

        public void Build(Transform parent, Sprite spRoja, Sprite spAzul, Sprite spRosa)
        {
            var go = new GameObject("PocionMenu", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.92f);
            _root = (RectTransform)go.transform;
            // Encima del log, debajo del área de HUDs. No pisa los botones (0.04-0.22).
            _root.anchorMin = new Vector2(0.1f, 0.24f);
            _root.anchorMax = new Vector2(0.9f, 0.42f);
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _rojaBtn = CrearBoton(_root, "Roja", spRoja, out _rojaTxt);
            _azulBtn = CrearBoton(_root, "Azul", spAzul, out _azulTxt);
            _rosaBtn = CrearBoton(_root, "Rosa", spRosa, out _rosaTxt);
            _volverBtn = CrearBoton(_root, "Volver", null, out _);

            _rojaBtn.onClick.AddListener(() => onSeleccion?.Invoke(TipoPocion.Roja));
            _azulBtn.onClick.AddListener(() => onSeleccion?.Invoke(TipoPocion.Azul));
            _rosaBtn.onClick.AddListener(() => onSeleccion?.Invoke(TipoPocion.Rosa));
            _volverBtn.onClick.AddListener(() => onVolver?.Invoke());

            SetVisible(false);
        }

        public void Refrescar(int rojas, int azules, int rosas)
        {
            if (_rojaTxt != null) _rojaTxt.text = "Roja\nx" + rojas;
            if (_azulTxt != null) _azulTxt.text = "Azul\nx" + azules;
            if (_rosaTxt != null) _rosaTxt.text = "Rosa\nx" + rosas;
            if (_rojaBtn != null) _rojaBtn.interactable = rojas > 0;
            if (_azulBtn != null) _azulBtn.interactable = azules > 0;
            if (_rosaBtn != null) _rosaBtn.interactable = rosas > 0;
        }

        public void SetVisible(bool v)
        {
            if (_root != null) _root.gameObject.SetActive(v);
        }

        private static Button CrearBoton(Transform parent, string label, Sprite icono, out Text txt)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            cb.pressedColor = new Color(0.6f, 0.8f, 1f, 1f);
            cb.selectedColor = new Color(0.4f, 0.9f, 1f, 1f);
            cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            // outline hijo
            var outlineGO = new GameObject("Outline", typeof(Image));
            outlineGO.transform.SetParent(go.transform, false);
            var oImg = outlineGO.GetComponent<Image>();
            oImg.color = new Color(1f, 0.9f, 0.2f, 1f);
            oImg.raycastTarget = false;
            var oRt = oImg.rectTransform;
            oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
            oRt.offsetMin = new Vector2(-3f, -3f); oRt.offsetMax = new Vector2(3f, 3f);
            outlineGO.transform.SetAsFirstSibling();
            outlineGO.SetActive(false);
            go.AddComponent<SelectionOutline>().Setup(outlineGO);

            if (icono != null)
            {
                var ic = new GameObject("Icono", typeof(RectTransform), typeof(Image));
                ic.transform.SetParent(go.transform, false);
                var iImg = ic.GetComponent<Image>();
                iImg.sprite = icono;
                iImg.preserveAspect = true;
                var ir = (RectTransform)ic.transform;
                ir.anchorMin = new Vector2(0.25f, 0.35f);
                ir.anchorMax = new Vector2(0.75f, 0.9f);
                ir.offsetMin = ir.offsetMax = Vector2.zero;
            }

            var t = new GameObject("Text", typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(go.transform, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = label;
            t.fontSize = 22;
            t.alignment = TextAnchor.LowerCenter;
            t.color = Color.white;
            var tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            txt = t;

            return btn;
        }
    }
}
