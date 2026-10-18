using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DreamHacks.UI
{
    /// <summary>
    /// Intro tipo cómic: muestra viñetas (imagen + texto) una tras otra con
    /// fundido y al terminar carga la escena siguiente. Construye el Canvas por
    /// código para no depender de nada en la escena, solo necesita un
    /// GameObject con este script y la cámara estándar.
    /// </summary>
    public class ComicIntro : MonoBehaviour
    {
        [System.Serializable]
        public class Vineta
        {
            public Sprite imagen;
            [TextArea] public string texto;
            public float duracion = 3.5f;
        }

        [SerializeField] private Vineta[] vinetas;
        [SerializeField] private string escenaSiguiente = "Hospital";
        [SerializeField, Min(0.1f)] private float fundido = 1.2f;
        [SerializeField] private bool saltarConTecla = true;

        [Header("Layout del texto")]
        [Tooltip("Fracción (0..1) de la pantalla que ocupa la banda inferior reservada para el texto. La imagen vive por encima.")]
        [SerializeField, Range(0.08f, 0.3f)] private float alturaBandaTexto = 0.18f;

        [Header("Efecto vibrante al final")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.6f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 24f;
        [SerializeField, Min(0f)] private float flashDuration = 0.35f;

        private Image _img;
        private Text _txt;
        private CanvasGroup _grupo;
        private RectTransform _contenidoRt;
        private Image _flash;

        private void Awake()
        {
            var canvasGO = new GameObject("CanvasComic", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var fondo = new GameObject("Fondo", typeof(Image)).GetComponent<Image>();
            fondo.transform.SetParent(canvasGO.transform, false);
            fondo.color = Color.black;
            Estirar(fondo.rectTransform);

            var cont = new GameObject("Contenido", typeof(CanvasGroup), typeof(RectTransform));
            cont.transform.SetParent(canvasGO.transform, false);
            _contenidoRt = (RectTransform)cont.transform;
            Estirar(_contenidoRt);
            _grupo = cont.GetComponent<CanvasGroup>();
            _grupo.alpha = 0f;

            // La imagen vive por encima de la banda del texto (no se solapan).
            _img = new GameObject("Imagen", typeof(Image)).GetComponent<Image>();
            _img.transform.SetParent(cont.transform, false);
            _img.preserveAspect = true;
            var iR = _img.rectTransform;
            iR.anchorMin = new Vector2(0f, alturaBandaTexto);
            iR.anchorMax = new Vector2(1f, 1f);
            iR.offsetMin = iR.offsetMax = Vector2.zero;

            // Banda inferior opaca reservada para texto (ya no se superpone al sprite).
            var textoBG = new GameObject("TextoFondo", typeof(Image)).GetComponent<Image>();
            textoBG.transform.SetParent(cont.transform, false);
            textoBG.color = new Color(0f, 0f, 0f, 1f);
            textoBG.raycastTarget = false;
            var tbR = textoBG.rectTransform;
            tbR.anchorMin = new Vector2(0f, 0f);
            tbR.anchorMax = new Vector2(1f, alturaBandaTexto);
            tbR.offsetMin = tbR.offsetMax = Vector2.zero;

            _txt = new GameObject("Texto", typeof(Text)).GetComponent<Text>();
            _txt.transform.SetParent(cont.transform, false);
            _txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txt.fontSize = 42;
            _txt.alignment = TextAnchor.MiddleCenter;
            _txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _txt.color = new Color(0.95f, 0.97f, 1f);
            var r = _txt.rectTransform;
            r.anchorMin = new Vector2(0.05f, 0f);
            r.anchorMax = new Vector2(0.95f, alturaBandaTexto);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var sombra = _txt.gameObject.AddComponent<Shadow>();
            sombra.effectDistance = new Vector2(2f, -2f);

            // Panel de flash encima de todo, invisible al inicio.
            _flash = new GameObject("Flash", typeof(Image)).GetComponent<Image>();
            _flash.transform.SetParent(canvasGO.transform, false);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _flash.raycastTarget = false;
            Estirar(_flash.rectTransform);
        }

        private static void Estirar(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private IEnumerator Start()
        {
            if (vinetas == null || vinetas.Length == 0) yield break;
            for (int i = 0; i < vinetas.Length; i++)
            {
                var v = vinetas[i];
                _img.sprite = v.imagen;
                _img.enabled = v.imagen != null;
                _txt.text = v.texto;
                yield return Fundir(0f, 1f);
                float t = 0f;
                while (t < v.duracion && !(saltarConTecla && AnyKeyPressed()))
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                // En la última viñeta no bajamos el alpha a 0: el shake+flash
                // deja la imagen en pantalla hasta el corte a la escena siguiente.
                if (i < vinetas.Length - 1)
                    yield return Fundir(1f, 0f);
            }
            yield return VibrarYFlash();
            if (!string.IsNullOrEmpty(escenaSiguiente))
                SceneManager.LoadScene(escenaSiguiente);
        }

        private IEnumerator VibrarYFlash()
        {
            // Shake del contenido + flash blanco que crece hasta cubrir todo.
            float t = 0f;
            Vector2 origen = _contenidoRt != null ? _contenidoRt.anchoredPosition : Vector2.zero;
            float dur = Mathf.Max(shakeDuration, flashDuration);
            while (t < dur)
            {
                if (_contenidoRt != null && t < shakeDuration)
                {
                    Vector2 off = Random.insideUnitCircle * shakeAmplitude * (1f - t / shakeDuration);
                    _contenidoRt.anchoredPosition = origen + off;
                }
                if (_flash != null && t < flashDuration)
                {
                    var c = _flash.color;
                    c.a = Mathf.Clamp01(t / flashDuration);
                    _flash.color = c;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_contenidoRt != null) _contenidoRt.anchoredPosition = origen;
            if (_flash != null) _flash.color = Color.white;
        }

        private IEnumerator Fundir(float a, float b)
        {
            for (float t = 0f; t < fundido; t += Time.deltaTime)
            {
                _grupo.alpha = Mathf.Lerp(a, b, t / fundido);
                yield return null;
            }
            _grupo.alpha = b;
        }

        private static bool AnyKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
            var ms = Mouse.current;
            if (ms != null && ms.leftButton.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }
    }
}
