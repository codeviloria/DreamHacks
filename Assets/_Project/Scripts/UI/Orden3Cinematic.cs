using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DreamHacks.Core;
using DreamHacks.World;

namespace DreamHacks.UI
{
    /// <summary>
    /// Cinemática "ORDEN 3: Caminar hacia el daño".
    /// Se construye entera por código (Canvas overlay), no necesita assets en la escena.
    /// Uso: Orden3Cinematic.Create();  (o ver Orden3Trigger).
    /// Avanzar diálogo: E / Espacio / Enter / clic.
    /// Al terminar guarda la elección en GameState (flags "orden3_si" / "orden3_no").
    /// </summary>
    public class Orden3Cinematic : MonoBehaviour
    {
        public static bool IsPlaying { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>"si" o "no" cuando la cinemática terminó.</summary>
        public string Choice { get; private set; }

        public static Orden3Cinematic Create()
        {
            if (IsPlaying) return null;
            var go = new GameObject("Orden3Cinematic");
            return go.AddComponent<Orden3Cinematic>();
        }

        // ------------------------------------------------------------- Constantes
        private const float CharsPerSecond = 38f;
        private static readonly Color HackerColor = new Color(0.35f, 1f, 0.55f);
        private static readonly Color IkerColor   = new Color(0.55f, 0.8f, 1f);
        private static readonly Color HannahColor = new Color(1f, 0.6f, 0.75f);
        private static readonly Color UnknownColor = new Color(1f, 0.25f, 0.25f);
        private static readonly Color SystemColor = new Color(0.9f, 0.95f, 1f);

        private enum DoorMode { None, Red, Green, RedAlert }

        // ------------------------------------------------------------- UI
        private Font _font;
        private Canvas _canvas;
        private RectTransform _stage;           // pasillo + puerta + personajes (recibe shake)
        private RawImage _hall;
        private RawImage _door;
        private Image _doorFrame;
        private RawImage _iker, _hannah;
        private RectTransform _ikerRT, _hannahRT;
        private Image _damage;                  // overlay rojo
        private Image _fade;                    // negro
        private GameObject _hud;
        private RectTransform _ikerBar, _hannahBar;
        private GameObject _dlg;
        private Text _speaker, _body, _indicator;
        private RawImage _portrait, _noisePortrait;
        private Text _bigText;                  // título / ACCESO DENEGADO
        private GameObject _choiceBox;
        private Text _yesText, _noText;
        private Image _yesBg, _noBg;
        private RectTransform _yesRT, _noRT;

        // ------------------------------------------------------------- Texturas
        private Texture2D _texIker, _texHannah, _texHacker;
        private Texture2D _hallTexA, _hallTexB;
        private Texture2D _doorTex, _portraitNoiseTex, _scanTex;
        private Color32[] _doorPx, _portraitPx;
        private const int DoorW = 30, DoorH = 40, PortW = 20, PortH = 23;

        // ------------------------------------------------------------- Estado
        private DoorMode _doorMode = DoorMode.None;
        private float _noiseTimer;
        private float _shakeBase, _shakeKick;
        private float _damageAlpha;
        private bool _walking;
        private float _animTimer;
        private int _frame;
        private bool _glitchSpeaker;
        private bool _shoutGlitch;
        private float _hpIker = 1f, _hpHannah = 1f;
        private int _portraitKind;              // 0 ninguno, 1 iker, 2 hannah, 3 hacker, 4 estática

        private void Awake() { IsPlaying = true; }

        private void Start()
        {
            StartCoroutine(Main());
        }

        private void OnDestroy()
        {
            IsPlaying = false;
            GameSignals.InputBlocked = false;
            if (_hallTexA != null) Destroy(_hallTexA);
            if (_hallTexB != null) Destroy(_hallTexB);
            if (_doorTex != null) Destroy(_doorTex);
            if (_portraitNoiseTex != null) Destroy(_portraitNoiseTex);
            if (_scanTex != null) Destroy(_scanTex);
        }

        // =====================================================================
        //  GUION
        // =====================================================================
        private IEnumerator Main()
        {
            Build();
            GameSignals.InputBlocked = true;
            SetFade(1f);
            _stage.gameObject.SetActive(false);
            _hud.SetActive(false);
            _dlg.SetActive(false);
            _choiceBox.SetActive(false);

            // ---- Título
            yield return TitleCard("ORDEN 3", "CAMINAR HACIA EL DAÑO");

            // ---- Escena 1: pasillo con la puerta de estática roja
            _hall.texture = _hallTexA;
            SetDoor(DoorMode.Red);
            PlaceChars(new Vector2(-330f, -70f), 1f);
            _stage.gameObject.SetActive(true);
            _hud.SetActive(true);
            SetFade(0f);
            yield return GlitchIn();

            yield return Say("Hacker Anónimo", "Caminen directamente a través de la estática roja para llegar al punto de extracción.");
            yield return Say("Iker", "¡¿Estás loco?! ¡Eso parece código corrupto, nos va a doler!");
            yield return Say("Hacker Anónimo", "No miren hacia otro lado. No dejen de caminar. El dolor es solo simulado.");
            HideDialog();

            // ---- Caminar hacia el daño
            var walk = StartCoroutine(WalkThroughDoor());
            yield return new WaitForSecondsRealtime(4.6f);
            yield return Say("Hannah", "¡Ah! ¡¿Simulado?! ¡Siento que mi piel se está quemando!", false, 2.8f);
            HideDialog();
            yield return walk;

            // ---- Pasillo final
            yield return FlashWhite(0.35f);
            _damageAlpha = 0f;
            _shakeBase = 0f;
            _hall.texture = _hallTexB;
            SetDoor(DoorMode.Green);
            PlaceChars(new Vector2(-330f, -70f), 1f);
            SetHp(0.3f, 0.3f);
            yield return Wait(0.8f);

            yield return Say("Iker", "Siento como si mi cuerpo se estuviera quemando, duele un poco.");
            yield return Say("Hannah", "Sí, pero espera, ¿qué es eso?");
            yield return Say("Iker", "Parece algún tipo de puerta verde.");
            yield return Say("???", "Esta es su anhelada salida. Al pasar a través de la puerta verde, regresarán a sus vidas normales.");
            yield return Say("???", "This is your longed-for exit. By passing through the green door, you will return to your normal lives.");
            yield return Say("Hannah", "¡Por fin vamos a escapar, Iker!");
            HideDialog();

            // ---- Intentan salir y no pueden
            yield return RunToDoorAndBounce();
            yield return Say("Iker", "Aagh. ¿Qué demonios? ¿Por qué no podemos salir?");
            yield return Say("???", "El camino está cerrado. La memoria del sistema está llena. Para avanzar y escapar, alguien tiene que ceder su código.");
            yield return Say("Hannah", "¿Qué demonios quieres decir? ¿Qué significa eso de ceder su código? ¿Qué quieres de nosotros ahora?");
            SetDoor(DoorMode.RedAlert);
            _shakeKick = 6f;
            yield return Say("???", "La memoria solo permite entrar a 1.");
            yield return Say("???", "Iker, tú eres más fuerte, puedo leer tus estadísticas en el código. Vamos, sabes que puedes hacerlo, sabes que quieres escapar, Iker.");
            _shakeBase = 2f;
            _damageAlpha = 0.12f;
            yield return Say("???", "ACABA CON ELLA, ES LA ÚNICA FORMA.", true);
            yield return Say("Hannah", "¡¿Qué demonios estás diciendo, qué demonios quieres decir?! Iker, ¡di algo, haz algo, Iker!");

            // ---- HAZLO HAZLO HAZLO
            _shakeBase = 4f; _damageAlpha = 0.2f;
            yield return Say("???", "HAZLO HAZLO HAZLO", true, 1.8f);
            yield return Say("Hannah", "Iker???", false, 1.4f);
            _shakeBase = 6f; _damageAlpha = 0.3f;
            yield return Say("???", "HAZLO HAZLO HAZLO", true, 1.8f);
            yield return Say("Hannah", "IKEEER?", false, 1.4f);
            _shakeBase = 9f; _damageAlpha = 0.42f;
            yield return Say("???", "HAZLO HAZLO HAZLO", true, 2.2f);
            HideDialog();

            // ---- Caja de texto del sistema
            _shakeBase = 0f;
            yield return Wait(0.4f);
            yield return ChoiceRoutine();

            var gs = GameState.I;
            if (gs != null)
            {
                gs.SetChoice(Choice == "si" ? "orden3_si" : "orden3_no");
                gs.SetFlag(Choice == "si" ? "orden3_si" : "orden3_no");
            }

            _damageAlpha = 0f;
            yield return Say("SISTEMA", Choice == "si" ? "Respuesta registrada: SÍ." : "Respuesta registrada: NO.", false, 2.2f);
            HideDialog();

            yield return FadeTo(1f, 0.8f);
            Finished = true;
            GameSignals.InputBlocked = false;
            Destroy(gameObject);
        }

        // =====================================================================
        //  SECUENCIAS
        // =====================================================================
        private IEnumerator TitleCard(string line1, string line2)
        {
            _bigText.gameObject.SetActive(true);
            _bigText.color = new Color(1f, 0.3f, 0.3f, 1f);
            _bigText.fontSize = 60;
            string full = line1 + "\n" + line2;
            float t = 0f;
            while (t < 2.8f)
            {
                t += Time.unscaledDeltaTime;
                int chars = Mathf.Min(full.Length, Mathf.FloorToInt(t * 28f));
                string shown = full.Substring(0, chars);
                // Glitch: de vez en cuando se corrompe el texto un instante.
                if (Random.value < 0.08f && chars > 2) shown = Corrupt(shown);
                _bigText.text = shown;
                _bigText.rectTransform.anchoredPosition = Random.value < 0.1f ? Random.insideUnitCircle * 6f : Vector2.zero;
                if (Advance() && t > 0.6f) break;
                yield return null;
            }
            _bigText.rectTransform.anchoredPosition = Vector2.zero;
            _bigText.gameObject.SetActive(false);
        }

        private static string Corrupt(string s)
        {
            var arr = s.ToCharArray();
            const string junk = "#@%&01$?/\\|";
            int n = Mathf.Max(1, arr.Length / 8);
            for (int i = 0; i < n; i++)
            {
                int k = Random.Range(0, arr.Length);
                if (arr[k] != '\n' && arr[k] != '<' && arr[k] != '>' && arr[k] != '/') arr[k] = junk[Random.Range(0, junk.Length)];
            }
            return new string(arr);
        }

        private IEnumerator GlitchIn()
        {
            for (int i = 0; i < 12; i++)
            {
                _stage.gameObject.SetActive(i % 3 != 0 || i == 11);
                _shakeKick = 10f;
                yield return Wait(0.05f);
            }
            _stage.gameObject.SetActive(true);
            yield return Wait(0.3f);
        }

        // Caminan hacia la puerta roja y la atraviesan recibiendo daño.
        private IEnumerator WalkThroughDoor()
        {
            const float total = 7.2f;
            const float damageStart = 4.0f;
            Vector2 start = new Vector2(-330f, -70f);
            Vector2 doorBase = new Vector2(0f, -42f);
            float t = 0f;
            _walking = true;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                float walkT = Mathf.Clamp01(t / (damageStart + 1.2f));
                float ease = Mathf.SmoothStep(0f, 1f, walkT);
                Vector2 p = Vector2.Lerp(start, doorBase, ease);
                float scale = Mathf.Lerp(1f, 0.42f, ease);
                float bob = Mathf.Abs(Mathf.Sin(t * 9f)) * 6f * scale;
                SetChar(_ikerRT, p + new Vector2(-34f * scale, bob), scale);
                SetChar(_hannahRT, p + new Vector2(34f * scale, Mathf.Abs(Mathf.Sin(t * 9f + 1f)) * 6f * scale), scale);

                // Daño: arranca cuando llegan a la estática.
                float dmg = Mathf.Clamp01((t - damageStart) / (total - damageStart));
                float pulse = (Mathf.Sin(t * 22f) * 0.5f + 0.5f);
                _damageAlpha = dmg > 0f ? Mathf.Lerp(0.15f, 0.5f, dmg) * (0.6f + 0.4f * pulse) : 0f;
                _shakeBase = dmg > 0f ? Mathf.Lerp(3f, 11f, dmg) : Mathf.Lerp(0f, 2f, walkT);
                if (dmg > 0f) SetHp(Mathf.Lerp(1f, 0.28f, dmg), Mathf.Lerp(1f, 0.22f, dmg));

                Color tint = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), dmg);
                _iker.color = tint; _hannah.color = tint;

