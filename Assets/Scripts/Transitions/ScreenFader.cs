using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Fundido a negro usado por SceneTransition. Singleton perezoso con su
    /// propio Canvas Overlay + Image a pantalla completa, DontDestroyOnLoad.
    /// Se suscribe a SceneManager.sceneLoaded para hacer el fade-IN por su
    /// cuenta: la corrutina del SceneTransition original muere al cargar la
    /// nueva escena, así que el fade-in no puede vivir allí.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;

        private Image _image;

        [Tooltip("Duración del fade-in automático al cargar una escena.")]
        private float _postLoadFadeDuration = 0.3f;

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

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_instance == this) _instance = null;
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            // Siempre que termine un LoadScene, asegurar que la pantalla queda
            // visible. Si no veníamos de un fade-out, es un no-op (alpha ya 0).
            StartCoroutine(FadeTo(0f, _postLoadFadeDuration));
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
