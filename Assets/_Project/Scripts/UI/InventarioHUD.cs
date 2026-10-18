using UnityEngine;
using UnityEngine.UI;
using DreamHacks.Combate;
using DreamHacks.Battle;

namespace DreamHacks.UI
{
    /// <summary>
    /// Contador de pociones del overworld (esquina superior derecha). Muestra
    /// Roja/Azul/Rosa con su icono y cantidad actual. Se construye por código y
    /// se refresca solo cuando cambia el inventario (se suscribe al evento
    /// estático Inventario.OnCambioEstatico, así funciona sin importar qué
    /// instancia cambie — pickups y HUD pueden tener instancias distintas).
    /// Lo spawnea BattleReturn al cargar cualquier overworld.
    /// </summary>
    public class InventarioHUD : MonoBehaviour
    {
        private Text _txtRojas;
        private Text _txtAzules;
        private Text _txtRosas;

        /// <summary>Crea el HUD como GameObject suelto en la escena.</summary>
        public static InventarioHUD Crear()
        {
            var go = new GameObject("InventarioHUD");
            return go.AddComponent<InventarioHUD>();
        }

        private void Awake()
        {
            ConstruirUI();
            Refrescar();
        }

        private void OnEnable()
        {
            Inventario.OnCambioEstatico += Refrescar;
        }

        private void OnDisable()
        {
            Inventario.OnCambioEstatico -= Refrescar;
        }

        private void Refrescar()
        {
            if (_txtRojas != null) _txtRojas.text = "x" + BattleState.InvRojas;
            if (_txtAzules != null) _txtAzules.text = "x" + BattleState.InvAzules;
            if (_txtRosas != null) _txtRosas.text = "x" + BattleState.InvRosas;
        }

        private void ConstruirUI()
        {
            var canvasGO = new GameObject("CanvasInventario", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150; // sobre el HUD de mundo, bajo diálogos (200)
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // Contenedor: esquina superior derecha.
            var cont = new GameObject("Contador", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cont.transform.SetParent(canvasGO.transform, false);
            var cRt = (RectTransform)cont.transform;
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            // Separado del borde derecho para que no se corte en la ventana de juego.
            cRt.anchoredPosition = new Vector2(-80f, -16f);
            var hlay = cont.GetComponent<HorizontalLayoutGroup>();
            hlay.spacing = 10;
            hlay.childAlignment = TextAnchor.MiddleRight;
            hlay.childControlWidth = false;
            hlay.childControlHeight = false;
            hlay.childForceExpandWidth = false;
            hlay.childForceExpandHeight = false;

            _txtRojas = CrearBadge(cont.transform, "Potion_Roja", "Pociones/potion_red",
                new Color(1f, 0.35f, 0.35f, 1f));
            _txtAzules = CrearBadge(cont.transform, "Potion_Azul", "Pociones/potion_blue",
                new Color(0.35f, 0.6f, 1f, 1f));
            _txtRosas = CrearBadge(cont.transform, "Potion_Rosa", "Pociones/potion_pink",
                new Color(1f, 0.5f, 0.85f, 1f));
        }

        private static Text CrearBadge(Transform parent, string name, string spritePath, Color fallback)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
            item.transform.SetParent(parent, false);
            var iRt = (RectTransform)item.transform;
            iRt.sizeDelta = new Vector2(92f, 40f);
            var bg = item.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.12f, 0.82f);
            bg.raycastTarget = false;
            var h = item.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 6;
            h.padding = new RectOffset(7, 7, 5, 5);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            // Icono de poción (si está en Resources); si no, cuadrito de color.
            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                var ico = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ico.transform.SetParent(item.transform, false);
                ico.sprite = sprite;
                ico.preserveAspect = true;
                ico.color = Color.white;
                ico.raycastTarget = false;
                var icoRt = ico.rectTransform;
                icoRt.sizeDelta = new Vector2(30f, 30f);
            }
            else
            {
                var ico = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ico.transform.SetParent(item.transform, false);
                ico.color = fallback;
                ico.raycastTarget = false;
                var icoRt = ico.rectTransform;
                icoRt.sizeDelta = new Vector2(26f, 26f);
            }

            // Cantidad.
            var txt = new GameObject("Count", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            txt.transform.SetParent(item.transform, false);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = "x0";
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            var tRt = txt.rectTransform;
            tRt.sizeDelta = new Vector2(40f, 30f);
            var sh = txt.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(2f, -2f);
            return txt;
        }
    }
}