                // Al entrar de lleno en la puerta se desvanecen.
                float a = 1f - Mathf.Clamp01((t - (total - 1.4f)) / 1.2f);
                var c1 = _iker.color; c1.a = a; _iker.color = c1;
                var c2 = _hannah.color; c2.a = a; _hannah.color = c2;
                yield return null;
            }
            _walking = false;
            _iker.color = Color.white; _hannah.color = Color.white;
        }

        private IEnumerator RunToDoorAndBounce()
        {
            Vector2 start = _ikerRT.anchoredPosition + new Vector2(34f, 0f);
            Vector2 door = new Vector2(0f, -42f);
            _walking = true;
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 1.6f);
                Vector2 p = Vector2.Lerp(start, door, k);
                float scale = Mathf.Lerp(1f, 0.45f, k);
                float bob = Mathf.Abs(Mathf.Sin(t * 12f)) * 6f * scale;
                SetChar(_ikerRT, p + new Vector2(-34f * scale, bob), scale);
                SetChar(_hannahRT, p + new Vector2(34f * scale, bob), scale);
                yield return null;
            }

            // Choque contra la puerta: acceso denegado.
            _walking = false;
            _shakeKick = 16f;
            SetDoor(DoorMode.RedAlert);
            _bigText.gameObject.SetActive(true);
            _bigText.fontSize = 56;
            _bigText.color = new Color(1f, 0.2f, 0.2f);
            _bigText.text = "ACCESO DENEGADO";
            _damageAlpha = 0.35f;

            Vector2 hit = _ikerRT.anchoredPosition;
            Vector2 back = hit + new Vector2(-150f, -28f);
            t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - t / 0.45f, 3f);
                var p = Vector2.Lerp(hit, back, k);
                float scale = Mathf.Lerp(0.45f, 0.62f, k);
                SetChar(_ikerRT, p + new Vector2(-34f * scale, 0f), scale);
                SetChar(_hannahRT, p + new Vector2(34f * scale, 0f), scale);
                yield return null;
            }
            yield return Wait(0.9f);
            _bigText.gameObject.SetActive(false);
            _damageAlpha = 0f;
            SetDoor(DoorMode.Green);
        }

        private IEnumerator ChoiceRoutine()
        {
            _choiceBox.SetActive(true);
            _damageAlpha = 0.15f;
            int sel = 1; // 0 = SÍ, 1 = NO (por defecto NO)
            RefreshChoice(sel);
            yield return null;

            while (true)
            {
                var kb = Keyboard.current;
                var ms = Mouse.current;
                if (kb != null)
                {
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) { sel = 0; RefreshChoice(sel); }
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) { sel = 1; RefreshChoice(sel); }
                }
                if (ms != null)
                {
                    Vector2 mp = ms.position.ReadValue();
                    bool onYes = RectTransformUtility.RectangleContainsScreenPoint(_yesRT, mp, null);
                    bool onNo = RectTransformUtility.RectangleContainsScreenPoint(_noRT, mp, null);
                    if (onYes && sel != 0) { sel = 0; RefreshChoice(sel); }
                    if (onNo && sel != 1) { sel = 1; RefreshChoice(sel); }
                    if (ms.leftButton.wasPressedThisFrame && (onYes || onNo))
                    {
                        Choice = onYes ? "si" : "no";
                        break;
                    }
                }
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                                   kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                {
                    Choice = sel == 0 ? "si" : "no";
                    break;
                }
                yield return null;
            }
            _choiceBox.SetActive(false);
        }

        private void RefreshChoice(int sel)
        {
            _yesBg.color = sel == 0 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.1f, 0.12f, 0.2f, 1f);
            _noBg.color  = sel == 1 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.1f, 0.12f, 0.2f, 1f);
            _yesText.color = sel == 0 ? Color.black : Color.white;
            _noText.color  = sel == 1 ? Color.black : Color.white;
            _yesText.text = sel == 0 ? "> SÍ <" : "SÍ";
            _noText.text  = sel == 1 ? "> NO <" : "NO";
        }

        private IEnumerator FlashWhite(float time)
        {
            _fade.color = new Color(1f, 1f, 1f, 1f);
            _shakeKick = 14f;
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                _fade.color = new Color(1f, 1f, 1f, 1f - t / time);
                yield return null;
            }
            _fade.color = new Color(0f, 0f, 0f, 0f);
        }

        private IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // =====================================================================
        //  DIÁLOGO
        // =====================================================================
        private IEnumerator Say(string who, string text, bool shout = false, float auto = 0f)
        {
            _dlg.SetActive(true);
            _speaker.text = who;
            _indicator.enabled = false;

            Color col = SpeakerColor(who);
            _speaker.color = col;
            _glitchSpeaker = who == "???";
            _shoutGlitch = shout;
            SetPortrait(who);

            _body.fontSize = shout ? 46 : 26;
            _body.color = shout ? new Color(1f, 0.2f, 0.2f) : (who == "???" ? new Color(1f, 0.7f, 0.7f) : Color.white);
            _body.text = "";

            float t = 0f;
            int n = 0;
            while (n < text.Length)
            {
                t += Time.unscaledDeltaTime * (shout ? 1.4f : 1f);
                n = Mathf.Min(text.Length, Mathf.FloorToInt(t * CharsPerSecond));
                _body.text = text.Substring(0, n);
                if (shout) _shakeKick = Mathf.Max(_shakeKick, 7f);
                if (Advance() && auto <= 0f) n = text.Length;
                yield return null;
            }
            _body.text = text;

            if (auto > 0f)
            {
                yield return Wait(auto);
            }
            else
            {
                _indicator.enabled = true;
                yield return null;
                while (!Advance())
                {
                    _indicator.color = new Color(1f, 1f, 1f, Mathf.PingPong(Time.unscaledTime * 2f, 1f));
                    yield return null;
                }
                _indicator.enabled = false;
            }
            _glitchSpeaker = false;
            _shoutGlitch = false;
        }

        private void HideDialog()
        {
            _dlg.SetActive(false);
            _glitchSpeaker = false;
            _shoutGlitch = false;
        }

        private static Color SpeakerColor(string who)
        {
            switch (who)
            {
                case "Hacker Anónimo": return HackerColor;
                case "Iker": return IkerColor;
                case "Hannah": return HannahColor;
                case "???": return UnknownColor;
                default: return SystemColor;
            }
        }

        private void SetPortrait(string who)
        {
            switch (who)
            {
                case "Iker":           _portraitKind = 1; _portrait.texture = _texIker; break;
                case "Hannah":         _portraitKind = 2; _portrait.texture = _texHannah; break;
                case "Hacker Anónimo": _portraitKind = 3; _portrait.texture = _texHacker; break;
                case "???":            _portraitKind = 4; break;
                default:               _portraitKind = 0; break;
            }
            _portrait.gameObject.SetActive(_portraitKind >= 1 && _portraitKind <= 3 && _portrait.texture != null);
            _noisePortrait.gameObject.SetActive(_portraitKind == 4);
        }

        private static bool Advance()
        {
            var kb = Keyboard.current;
            var ms = Mouse.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                               kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                               kb.zKey.wasPressedThisFrame)) return true;
            return ms != null && ms.leftButton.wasPressedThisFrame;
        }

        // =====================================================================
        //  ESCENA / PERSONAJES
        // =====================================================================
        private void PlaceChars(Vector2 pos, float scale)
        {
            SetChar(_ikerRT, pos + new Vector2(-34f * scale, 0f), scale);
            SetChar(_hannahRT, pos + new Vector2(34f * scale, 0f), scale);
            _iker.color = Color.white; _hannah.color = Color.white;
        }

        private static void SetChar(RectTransform rt, Vector2 feetPos, float scale)
        {
            rt.anchoredPosition = feetPos;
            rt.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetDoor(DoorMode m)
        {
            _doorMode = m;
            _door.gameObject.SetActive(m != DoorMode.None);
            _doorFrame.gameObject.SetActive(m != DoorMode.None);
        }

        private void SetHp(float iker, float hannah)
        {
            _hpIker = iker; _hpHannah = hannah;
            _ikerBar.sizeDelta = new Vector2(236f * Mathf.Clamp01(iker), 12f);
            _hannahBar.sizeDelta = new Vector2(236f * Mathf.Clamp01(hannah), 12f);
        }

        private void SetFade(float a)
        {
            _fade.color = new Color(0f, 0f, 0f, a);
        }

        private IEnumerator FadeTo(float target, float dur)
        {
            float start = _fade.color.a;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(start, target, t / dur));
                yield return null;
            }
            SetFade(target);
        }

        // =====================================================================
        //  UPDATE: ruido, shake, animación
        // =====================================================================
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Shake del escenario
            _shakeKick = Mathf.MoveTowards(_shakeKick, 0f, dt * 40f);
            float amp = Mathf.Max(_shakeBase, _shakeKick);
            if (_stage != null)
                _stage.anchoredPosition = amp > 0.01f ? (Vector2)(Random.insideUnitCircle * amp) : Vector2.zero;

            // Overlay de daño
            if (_damage != null)
            {
                var c = _damage.color;
                c.a = Mathf.MoveTowards(c.a, _damageAlpha, dt * 2f);
                _damage.color = c;
            }

            // Animación de sprites (2 frames lado a lado)
            _animTimer += dt;
            float period = _walking ? 0.16f : 0.55f;
            if (_animTimer >= period)
            {
                _animTimer = 0f;
                _frame = 1 - _frame;
                Rect uv = new Rect(_frame * 0.5f, 0f, 0.5f, 1f);
                if (_iker != null) _iker.uvRect = uv;
                if (_hannah != null) _hannah.uvRect = uv;
                if (_portrait != null) _portrait.uvRect = uv;
            }

            // Ruido de la puerta y del retrato "???"
            _noiseTimer += dt;
            if (_noiseTimer >= 0.05f)
            {
                _noiseTimer = 0f;
                if (_doorMode != DoorMode.None) UpdateDoorNoise();
                if (_portraitKind == 4) UpdatePortraitNoise();
            }

            // Glitch del nombre / texto de "???"
            if (_speaker != null)
            {
                _speaker.rectTransform.anchoredPosition = _glitchSpeaker && Random.value < 0.35f
                    ? new Vector2(160f, -14f) + Random.insideUnitCircle * 4f
                    : new Vector2(160f, -14f);
            }
            if (_body != null)
            {
                _body.rectTransform.anchoredPosition = _shoutGlitch
                    ? new Vector2(160f, -56f) + Random.insideUnitCircle * 5f
                    : new Vector2(160f, -56f);
            }
        }

        private void UpdateDoorNoise()
        {
            float time = Time.unscaledTime;
            bool red = _doorMode == DoorMode.Red || _doorMode == DoorMode.RedAlert;
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 5f);
            float bar = Mathf.Repeat(time * 0.9f, 1f) * DoorH;

            for (int y = 0; y < DoorH; y++)
            {
                float band = Mathf.Abs(y - bar) < 3f ? 1.5f : 1f;
                bool tear = Random.value < 0.04f;
                for (int x = 0; x < DoorW; x++)
                {
                    float v = Random.value;
                    if (tear) v = Mathf.Max(v, 0.8f);
                    byte r, g, b;
                    if (red)
                    {
                        float k = v * band * (_doorMode == DoorMode.RedAlert ? 1f : pulse);
                        r = (byte)Mathf.Clamp(k * 255f, 0, 255);
                        g = (byte)Mathf.Clamp(k * 38f * (Random.value < 0.02f ? 6f : 1f), 0, 255);
                        b = (byte)Mathf.Clamp(k * 32f, 0, 255);
                    }
                    else
                    {
                        // Puerta verde: resplandor suave con poco ruido.
                        float cx = Mathf.Abs(x - DoorW * 0.5f) / (DoorW * 0.5f);
                        float glow = Mathf.Lerp(1f, 0.55f, cx) * pulse;
                        float k = Mathf.Clamp01(glow * (0.78f + 0.22f * v) * band);
                        r = (byte)(k * 60f);
                        g = (byte)(k * 255f);
                        b = (byte)(k * 110f);
                    }
                    _doorPx[y * DoorW + x] = new Color32(r, g, b, 255);
                }
            }
            _doorTex.SetPixels32(_doorPx);
            _doorTex.Apply(false);
        }

        private void UpdatePortraitNoise()
        {
            for (int y = 0; y < PortH; y++)
            {
                for (int x = 0; x < PortW; x++)
                {
                    float v = Random.value;
                    // Silueta misteriosa en el centro.
                    float dx = (x - PortW * 0.5f) / (PortW * 0.32f);
                    float dy = (y - PortH * 0.45f) / (PortH * 0.38f);
                    bool sil = dx * dx + dy * dy < 1f || (y < PortH * 0.28f && Mathf.Abs(dx) < 1.6f);
                    if (sil) v *= 0.25f;
                    byte r = (byte)(v * 255f);
                    _portraitPx[y * PortW + x] = new Color32(r, (byte)(v * 45f), (byte)(v * 45f), 255);
                }
            }
            _portraitNoiseTex.SetPixels32(_portraitPx);
            _portraitNoiseTex.Apply(false);
        }

        // =====================================================================
        //  CONSTRUCCIÓN DE LA UI
        // =====================================================================
        private static readonly Vector2 C = new Vector2(0.5f, 0.5f);

        private RectTransform MakeRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        private RectTransform Stretch(string name, Transform parent)
        {
            return MakeRect(name, parent, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero);
        }

        private Image MakeImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        private RawImage MakeRaw(RectTransform rt, Texture tex)
        {
            var r = rt.gameObject.AddComponent<RawImage>();
            r.texture = tex;
            r.raycastTarget = false;
            return r;
        }

        private Text MakeText(RectTransform rt, string s, int size, Color c, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Texture2D MakeTex(int w, int h, FilterMode f = FilterMode.Point)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = f, wrapMode = TextureWrapMode.Clamp };
        }

        private static Texture2D Load(string name)
        {
            var t = Resources.Load<Texture2D>("Cinematic/" + name);
            if (t != null) t.filterMode = FilterMode.Point;
            return t;
        }

        private void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _texIker = Load("Iker_Derecha");
            _texHannah = Load("Hannah_Derecha");
            _texHacker = Load("Hacker_Idle");

            // Canvas
            var cgo = new GameObject("Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)cgo.transform;

            // Fondo negro permanente (por si el pasillo parpadea)
            MakeImage(Stretch("Black", root), Color.black);

            // ---- Escenario
            _stage = Stretch("Stage", root);
            _stage.offsetMin = new Vector2(-24f, -24f);   // margen extra para que el shake no deje bordes
            _stage.offsetMax = new Vector2(24f, 24f);

            _hallTexA = BuildHall(new Color(0.09f, 0.09f, 0.17f), new Color(0.07f, 0.07f, 0.12f),
                                  new Color(0.05f, 0.05f, 0.09f), new Color(0.75f, 0.04f, 0.04f), new Color(0.55f, 0.15f, 0.2f));
            _hallTexB = BuildHall(new Color(0.07f, 0.13f, 0.14f), new Color(0.05f, 0.1f, 0.11f),
                                  new Color(0.04f, 0.07f, 0.08f), new Color(0.05f, 0.55f, 0.22f), new Color(0.2f, 0.65f, 0.5f));
            _hall = MakeRaw(Stretch("Hall", _stage), _hallTexA);

            _doorFrame = MakeImage(MakeRect("DoorFrame", _stage, C, C, C, new Vector2(0f, 55f), new Vector2(172f, 218f)), new Color(0.05f, 0.05f, 0.07f));
            _doorTex = MakeTex(DoorW, DoorH);
            _doorPx = new Color32[DoorW * DoorH];
            _door = MakeRaw(MakeRect("Door", _stage, C, C, C, new Vector2(0f, 55f), new Vector2(150f, 200f)), _doorTex);

            _iker = MakeCharacter("Iker", _texIker, out _ikerRT);
            _hannah = MakeCharacter("Hannah", _texHannah, out _hannahRT);

            // ---- Overlay de daño
            _damage = MakeImage(Stretch("Damage", root), new Color(0.9f, 0.05f, 0.05f, 0f));

            // ---- Scanlines
            _scanTex = MakeTex(1, 4);
            _scanTex.wrapMode = TextureWrapMode.Repeat;
            _scanTex.SetPixels32(new[]
            {
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 70)
            });
            _scanTex.Apply(false);
            var scan = MakeRaw(Stretch("Scanlines", root), _scanTex);
            scan.uvRect = new Rect(0, 0, 1, 135);

            // ---- HUD de vida
            _hud = MakeRect("HUD", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(300f, 80f)).gameObject;
            _ikerBar = MakeBar(_hud.transform, "IKER", 0f, IkerColor);
            _hannahBar = MakeBar(_hud.transform, "HANNAH", -34f, HannahColor);

            // ---- Caja de diálogo
            var dlgRT = MakeRect("Dialog", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(900f, 150f));
            _dlg = dlgRT.gameObject;
            MakeImage(dlgRT, new Color(0.02f, 0.03f, 0.08f, 0.94f));
            var ol = _dlg.AddComponent<Outline>();
            ol.effectColor = new Color(0.4f, 0.85f, 1f, 0.9f);
            ol.effectDistance = new Vector2(2f, -2f);

            var frameRT = MakeRect("PortraitFrame", dlgRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(124f, 124f));
            MakeImage(frameRT, new Color(0.08f, 0.1f, 0.18f, 1f));
            _portrait = MakeRaw(MakeRect("Portrait", frameRT, C, C, C, Vector2.zero, new Vector2(104f, 104f)), null);
            _portrait.uvRect = new Rect(0f, 0f, 0.5f, 1f);
            _portrait.rectTransform.sizeDelta = new Vector2(104f * 20f / 23f * 1.0f, 104f);
            _portraitNoiseTex = MakeTex(PortW, PortH);
            _portraitPx = new Color32[PortW * PortH];
            _noisePortrait = MakeRaw(MakeRect("PortraitNoise", frameRT, C, C, C, Vector2.zero, new Vector2(104f, 104f)), _portraitNoiseTex);

            _speaker = MakeText(MakeRect("Speaker", dlgRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -14f), new Vector2(700f, 36f)),
                                "", 28, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            _body = MakeText(MakeRect("Body", dlgRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -56f), new Vector2(720f, 90f)),
                             "", 26, Color.white, TextAnchor.UpperLeft);
            _indicator = MakeText(MakeRect("Indicator", dlgRT, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 8f), new Vector2(40f, 30f)),
                                  "▼", 22, Color.white, TextAnchor.LowerRight);
            _indicator.enabled = false;

            // ---- Caja del sistema (SÍ / NO)
            var cbRT = MakeRect("ChoiceBox", root, C, C, C, new Vector2(0f, 10f), new Vector2(520f, 240f));
            _choiceBox = cbRT.gameObject;
            MakeImage(cbRT, new Color(0f, 0f, 0f, 0.96f));
            var cbo = _choiceBox.AddComponent<Outline>();
            cbo.effectColor = new Color(1f, 1f, 1f, 0.95f);
            cbo.effectDistance = new Vector2(3f, -3f);
            MakeText(MakeRect("Title", cbRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(0f, 30f)),
                     "[ CAJA DE TEXTO DEL SISTEMA ]", 20, new Color(0.5f, 0.8f, 1f), TextAnchor.UpperCenter);
            MakeText(MakeRect("Question", cbRT, C, C, C, new Vector2(0f, 24f), new Vector2(500f, 80f)),
                     "¿HACERLO?", 64, new Color(1f, 0.25f, 0.25f), TextAnchor.MiddleCenter, FontStyle.Bold);

            _yesRT = MakeRect("Yes", cbRT, C, C, C, new Vector2(-110f, -62f), new Vector2(170f, 56f));
            _yesBg = MakeImage(_yesRT, Color.black);
            _yesText = MakeText(Stretch("Label", _yesRT), "SÍ", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _noRT = MakeRect("No", cbRT, C, C, C, new Vector2(110f, -62f), new Vector2(170f, 56f));
            _noBg = MakeImage(_noRT, Color.black);
            _noText = MakeText(Stretch("Label", _noRT), "NO", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            // ---- Fade y texto grande
            _fade = MakeImage(Stretch("Fade", root), new Color(0f, 0f, 0f, 1f));
            _bigText = MakeText(MakeRect("BigText", root, C, C, C, Vector2.zero, new Vector2(900f, 300f)),
                                "", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _bigText.gameObject.SetActive(false);

            SetHp(1f, 1f);
        }

        private RawImage MakeCharacter(string name, Texture2D tex, out RectTransform rt)
        {
            rt = MakeRect(name, _stage, C, C, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(100f, 115f));
            var r = MakeRaw(rt, tex);
            r.uvRect = new Rect(0f, 0f, 0.5f, 1f);
            if (tex == null) r.color = new Color(1f, 1f, 1f, 0f);
            return r;
        }

        private RectTransform MakeBar(Transform parent, string label, float y, Color col)
        {
            MakeText(MakeRect(label + "_Label", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(80f, 22f)),
                     label, 18, col, TextAnchor.UpperLeft, FontStyle.Bold);
            var bg = MakeRect(label + "_BarBg", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, y - 4f), new Vector2(240f, 16f));
            MakeImage(bg, new Color(0.1f, 0.1f, 0.15f, 0.9f));
            var fill = MakeRect(label + "_BarFill", bg, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(236f, 12f));
            MakeImage(fill, col);
            return fill;
        }

        // Pasillo en perspectiva dibujado a 192x108 píxeles.
        private static Texture2D BuildHall(Color wall, Color floor, Color ceil, Color glow, Color lineCol)
        {
            const int W = 192, H = 108;
            var tex = MakeTex(W, H);
            var px = new Color32[W * H];
            const float vx = 96f, vy = 70f, hw = 20f, hh = 25f;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float dx = x - vx, dy = y - vy;
                    float fx = Mathf.Abs(dx) / hw, fy = Mathf.Abs(dy) / hh;
                    float f = Mathf.Max(fx, fy);
                    Color c;
                    if (f < 1f)
                    {
                        c = Color.Lerp(wall, Color.black, 0.6f);
                    }
                    else
                    {
                        float depth = Mathf.Clamp01((f - 1f) / 5f);
                        float shade = Mathf.Lerp(0.35f, 1f, depth);
                        bool side = fx > fy;
                        float u = 1f / f;
                        c = side ? wall : (dy < 0f ? floor : ceil);

                        float ln = Mathf.Repeat(u * 7f, 1f);
                        if (ln < 0.07f) c = Color.Lerp(c, lineCol, 0.55f);

                        // Luces del techo
                        if (!side && dy > 0f && Mathf.Repeat(u * 3.5f, 1f) < 0.16f && Mathf.Abs(dx) < hw * f * 0.28f)
                            c = Color.Lerp(c, new Color(0.8f, 0.85f, 1f), 0.8f);

                        c = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                        c += glow * Mathf.Pow(1f - depth, 3f) * 0.45f;
                    }
                    c.a = 1f;
                    px[y * W + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }
    }
}
