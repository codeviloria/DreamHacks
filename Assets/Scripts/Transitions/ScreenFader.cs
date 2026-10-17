using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Fundido a negro usado por SceneTransition antes y después del cambio de
    /// escena. Es un singleton perezoso: se crea on-demand su propio Canvas +
    /// Image a pantalla completa, se marca DontDestroyOnLoad y se queda listo
    /// para cualquier otra transición futura.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;

        private Image _image;

        public static ScreenFader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("~ScreenFader");
                    _instance = go.AddComponent<ScreenFader>();
                    _instance.Build();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Build()
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasGO.AddComponent<CanvasScaler>();

            var imgGO = new GameObject("Fade");
            imgGO.transform.SetParent(canvasGO.transform, false);
            _image = imgGO.AddComponent<Image>();
            _image.color = new Color(0f, 0f, 0f, 0f);
            _image.raycastTarget = false;

            var rt = _image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_image == null) yield break;
            var start = _image.color.a;
            if (duration <= 0f) { SetAlpha(targetAlpha); yield break; }
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(targetAlpha);
        }

        public void SetAlpha(float a)
        {
            if (_image == null) return;
            var c = _image.color;
            c.a = a;
            _image.color = c;
        }
    }
}
