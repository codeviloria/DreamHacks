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
    /// Escena "TransicionComa": muestra diapositivas con fundido y texto, y
    /// luego carga la escena indicada. Construye el Canvas y los widgets por
    /// código para no depender de nada en la escena, solo pide un GameObject
    /// con este script y la cámara estándar.
    /// </summary>
    public class TransicionComa : MonoBehaviour
    {
        [System.Serializable]
        public class Diapositiva
        {
            public Sprite imagen;
            [TextArea] public string texto;
            public float duracion = 3.5f;
        }

        [SerializeField] private Diapositiva[] diapositivas;
        [SerializeField] private string escenaSiguiente = "Hospital";
        [SerializeField, Min(0.1f)] private float fundido = 1.2f;
        [SerializeField] private bool saltarConTecla = true;

        private Image _img;
        private Text _txt;
        private CanvasGroup _grupo;

        private void Awake()
        {
            var canvasGO = new GameObject("CanvasTransicion", typeof(Canvas), typeof(CanvasScaler));
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
            Estirar((RectTransform)cont.transform);
            _grupo = cont.GetComponent<CanvasGroup>();
            _grupo.alpha = 0f;

            _img = new GameObject("Imagen", typeof(Image)).GetComponent<Image>();
            _img.transform.SetParent(cont.transform, false);
            _img.preserveAspect = true;
            Estirar(_img.rectTransform);

            _txt = new GameObject("Texto", typeof(Text)).GetComponent<Text>();
            _txt.transform.SetParent(cont.transform, false);
            _txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txt.fontSize = 34;
            _txt.alignment = TextAnchor.LowerCenter;
            _txt.color = new Color(0.8f, 1f, 0.95f);
            var r = _txt.rectTransform;
            r.anchorMin = new Vector2(0.1f, 0.04f);
            r.anchorMax = new Vector2(0.9f, 0.25f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var sombra = _txt.gameObject.AddComponent<Shadow>();
            sombra.effectDistance = new Vector2(2f, -2f);
        }

        private static void Estirar(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private IEnumerator Start()
        {
            if (diapositivas == null) yield break;
            foreach (var d in diapositivas)
            {
                _img.sprite = d.imagen;
                _img.enabled = d.imagen != null;
                _txt.text = d.texto;
                yield return Fundir(0f, 1f);
                float t = 0f;
                while (t < d.duracion && !(saltarConTecla && AnyKeyPressed()))
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                yield return Fundir(1f, 0f);
            }
            if (!string.IsNullOrEmpty(escenaSiguiente))
                SceneManager.LoadScene(escenaSiguiente);
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
