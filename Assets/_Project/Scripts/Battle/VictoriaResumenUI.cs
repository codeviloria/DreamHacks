using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DreamHacks.World;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DreamHacks.Battle
{
    /// <summary>
    /// Cuadro decorado de victoria estilo JRPG que se muestra en el overworld
    /// (p.ej. Hospital) al volver de una batalla ganada. Se construye por código:
    /// marco dorado doble + fondo azul-noche, título "¡VICTORIA!!!", EXP ganada,
    /// nivel y salud restante en corazones. Se cierra con cualquier tecla/click.
    /// Lo spawnea BattleReturn cuando BattleState.PendingWinSummary está en true.
    /// Los sprites de corazón se pasan por campos estáticos desde la escena Battle
    /// (los assets sobreviven a la carga de escena; los GOs de la escena no).
    /// </summary>
    public class VictoriaResumenUI : MonoBehaviour
    {
        // Datos que BattleManager deja cargados antes de volver al overworld.
        private static Sprite[] _corazones; // [lleno, 3/4, 1/2, 1/4, vacío]
        private static int _cuartos;
        private static int _cuartosMax;

        /// <summary>
        /// Llama BattleManager.WinSequence() desde la escena Battle, antes de
        /// cargar la escena de retorno. Recibe los assets de corazón (sobreviven
        /// al cambio de escena) y la salud final en cuartos.
        /// </summary>
        public static void CargarDatos(Sprite lleno, Sprite tresCuartos, Sprite medio,
            Sprite unCuarto, Sprite vacio, int cuartos, int cuartosMax)
        {
            _corazones = new[] { lleno, tresCuartos, medio, unCuarto, vacio };
            _cuartos = cuartos;
            _cuartosMax = cuartosMax;
        }

        private IEnumerator Start()
        {
            // Consumir el flag ya: si el player muere/reinicia, no debe reaparecer.
            BattleState.PendingWinSummary = false;

            // Congelar al Player mientras el cuadro está abierto. Mecanismo doble:
            // - GameSignals.InputBlocked: overworld top-down (PlayerController de _Project).
            // - PlayerController2D.enabled: escenas metroidvania que usan ese controlador.
            GameSignals.InputBlocked = true;
            var player = GameObject.FindGameObjectWithTag("Player");
            var controller = player != null ? player.GetComponent<PlayerController2D>() : null;
            if (controller != null) controller.enabled = false;

            yield return ConstruirYMostrar();

            if (controller != null) controller.enabled = true;
            GameSignals.InputBlocked = false;
            Destroy(gameObject);
        }

        private IEnumerator ConstruirYMostrar()
        {
            // Canvas propio overlay (independiente del HUD de la escena).
            var canvasGO = new GameObject("CanvasVictoria", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // Fondo oscuro que bloquea clicks al mundo.
            var fondo = CrearImagen(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.72f));
            Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;

            // Marco exterior dorado.
            var marco = CrearImagen(canvasGO.transform, "Marco", new Color(0.85f, 0.65f, 0.12f, 1f));
            Anclar(marco.rectTransform, new Vector2(0.19f, 0.10f), new Vector2(0.81f, 0.90f));

            // Fondo interior azul-noche (efecto doble filete: el dorado asoma por los bordes).
            var panel = CrearImagen(marco.transform, "Panel", new Color(0.05f, 0.07f, 0.16f, 1f));
            Anclar(panel.rectTransform, Vector2.zero, Vector2.one);
            var pInset = panel.rectTransform;
            pInset.offsetMin = new Vector2(8f, 8f);
            pInset.offsetMax = new Vector2(-8f, -8f);

            // Contenido vertical dentro del panel.
            var contenido = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contenido.transform.SetParent(panel.transform, false);
            Anclar((RectTransform)contenido.transform, Vector2.zero, Vector2.one);
            var vlay = contenido.GetComponent<VerticalLayoutGroup>();
            vlay.padding = new RectOffset(24, 24, 20, 20);
            vlay.spacing = 14;
            vlay.childAlignment = TextAnchor.MiddleCenter;
            vlay.childControlWidth = true;
            vlay.childControlHeight = true;
            vlay.childForceExpandWidth = true;
            vlay.childForceExpandHeight = true;

            // Título "¡VICTORIA!!!".
            var titulo = CrearTexto(contenido.transform, "Titulo", "¡VICTORIA!!!", 110,
                new Color(1f, 0.92f, 0.25f, 1f), 160f);

            // Separador decorativo dorado.
            CrearSeparador(contenido.transform);

            // Fila EXP + nivel.
            string expTxt = "+" + BattleState.LastExpGained + " EXP     ★  Nivel " + BattleState.PlayerNivel + "  ★";
            CrearFila(contenido.transform, "FilaEXP", expTxt, 44, new Color(0.55f, 1f, 0.55f, 1f), 64f);

            // Fila de salud con corazones (si hay datos).
            if (_corazones != null && _cuartosMax > 0)
                CrearFilaSalud(contenido.transform);

            // Separador decorativo.
            CrearSeparador(contenido.transform);

            // Hint "presioná cualquier tecla".
            var hint = CrearTexto(contenido.transform, "Hint", "presioná cualquier tecla", 26,
                new Color(1f, 1f, 1f, 1f), 38f);

            // Pop de entrada del cuadro completo.
            var marcoRtT = (RectTransform)marco.transform;
            float t = 0f, popDur = 0.35f;
            Vector3 escalaBase = Vector3.one;
            while (t < popDur)
            {
                float p = t / popDur;
                marcoRtT.localScale = escalaBase * Mathf.Lerp(0.3f, 1.06f, p);
                t += Time.deltaTime;
                yield return null;
            }
            marcoRtT.localScale = escalaBase;

            // Grace 0.6s: ignora la tecla del golpe final que ganó la batalla.
            float grace = 0.6f;
            while (grace > 0f) { grace -= Time.deltaTime; yield return null; }

            // Pulso suave + parpadeo del hint hasta cualquier tecla/click.
            float holdT = 0f;
            while (!AnyKey())
            {
                holdT += Time.deltaTime;
                marcoRtT.localScale = escalaBase * (1f + 0.02f * Mathf.Sin(holdT * 4f));
                hint.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.PingPong(holdT * 1.5f, 1f));
                yield return null;
            }
            marcoRtT.localScale = escalaBase;

            // Fade out.
            var cg = canvasGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = canvasGO.AddComponent<CanvasGroup>();
            float fo = 0f, foDur = 0.25f;
            while (fo < foDur)
            {
                cg.alpha = 1f - fo / foDur;
                fo += Time.deltaTime;
                yield return null;
            }
        }

        // ---------- construcción de filas ----------

        private void CrearFilaSalud(Transform parent)
        {
            var row = new GameObject("FilaSalud", typeof(RectTransform), typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 46f;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 8;
            // Importante: con childControlWidth/Height = true el layout respeta los
            // LayoutElement.preferred* de cada hijo. Con false usaba el sizeDelta
            // default (100px) y la fila desbordaba el cuadro.
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var etiqueta = new GameObject("Label", typeof(Text), typeof(LayoutElement)).GetComponent<Text>();
            etiqueta.transform.SetParent(row.transform, false);
            etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            etiqueta.text = "SALUD";
            etiqueta.fontSize = 30;
            etiqueta.alignment = TextAnchor.MiddleRight;
            etiqueta.color = Color.white;
            etiqueta.raycastTarget = false;
            // Red de seguridad: si el ancho asignado no alcanza, el texto se
            // auto-enciende en vez de quedar truncado.
            etiqueta.resizeTextForBestFit = true;
            etiqueta.resizeTextMinSize = 14;
            etiqueta.resizeTextMaxSize = 30;
            etiqueta.GetComponent<LayoutElement>().preferredWidth = 110f;
            etiqueta.GetComponent<LayoutElement>().preferredHeight = 40f;
            AgregarSombra(etiqueta.gameObject, new Vector2(3f, -3f));

            int totalCorazones = Mathf.CeilToInt(_cuartosMax / 4f);
            float tamIcono = 34f;
            for (int i = 0; i < totalCorazones; i++)
            {
                int base4 = i * 4;
                int frac = Mathf.Clamp(_cuartos - base4, 0, 4);
                // Orden del array: [lleno, 3/4, 1/2, 1/4, vacío].
                Sprite s = _corazones[frac];
                var go = new GameObject("H" + i, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(row.transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = s;
                img.preserveAspect = true; // mantiene la proporción del sprite dentro del cuadrado
                img.color = Color.white;
                img.raycastTarget = false;
                img.enabled = s != null;
                var le = go.GetComponent<LayoutElement>();
                le.preferredWidth = tamIcono;
                le.preferredHeight = tamIcono;
            }
        }

        private static void CrearSeparador(Transform parent)
        {
            var sep = new GameObject("Separador", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            sep.transform.SetParent(parent, false);
            sep.GetComponent<LayoutElement>().minHeight = 3f;
            var img = sep.GetComponent<Image>();
            img.color = new Color(0.85f, 0.65f, 0.12f, 0.85f);
            img.raycastTarget = false;
        }

        private static Text CrearFila(Transform parent, string name, string texto, int fontSize, Color color, float altura)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = altura;
            var t = new GameObject("Text", typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(row.transform, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = texto;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            AgregarSombra(t.gameObject, new Vector2(3f, -3f));
            return t;
        }

        private static Text CrearTexto(Transform parent, string name, string texto, int fontSize, Color color, float altura)
        {
            var txt = new GameObject(name, typeof(Text), typeof(LayoutElement)).GetComponent<Text>();
            txt.transform.SetParent(parent, false);
            txt.GetComponent<LayoutElement>().minHeight = altura;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = texto;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.raycastTarget = false;
            // Outline + shadow para look pixel acorde a los banners de batalla.
            var ol = txt.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(4f, -4f);
            AgregarSombra(txt.gameObject, new Vector2(6f, -6f));
            return txt;
        }

        private static void AgregarSombra(GameObject go, Vector2 distancia)
        {
            var sh = go.AddComponent<Shadow>();
            sh.effectDistance = distancia;
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        }

        private static Image CrearImagen(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static void Anclar(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Estirar(RectTransform rt) => Anclar(rt, Vector2.zero, Vector2.one);

        private static bool AnyKey()
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
