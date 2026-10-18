using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DreamHacks.Combate;

namespace DreamHacks.UI
{
    // Panel pequeño temporal que aparece al tomar una poción, con la cara del
    // Iker a la izquierda y una frase a la derecha explicando qué hace esa
    // poción. Vive su propio tiempo y se autodestruye.
    public class DialogoPocion : MonoBehaviour
    {
        public static void Mostrar(TipoPocion tipo, Sprite portraitIker, float duracion = 2.8f)
        {
            var host = new GameObject("DialogoPocion").AddComponent<DialogoPocion>();
            host.StartCoroutine(host.Rutina(tipo, portraitIker, duracion));
        }

        private IEnumerator Rutina(TipoPocion tipo, Sprite portraitIker, float duracion)
        {
            var canvasGO = new GameObject("CanvasDialogoPocion", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var panel = new GameObject("Panel", typeof(Image)).GetComponent<Image>();
            panel.transform.SetParent(canvasGO.transform, false);
            panel.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            panel.raycastTarget = false;
            var pRt = panel.rectTransform;
            pRt.anchorMin = new Vector2(0.08f, 0.08f);
            pRt.anchorMax = new Vector2(0.92f, 0.26f);
            pRt.offsetMin = pRt.offsetMax = Vector2.zero;

            // Borde amarillo tipo marco.
            var borde = new GameObject("Borde", typeof(Image)).GetComponent<Image>();
            borde.transform.SetParent(panel.transform, false);
            borde.color = new Color(1f, 0.9f, 0.3f, 1f);
            borde.raycastTarget = false;
            var bRt = borde.rectTransform;
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(-4f, -4f); bRt.offsetMax = new Vector2(4f, 4f);
            borde.transform.SetAsFirstSibling();

            // Portrait Iker a la izquierda.
            if (portraitIker != null)
            {
                var retrato = new GameObject("Portrait", typeof(Image)).GetComponent<Image>();
                retrato.transform.SetParent(panel.transform, false);
                retrato.sprite = portraitIker;
                retrato.preserveAspect = true;
                retrato.raycastTarget = false;
                var rRt = retrato.rectTransform;
                rRt.anchorMin = new Vector2(0.01f, 0.05f);
                rRt.anchorMax = new Vector2(0.22f, 0.95f);
                rRt.offsetMin = rRt.offsetMax = Vector2.zero;
            }

            var txt = new GameObject("Texto", typeof(Text)).GetComponent<Text>();
            txt.transform.SetParent(panel.transform, false);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = Mensaje(tipo);
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = ColorPorTipo(tipo);
            txt.raycastTarget = false;
            var tRt = txt.rectTransform;
            tRt.anchorMin = new Vector2(0.24f, 0.1f);
            tRt.anchorMax = new Vector2(0.98f, 0.9f);
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;
            var sh = txt.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(2f, -2f);

            float t = 0f;
            Vector3 escalaBase = Vector3.one;
            Vector2 origen = pRt.anchoredPosition;
            while (t < duracion)
            {
                float p = t / duracion;
                float escala = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(p * 4f));
                pRt.localScale = escalaBase * escala;
                float alpha = p < 0.85f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.85f) / 0.15f);
                var pc = panel.color; pc.a = alpha * 0.92f; panel.color = pc;
                var tc = txt.color; tc.a = alpha; txt.color = tc;
                var bc = borde.color; bc.a = alpha; borde.color = bc;
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        private static string Mensaje(TipoPocion t)
        {
            switch (t)
            {
                case TipoPocion.Roja: return "Poción Roja. Suma +1 al poder de ataque de Iker.";
                case TipoPocion.Azul: return "Poción Azul. Reduce el próximo golpe recibido.";
                case TipoPocion.Rosa: return "Poción Rosa. Cura un corazón entero.";
            }
            return "Poción.";
        }

        private static Color ColorPorTipo(TipoPocion t)
        {
            switch (t)
            {
                case TipoPocion.Roja: return new Color(1f, 0.55f, 0.55f);
                case TipoPocion.Azul: return new Color(0.55f, 0.75f, 1f);
                case TipoPocion.Rosa: return new Color(1f, 0.6f, 0.9f);
            }
            return Color.white;
        }
    }
}
