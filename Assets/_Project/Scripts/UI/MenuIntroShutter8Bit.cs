using UnityEngine;
using UnityEngine.UI;

namespace DreamHacks.UI
{
    /// <summary>
    /// Cortina de apertura en negro estilo 8-bit clásico.
    /// Al iniciar el menú, dos barras negras cubren toda la pantalla y se abren
    /// hacia arriba y hacia abajo en pasos discretos (estética 8-bit).
    /// Cuando terminan de abrirse, activa IsIntroFinished para que los botones,
    /// el prisma y el título comiencen sus animaciones.
    /// </summary>
    public class MenuIntroShutter8Bit : MonoBehaviour
    {
        public static bool IsIntroFinished { get; private set; } = true;
        public static float IntroFinishedTime { get; private set; }

        [Header("Configuración de Apertura 8-bit")]
        [Tooltip("Pausa breve en negro antes de empezar a abrir la pantalla.")]
        [SerializeField] private float initialBlackDelay = 0.25f;
        [Tooltip("Duración total de la apertura de las barras negras (segundos).")]
        [SerializeField] private float openDuration = 1.15f;
        [Tooltip("Cantidad de escalones discretos para dar el efecto clásico de 8 bits.")]
        [SerializeField] private int retroSteps = 14;

        private RectTransform _topBar;
        private RectTransform _bottomBar;
        private GameObject _shutterContainer;
        private float _elapsed;

        private void Awake()
        {
            IsIntroFinished = false;
            _elapsed = 0f;
            EnsureShutterBars();
        }

        private void EnsureShutterBars()
        {
            Transform existing = transform.Find("Overlay_Cortina8Bit");
            if (existing != null)
            {
                _shutterContainer = existing.gameObject;
                _shutterContainer.SetActive(true);
                _shutterContainer.transform.SetAsLastSibling();
                _topBar = existing.Find("Cortina_Superior") as RectTransform;
                _bottomBar = existing.Find("Cortina_Inferior") as RectTransform;
            }
            else
            {
                _shutterContainer = new GameObject("Overlay_Cortina8Bit", typeof(RectTransform));
                _shutterContainer.transform.SetParent(transform, false);
                _shutterContainer.transform.SetAsLastSibling();

                var containerRt = _shutterContainer.GetComponent<RectTransform>();
                containerRt.anchorMin = Vector2.zero;
                containerRt.anchorMax = Vector2.one;
                containerRt.offsetMin = Vector2.zero;
                containerRt.offsetMax = Vector2.zero;

                _topBar = CreateBar("Cortina_Superior", containerRt, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                _bottomBar = CreateBar("Cortina_Inferior", containerRt, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f));
            }

            if (_topBar != null) _topBar.localScale = Vector3.one;
            if (_bottomBar != null) _bottomBar.localScale = Vector3.one;
        }

        private static RectTransform CreateBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true; // Bloquea clics mientras se abre la pantalla
            return rt;
        }

        private void Update()
        {
            if (IsIntroFinished) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < initialBlackDelay)
            {
                return;
            }

            float openProgress = Mathf.Clamp01((_elapsed - initialBlackDelay) / Mathf.Max(0.01f, openDuration));

            if (openProgress >= 1f)
            {
                IsIntroFinished = true;
                IntroFinishedTime = Time.unscaledTime;
                if (_shutterContainer != null)
                    _shutterContainer.SetActive(false);
                return;
            }

            // Escalones estilo 8-bit (de 1.0 a 0.0 en pasos discretos)
            float steppedOpen = Mathf.Floor(openProgress * retroSteps) / Mathf.Max(1f, retroSteps);
            float remainingClosure = 1f - steppedOpen;

            if (_topBar != null)
                _topBar.localScale = new Vector3(1f, remainingClosure, 1f);
            if (_bottomBar != null)
                _bottomBar.localScale = new Vector3(1f, remainingClosure, 1f);
        }

        public void CloseShutter(float duration = 1.15f, System.Action onComplete = null)
        {
            StartCoroutine(CloseShutterRoutine(duration, onComplete));
        }

        public System.Collections.IEnumerator CloseShutterRoutine(float duration, System.Action onComplete = null)
        {
            EnsureShutterBars();
            if (_shutterContainer != null)
                _shutterContainer.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float steppedProgress = Mathf.Floor(progress * retroSteps) / Mathf.Max(1f, retroSteps);

                if (_topBar != null)
                    _topBar.localScale = new Vector3(1f, steppedProgress, 1f);
                if (_bottomBar != null)
                    _bottomBar.localScale = new Vector3(1f, steppedProgress, 1f);

                yield return null;
            }

            if (_topBar != null) _topBar.localScale = Vector3.one;
            if (_bottomBar != null) _bottomBar.localScale = Vector3.one;

            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            IsIntroFinished = true;
        }
    }
}
