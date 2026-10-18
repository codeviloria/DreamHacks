using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DreamHacks.Combate;
using DreamHacks.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DreamHacks.Battle
{
    // UI de batalla v2: Iker a la izquierda con portrait, enemigo a la derecha
    // con su sprite (Goblin/Skel). Dos HUDs de corazones (rojo + naranja).
    // Panel inferior con 4 botones: Atacar / Pociones / Combo / Huir.
    // Subpaneles PocionMenu y ComboQTE que BattleManager controla.
    public class BattleUI : MonoBehaviour
    {
        [Header("Portraits/Sprites")]
        [SerializeField] private Sprite ikerPortrait;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Sprite enemyGoblin;
        [SerializeField] private Sprite enemySkel;
        [SerializeField] private Sprite enemySlime;

        [Header("Sprites de corazon rojo (Iker)")]
        [SerializeField] private Sprite corazonLleno;
        [SerializeField] private Sprite corazonTresCuartos;
        [SerializeField] private Sprite corazonMedio;
        [SerializeField] private Sprite corazonUnCuarto;
        [SerializeField] private Sprite corazonVacio;

        [Header("Sprites de corazon naranja (Enemigo)")]
        [SerializeField] private Sprite corazonOLleno;
        [SerializeField] private Sprite corazonOTresCuartos;
        [SerializeField] private Sprite corazonOMedio;
        [SerializeField] private Sprite corazonOUnCuarto;
        [SerializeField] private Sprite corazonOVacio;

        [Header("Pociones (iconos para el submenu)")]
        [SerializeField] private Sprite spRoja;
        [SerializeField] private Sprite spAzul;
        [SerializeField] private Sprite spRosa;

        public Image IkerImage { get; private set; }
        public Image EnemyImage { get; private set; }
        public Text LogText { get; private set; }
        public Button AtacarButton { get; private set; }
        public Button PocionesButton { get; private set; }
        public Button ComboButton { get; private set; }
        public Button HuirButton { get; private set; }

        public HUDCorazones HudIker { get; private set; }
        public HUDCorazones HudEnemigo { get; private set; }
        public PocionMenu PocionMenu { get; private set; }
        public ComboQTE ComboQTE { get; private set; }

        // Accesores a los sprites de corazón rojo (Iker). Los usa BattleManager
        // para pasarle los assets al cuadro de victoria del overworld.
        public Sprite CorazonLleno => corazonLleno;
        public Sprite CorazonTresCuartos => corazonTresCuartos;
        public Sprite CorazonMedio => corazonMedio;
        public Sprite CorazonUnCuarto => corazonUnCuarto;
        public Sprite CorazonVacio => corazonVacio;

        private Color _ikerColor;
        private Color _enemyColor;

        private void Awake()
        {
            if (ikerPortrait == null) ikerPortrait = Resources.Load<Sprite>("Portraits/Iker_Portrait");

            var canvasGO = new GameObject("CanvasBattle", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // Fondo oscuro.
            var fondo = new GameObject("Fondo", typeof(Image)).GetComponent<Image>();
            fondo.transform.SetParent(canvasGO.transform, false);
            fondo.color = new Color(0.02f, 0.02f, 0.04f, 1f);
            Estirar(fondo.rectTransform);

            // Fondo tipo Matrix (encima del fondo, debajo del resto).
            var matrixGO = new GameObject("MatrixBG").AddComponent<MatrixRain>();
            matrixGO.transform.SetParent(canvasGO.transform, false);
            matrixGO.Construir((RectTransform)canvasGO.transform);

            // Iker portrait (izquierda).
            IkerImage = new GameObject("IkerImage", typeof(Image)).GetComponent<Image>();
            IkerImage.transform.SetParent(canvasGO.transform, false);
            IkerImage.sprite = ikerPortrait;
            IkerImage.preserveAspect = true;
            var ikerRt = IkerImage.rectTransform;
            ikerRt.anchorMin = new Vector2(0.05f, 0.42f);
            ikerRt.anchorMax = new Vector2(0.42f, 0.88f);
            ikerRt.offsetMin = ikerRt.offsetMax = Vector2.zero;
            _ikerColor = IkerImage.color;

            // Enemigo (derecha).
            EnemyImage = new GameObject("EnemyImage", typeof(Image)).GetComponent<Image>();
            EnemyImage.transform.SetParent(canvasGO.transform, false);
            if (enemySprite != null) EnemyImage.sprite = enemySprite;
            EnemyImage.color = enemySprite != null ? Color.white : new Color(0.95f, 0.1f, 0.1f, 1f);
            EnemyImage.preserveAspect = true;
            var eRt = EnemyImage.rectTransform;
            eRt.anchorMin = new Vector2(0.58f, 0.42f);
            eRt.anchorMax = new Vector2(0.95f, 0.88f);
            eRt.offsetMin = eRt.offsetMax = Vector2.zero;
            _enemyColor = EnemyImage.color;

            // HUD Iker (rojo) arriba-izquierda.
            HudIker = CrearHUD(canvasGO.transform, "HUD_Iker", new Vector2(0.03f, 0.9f), new HUDCorazones.Config
            {
                lleno = corazonLleno,
                tresCuartos = corazonTresCuartos,
                medio = corazonMedio,
                unCuarto = corazonUnCuarto,
                vacio = corazonVacio,
                tint = Color.white,
                tamano = 28f, separacion = 3f, porFila = 10
            });
            // HUD Enemigo (naranja) arriba-derecha.
            HudEnemigo = CrearHUD(canvasGO.transform, "HUD_Enemigo", new Vector2(0.57f, 0.9f), new HUDCorazones.Config
            {
                lleno = corazonOLleno != null ? corazonOLleno : corazonLleno,
                tresCuartos = corazonOTresCuartos != null ? corazonOTresCuartos : corazonTresCuartos,
                medio = corazonOMedio != null ? corazonOMedio : corazonMedio,
                unCuarto = corazonOUnCuarto != null ? corazonOUnCuarto : corazonUnCuarto,
                vacio = corazonOVacio != null ? corazonOVacio : corazonVacio,
                tint = Color.white,
                tamano = 28f, separacion = 3f, porFila = 10
            });

            // Log.
            LogText = CrearTexto(canvasGO.transform, "Log", new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.36f), TextAnchor.MiddleCenter, 22);

            // Panel inferior con 4 botones.
            var panel = new GameObject("Botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(canvasGO.transform, false);
            var pRt = (RectTransform)panel.transform;
            pRt.anchorMin = new Vector2(0.05f, 0.04f);
            pRt.anchorMax = new Vector2(0.95f, 0.22f);
            pRt.offsetMin = pRt.offsetMax = Vector2.zero;
            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AtacarButton = CrearBoton(panel.transform, "Atacar");
            PocionesButton = CrearBoton(panel.transform, "Pociones");
            ComboButton = CrearBoton(panel.transform, "Combo");
            HuirButton = CrearBoton(panel.transform, "Huir");

            // Marca el botón Atacar como seleccionado para habilitar navegación con flechas.
            StartCoroutine(SeleccionarAtacarCuandoListo());

            // Submenu Pociones.
            PocionMenu = new GameObject("PocionMenuHost").AddComponent<PocionMenu>();
            PocionMenu.transform.SetParent(canvasGO.transform, false);
            PocionMenu.Build(canvasGO.transform, spRoja, spAzul, spRosa);

            // Combo QTE.
            ComboQTE = new GameObject("ComboQTEHost").AddComponent<ComboQTE>();
            ComboQTE.transform.SetParent(canvasGO.transform, false);
            ComboQTE.Build(canvasGO.transform);
        }

        public void SetEnemySprite(Sprite s)
        {
            if (EnemyImage == null) return;
            if (s == null) return;
            EnemyImage.sprite = s;
            EnemyImage.color = Color.white;
            _enemyColor = Color.white;
        }

        public void SetEnemyByKey(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (id.StartsWith("Goblin")) SetEnemySprite(enemyGoblin);
            else if (id.StartsWith("Skel")) SetEnemySprite(enemySkel);
            else if (id.StartsWith("Slime")) SetEnemySprite(enemySlime);
        }

        public void AttachVidas(IVida iker, IVida enemy)
        {
            if (HudIker != null) HudIker.SetVida(iker);
            if (HudEnemigo != null) HudEnemigo.SetVida(enemy);
        }

        public void SetBotonesInteractivos(bool interactivo)
        {
            if (AtacarButton != null) AtacarButton.interactable = interactivo;
            if (PocionesButton != null) PocionesButton.interactable = interactivo;
            if (ComboButton != null) ComboButton.interactable = interactivo;
            if (HuirButton != null) HuirButton.interactable = interactivo;
            if (interactivo) SeleccionarAtacarSiNinguno();
        }

        private IEnumerator SeleccionarAtacarCuandoListo()
        {
            yield return null; // esperar un frame a que EventSystem exista
            SeleccionarAtacarSiNinguno();
        }

        private void SeleccionarAtacarSiNinguno()
        {
            var es = EventSystem.current;
            if (es == null || AtacarButton == null) return;
            if (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy)
                es.SetSelectedGameObject(AtacarButton.gameObject);
        }

        public IEnumerator FlashPlayer() { yield return FlashImpacto(IkerImage, _ikerColor); }
        public IEnumerator FlashEnemy() { yield return FlashImpacto(EnemyImage, _enemyColor); }

        public IEnumerator MostrarBannerPixel(string titulo, string subtexto, Color tinte)
        {
            var canvas = transform.GetComponentInChildren<Canvas>();
            if (canvas == null) yield break;

            var cont = new GameObject("BannerPixel", typeof(RectTransform));
            cont.transform.SetParent(canvas.transform, false);
            var cRt = (RectTransform)cont.transform;
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;

            // Fondo oscuro semitransparente para legibilidad.
            var fondo = new GameObject("Fondo", typeof(Image)).GetComponent<Image>();
            fondo.transform.SetParent(cont.transform, false);
            fondo.color = new Color(0f, 0f, 0f, 0.75f);
            fondo.raycastTarget = false;
            var fRt = fondo.rectTransform;
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;

            // Título gigante.
            var txt = new GameObject("Titulo", typeof(Text)).GetComponent<Text>();
            txt.transform.SetParent(cont.transform, false);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = titulo;
            txt.fontSize = 180;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = tinte;
            txt.raycastTarget = false;
            var tRt = txt.rectTransform;
            tRt.anchorMin = new Vector2(0.0f, 0.35f);
            tRt.anchorMax = new Vector2(1.0f, 0.75f);
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;
            // Outline grueso para look pixel.
            var outline = txt.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(6f, -6f);
            var sh = txt.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(10f, -10f);
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);

            // Subtexto.
            var sub = new GameObject("Sub", typeof(Text)).GetComponent<Text>();
            sub.transform.SetParent(cont.transform, false);
            sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sub.text = subtexto;
            sub.fontSize = 36;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(1f, 1f, 1f, 1f);
            sub.raycastTarget = false;
            var sRt = sub.rectTransform;
            sRt.anchorMin = new Vector2(0.0f, 0.22f);
            sRt.anchorMax = new Vector2(1.0f, 0.33f);
            sRt.offsetMin = sRt.offsetMax = Vector2.zero;
            var sSh = sub.gameObject.AddComponent<Shadow>();
            sSh.effectDistance = new Vector2(3f, -3f);

            // Pop de entrada.
            float t = 0f, popDur = 0.35f;
            Vector3 escalaBase = Vector3.one;
            while (t < popDur)
            {
                float p = t / popDur;
                float escala = Mathf.Lerp(0.3f, 1.08f, p);
                tRt.localScale = escalaBase * escala;
                t += Time.deltaTime;
                yield return null;
            }
            tRt.localScale = escalaBase;

            // Grace period: ignora teclas mientras el player suelta la que estaba
            // usando para atacar; evita que el banner se cierre al instante.
            float grace = 0.6f;
            while (grace > 0f) { grace -= Time.deltaTime; yield return null; }

            // Loop de pulso + parpadeo del subtexto, hasta que se presione cualquier tecla.
            float holdT = 0f;
            while (!AnyKey())
            {
                holdT += Time.deltaTime;
                tRt.localScale = escalaBase * (1f + 0.03f * Mathf.Sin(holdT * 4f));
                sub.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.PingPong(holdT * 1.5f, 1f));
                yield return null;
            }

            // Fade out rápido.
            float fo = 0f, foDur = 0.25f;
            while (fo < foDur)
            {
                float a = 1f - fo / foDur;
                txt.color = new Color(tinte.r, tinte.g, tinte.b, a);
                sub.color = new Color(1f, 1f, 1f, a);
                fondo.color = new Color(0f, 0f, 0f, 0.75f * a);
                fo += Time.deltaTime;
                yield return null;
            }
            Destroy(cont);
        }

        private static bool AnyKey()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
            var ms = UnityEngine.InputSystem.Mouse.current;
            if (ms != null && ms.leftButton.wasPressedThisFrame) return true;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }

        public IEnumerator MostrarBannerVictoria(int nivel, int atkAnterior, int atkNuevo, int defensa, int saludCuartos, int saludCuartosMax)
        {
            var canvas = transform.GetComponentInChildren<Canvas>();
            if (canvas == null) yield break;

            var cont = new GameObject("BannerVictoria", typeof(RectTransform));
            cont.transform.SetParent(canvas.transform, false);
            var cRt = (RectTransform)cont.transform;
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;

            var fondo = new GameObject("Fondo", typeof(Image)).GetComponent<Image>();
            fondo.transform.SetParent(cont.transform, false);
            fondo.color = new Color(0f, 0f, 0f, 0.82f);
            fondo.raycastTarget = false;
            var fRt = fondo.rectTransform;
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;

            // Título "¡VICTORIA!" arriba.
            var titulo = CrearTextoPixel(cont.transform, "Titulo", "¡VICTORIA!", 160,
                new Vector2(0f, 0.74f), new Vector2(1f, 0.95f), new Color(1f, 0.95f, 0.3f, 1f));
            var tituloRt = titulo.rectTransform;

            // Subtítulo "+1 EXP   Nivel X".
            CrearTextoPixel(cont.transform, "Sub", "+1 EXP   Nivel " + nivel, 36,
                new Vector2(0f, 0.66f), new Vector2(1f, 0.74f), Color.white);

            // Panel central con 3 filas de stats.
            var panel = new GameObject("StatsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(cont.transform, false);
            var pRt = (RectTransform)panel.transform;
            pRt.anchorMin = new Vector2(0.18f, 0.22f);
            pRt.anchorMax = new Vector2(0.82f, 0.62f);
            pRt.offsetMin = pRt.offsetMax = Vector2.zero;
            var vLay = panel.GetComponent<VerticalLayoutGroup>();
            vLay.childAlignment = TextAnchor.MiddleCenter;
            vLay.spacing = 18;
            vLay.childControlWidth = true;
            vLay.childControlHeight = true;
            vLay.childForceExpandWidth = true;
            vLay.childForceExpandHeight = true;

            int atkDelta = atkNuevo - atkAnterior;
            string atkTxt = "ATAQUE   " + atkNuevo + " cuartos" + (atkDelta > 0 ? "   <color=#7CFF7C>+" + atkDelta + "</color>" : "");
            CrearFilaStat(panel.transform, "FilaAtaque", atkTxt);
            CrearFilaStat(panel.transform, "FilaDefensa", "DEFENSA   " + defensa + " golpes = 1 cuarto");
            CrearFilaSalud(panel.transform, saludCuartos, saludCuartosMax);

            // Línea inferior "presioná cualquier tecla".
            var hint = CrearTextoPixel(cont.transform, "Hint", "presioná cualquier tecla", 28,
                new Vector2(0f, 0.08f), new Vector2(1f, 0.16f), new Color(1f, 1f, 1f, 1f));

            // Pop del título.
            float t = 0f, popDur = 0.35f;
            Vector3 escalaBase = Vector3.one;
            while (t < popDur)
            {
                float p = t / popDur;
                float escala = Mathf.Lerp(0.3f, 1.08f, p);
                tituloRt.localScale = escalaBase * escala;
                t += Time.deltaTime;
                yield return null;
            }
            tituloRt.localScale = escalaBase;

            // Grace 0.6s para no comer la última tecla del combate.
            float grace = 0.6f;
            while (grace > 0f) { grace -= Time.deltaTime; yield return null; }

            float holdT = 0f;
            while (!AnyKey())
            {
                holdT += Time.deltaTime;
                tituloRt.localScale = escalaBase * (1f + 0.03f * Mathf.Sin(holdT * 4f));
                hint.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.PingPong(holdT * 1.5f, 1f));
                yield return null;
            }

            float fo = 0f, foDur = 0.25f;
            while (fo < foDur)
            {
                float a = 1f - fo / foDur;
                fondo.color = new Color(0f, 0f, 0f, 0.82f * a);
                var cg = cont.GetComponent<CanvasGroup>();
                if (cg == null) cg = cont.AddComponent<CanvasGroup>();
                cg.alpha = a;
                fo += Time.deltaTime;
                yield return null;
            }
            Destroy(cont);
        }

        private static Text CrearTextoPixel(Transform parent, string name, string texto, int fontSize, Vector2 aMin, Vector2 aMax, Color color)
        {
            var txt = new GameObject(name, typeof(Text)).GetComponent<Text>();
            txt.transform.SetParent(parent, false);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = texto;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.raycastTarget = false;
            txt.supportRichText = true;
            var rt = txt.rectTransform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var ol = txt.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(4f, -4f);
            var sh = txt.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(6f, -6f);
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
            return txt;
        }

        private void CrearFilaStat(Transform parent, string name, string texto)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 48f;
            var t = new GameObject("Text", typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(row.transform, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = texto;
            t.fontSize = 34;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.supportRichText = true;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(3f, -3f);
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        }

        private void CrearFilaSalud(Transform parent, int cuartos, int cuartosMax)
        {
            var row = new GameObject("FilaSalud", typeof(RectTransform), typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 56f;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 14;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var etiqueta = new GameObject("Label", typeof(Text), typeof(LayoutElement)).GetComponent<Text>();
            etiqueta.transform.SetParent(row.transform, false);
            etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            etiqueta.text = "SALUD";
            etiqueta.fontSize = 34;
            etiqueta.alignment = TextAnchor.MiddleRight;
            etiqueta.color = Color.white;
            etiqueta.raycastTarget = false;
            etiqueta.GetComponent<LayoutElement>().preferredWidth = 150f;
            etiqueta.GetComponent<LayoutElement>().preferredHeight = 48f;
            var eSh = etiqueta.gameObject.AddComponent<Shadow>();
            eSh.effectDistance = new Vector2(3f, -3f);

            // Dibuja los corazones directamente desde los sprites rojos de Iker.
            int totalCorazones = Mathf.CeilToInt(cuartosMax / 4f);
            float tamIcono = 42f;
            for (int i = 0; i < totalCorazones; i++)
            {
                int base4 = i * 4;
                int frac = Mathf.Clamp(cuartos - base4, 0, 4);
                Sprite s;
                switch (frac)
                {
                    case 4: s = corazonLleno; break;
                    case 3: s = corazonTresCuartos; break;
                    case 2: s = corazonMedio; break;
                    case 1: s = corazonUnCuarto; break;
                    default: s = corazonVacio; break;
                }
                var go = new GameObject("H" + i, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(row.transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = s;
                img.color = Color.white;
                img.raycastTarget = false;
                img.enabled = s != null;
                var le = go.GetComponent<LayoutElement>();
                le.preferredWidth = tamIcono;
                le.preferredHeight = tamIcono;
            }
        }

        public IEnumerator MostrarBanner(string texto, float duracion, Color colorTint)
        {
            var canvas = transform.GetComponentInChildren<Canvas>();
            if (canvas == null) yield break;
            var go = new GameObject("Banner", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            var txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = texto;
            txt.fontSize = 96;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = colorTint;
            var rt = txt.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.4f);
            rt.anchorMax = new Vector2(0.9f, 0.7f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            txt.raycastTarget = false;
            var sh = go.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(4f, -4f);
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);

            float t = 0f;
            Vector3 escalaBase = Vector3.one;
            while (t < duracion)
            {
                float p = t / duracion;
                float escala = Mathf.Lerp(0.5f, 1.1f, Mathf.Clamp01(p * 4f)) + 0.05f * Mathf.Sin(t * 12f);
                rt.localScale = escalaBase * escala;
                float alpha = p < 0.8f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.8f) / 0.2f);
                var c = txt.color; c.a = alpha; txt.color = c;
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(go);
        }

        private static IEnumerator FlashImpacto(Image img, Color baseColor)
        {
            if (img == null) yield break;
            var rt = img.rectTransform;
            Vector2 origen = rt.anchoredPosition;
            Vector3 escalaBase = rt.localScale;
            Color tintImpacto = new Color(1f, 0.3f, 0.3f, 1f); // rojo saturado para que destaque
            for (int i = 0; i < 4; i++)
            {
                img.color = tintImpacto;
                rt.localScale = escalaBase * 1.08f;
                rt.anchoredPosition = origen + Random.insideUnitCircle * 18f;
                yield return new WaitForSeconds(0.05f);
                img.color = baseColor;
                rt.localScale = escalaBase;
                rt.anchoredPosition = origen;
                yield return new WaitForSeconds(0.05f);
            }
            img.color = baseColor;
            rt.localScale = escalaBase;
            rt.anchoredPosition = origen;
        }

        private HUDCorazones CrearHUD(Transform parent, string name, Vector2 anchoredNorm, HUDCorazones.Config cfg)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchoredNorm;
            rt.anchorMax = anchoredNorm;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400, 80);
            var hud = go.AddComponent<HUDCorazones>();
            hud.Configurar(cfg);
            return hud;
        }

        private static void Estirar(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static Text CrearTexto(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align, int fontSize)
        {
            var t = new GameObject(name, typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = align;
            t.color = new Color(0.9f, 0.95f, 1f);
            t.fontSize = fontSize;
            var rt = t.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private static Button CrearBoton(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(Image), typeof(Button));
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
            cb.selectedColor = new Color(0.4f, 0.9f, 1f, 1f); // celeste bien visible
            cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            // Borde-selección adicional (hijo Outline que se activa cuando selected).
            var outlineGO = new GameObject("Outline", typeof(Image));
            outlineGO.transform.SetParent(go.transform, false);
            var oImg = outlineGO.GetComponent<Image>();
            oImg.color = new Color(1f, 0.9f, 0.2f, 1f);
            oImg.raycastTarget = false;
            var oRt = oImg.rectTransform;
            oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
            oRt.offsetMin = new Vector2(-4f, -4f); oRt.offsetMax = new Vector2(4f, 4f);
            outlineGO.transform.SetAsFirstSibling();
            outlineGO.SetActive(false);
            go.AddComponent<SelectionOutline>().Setup(outlineGO);

            var txt = new GameObject("Text", typeof(Text)).GetComponent<Text>();
            txt.transform.SetParent(go.transform, false);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            return btn;
        }
    }

}
