using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DreamHacks.Battle
{
    // QTE "flechas con palabras": muestra una secuencia aleatoria de 3-5 flechas
    // y espera que el jugador la ingrese con las teclas de flechas antes del timeout.
    // Dispara onResultado(exito).
    public class ComboQTE : MonoBehaviour
    {
        private RectTransform _root;
        private HorizontalLayoutGroup _layout;
        private Text _tituloTxt;
        private Text _siguienteTxt;
        private RectTransform _barraFillRt;
        private float _duracionActual;

        public event Action<bool> onResultado;
        public event Action onTimeout;

        private readonly List<Flecha> _secuencia = new List<Flecha>();
        private readonly List<Text> _flechasTxt = new List<Text>();
        private int _indice;
        private Coroutine _timer;
        private bool _activo;
        private int _graceDeadlineFrame;

        private enum Flecha { Izquierda, Arriba, Derecha, Abajo }

        public void Build(Transform parent)
        {
            var go = new GameObject("ComboQTE", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.85f);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0.1f, 0.3f);
            _root.anchorMax = new Vector2(0.9f, 0.75f);
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            _tituloTxt = CrearTexto(_root, "Titulo", new Vector2(0.05f, 0.88f), new Vector2(0.55f, 0.98f), TextAnchor.MiddleLeft, 26);
            _tituloTxt.text = "¡Combo!";

            // Cartel grande "SIGUIENTE: ↓" para que el jugador vea qué flecha viene.
            _siguienteTxt = CrearTexto(_root, "Siguiente", new Vector2(0.55f, 0.82f), new Vector2(0.95f, 0.99f), TextAnchor.MiddleRight, 44);
            _siguienteTxt.color = new Color(1f, 0.9f, 0.3f);
            _siguienteTxt.text = "";

            // Barra de tiempo (fondo + fill).
            var barraBG = new GameObject("BarraBG", typeof(RectTransform), typeof(Image));
            barraBG.transform.SetParent(_root, false);
            var bRt = (RectTransform)barraBG.transform;
            bRt.anchorMin = new Vector2(0.05f, 0.73f);
            bRt.anchorMax = new Vector2(0.95f, 0.8f);
            bRt.offsetMin = bRt.offsetMax = Vector2.zero;
            barraBG.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barraBG.transform, false);
            var fRt = (RectTransform)fillGO.transform;
            fRt.anchorMin = new Vector2(0f, 0f);
            fRt.anchorMax = new Vector2(1f, 1f);
            fRt.pivot = new Vector2(0f, 0.5f);
            fRt.offsetMin = new Vector2(2f, 2f); fRt.offsetMax = new Vector2(-2f, -2f);
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.color = new Color(0.4f, 1f, 0.5f, 1f);
            fillImg.raycastTarget = false;
            _barraFillRt = fRt;

            var contenedor = new GameObject("Flechas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contenedor.transform.SetParent(_root, false);
            var cRt = (RectTransform)contenedor.transform;
            cRt.anchorMin = new Vector2(0.05f, 0.1f);
            cRt.anchorMax = new Vector2(0.95f, 0.68f);
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;
            _layout = contenedor.GetComponent<HorizontalLayoutGroup>();
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.spacing = 20;
            _layout.childControlWidth = true;
            _layout.childControlHeight = true;
            _layout.childForceExpandWidth = true;
            _layout.childForceExpandHeight = true;

            SetVisible(false);
        }

        public void SetVisible(bool v)
        {
            if (_root != null) _root.gameObject.SetActive(v);
        }

        public void Iniciar(float tiempoSegundos = 4f)
        {
            if (_root == null) return;
            SetVisible(true);
            _indice = 0;
            _secuencia.Clear();
            int n = UnityEngine.Random.Range(3, 6);
            for (int i = 0; i < n; i++) _secuencia.Add((Flecha)UnityEngine.Random.Range(0, 4));
            Dibujar();
            ActualizarSiguiente();
            _duracionActual = Mathf.Max(0.5f, tiempoSegundos);
            SetBarraProgreso(1f);
            // Grace de 3 frames: evita que una tecla disparada el mismo frame
            // que se abrió el combo (p.ej. click con Enter en el botón) se cuente.
            _graceDeadlineFrame = Time.frameCount + 3;
            _stickUltimo = Vector2.zero;
            _activo = true;
            if (_timer != null) StopCoroutine(_timer);
            _timer = StartCoroutine(Temporizador(_duracionActual));
            Debug.Log("[QTE] Iniciar secuencia=" + string.Join(",", _secuencia) + " tiempo=" + _duracionActual + "s");
        }

        private void Update()
        {
            if (!_activo) return;
            PulsarActiva();
            if (_indice >= _secuencia.Count) return;
            if (Time.frameCount < _graceDeadlineFrame) return;
            Flecha? pulsada = LeerFlecha();
            if (!pulsada.HasValue) return;
            Flecha esperada = _secuencia[_indice];
            Debug.Log("[QTE] input=" + pulsada.Value + " esperada=" + esperada + " indice=" + _indice);
            if (pulsada.Value == esperada)
            {
                int idxAcertado = _indice;
                _indice++;
                PintarAcertada(idxAcertado);
                StartCoroutine(FlashAcertado(idxAcertado));
                ActualizarSiguiente();
                if (_indice >= _secuencia.Count) Terminar(true);
            }
            else
            {
                int idxFallado = _indice;
                StartCoroutine(FlashFallado(idxFallado));
                Terminar(false);
            }
        }

        private void ActualizarSiguiente()
        {
            if (_siguienteTxt == null) return;
            if (_indice >= 0 && _indice < _secuencia.Count)
                _siguienteTxt.text = "SIGUIENTE: " + Simbolo(_secuencia[_indice]);
            else
                _siguienteTxt.text = "";
        }

        private void PulsarActiva()
        {
            if (_indice < 0 || _indice >= _flechasTxt.Count) return;
            for (int i = 0; i < _flechasTxt.Count; i++)
            {
                if (_flechasTxt[i] == null) continue;
                var rt = _flechasTxt[i].rectTransform;
                if (i == _indice)
                {
                    float s = 1f + 0.12f * Mathf.PingPong(Time.time * 3f, 1f);
                    rt.localScale = new Vector3(s, s, 1f);
                }
                else if (i > _indice)
                {
                    rt.localScale = Vector3.one;
                }
            }
        }

        private void PintarAcertada(int i)
        {
            if (i < 0 || i >= _flechasTxt.Count) return;
            var t = _flechasTxt[i];
            if (t != null) t.color = new Color(0.4f, 1f, 0.4f);
            // La palabra también (hermana del Text flecha).
            var card = t != null ? t.transform.parent : null;
            if (card != null)
            {
                for (int k = 0; k < card.childCount; k++)
                {
                    var txt = card.GetChild(k).GetComponent<Text>();
                    if (txt != null) txt.color = new Color(0.4f, 1f, 0.4f);
                }
            }
        }

        private void Dibujar()
        {
            if (_layout == null) return;
            foreach (Transform c in _layout.transform)
            {
                if (Application.isPlaying) Destroy(c.gameObject);
                else DestroyImmediate(c.gameObject);
            }
            _flechasTxt.Clear();
            for (int i = 0; i < _secuencia.Count; i++)
            {
                var card = new GameObject("F" + i, typeof(RectTransform), typeof(VerticalLayoutGroup));
                card.transform.SetParent(_layout.transform, false);
                var v = card.GetComponent<VerticalLayoutGroup>();
                v.childAlignment = TextAnchor.MiddleCenter;
                v.spacing = 4;
                v.childControlWidth = true;
                v.childControlHeight = true;
                v.childForceExpandHeight = true;
                v.childForceExpandWidth = true;

                var flecha = CrearTexto(card.transform, "Flecha", default, default, TextAnchor.MiddleCenter, 60);
                flecha.text = Simbolo(_secuencia[i]);
                flecha.color = i < _indice ? new Color(0.4f, 1f, 0.4f) : Color.white;
                _flechasTxt.Add(flecha);

                var palabra = CrearTexto(card.transform, "Palabra", default, default, TextAnchor.MiddleCenter, 20);
                palabra.text = Palabra(_secuencia[i]);
                palabra.color = i < _indice ? new Color(0.4f, 1f, 0.4f) : new Color(0.8f, 0.85f, 0.95f);
            }
        }

        private IEnumerator FlashAcertado(int idx)
        {
            if (idx < 0 || idx >= _flechasTxt.Count) yield break;
            var t = _flechasTxt[idx];
            if (t == null) yield break;
            var rt = t.rectTransform;
            // Pop grande 0.3s + flash blanco → verde brillante.
            float dur = 0.35f, elapsed = 0f;
            Vector3 baseScale = Vector3.one;
            while (elapsed < dur)
            {
                float p = elapsed / dur;
                float s = Mathf.Lerp(1.8f, 1f, p);
                rt.localScale = baseScale * s;
                t.color = Color.Lerp(Color.white, new Color(0.4f, 1f, 0.4f), p);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.localScale = baseScale;
            t.color = new Color(0.4f, 1f, 0.4f);
        }

        private IEnumerator FlashFallado(int idx)
        {
            if (idx < 0 || idx >= _flechasTxt.Count) yield break;
            var t = _flechasTxt[idx];
            if (t == null) yield break;
            var rt = t.rectTransform;
            Vector3 baseScale = Vector3.one;
            Vector2 origen = rt.anchoredPosition;
            for (int k = 0; k < 4; k++)
            {
                rt.anchoredPosition = origen + new Vector2(10f, 0f);
                t.color = new Color(1f, 0.2f, 0.2f);
                yield return new WaitForSeconds(0.04f);
                rt.anchoredPosition = origen - new Vector2(10f, 0f);
                yield return new WaitForSeconds(0.04f);
            }
            rt.anchoredPosition = origen;
        }

        private IEnumerator Temporizador(float dur)
        {
            float t = 0f;
            while (t < dur && _activo)
            {
                SetBarraProgreso(Mathf.Clamp01(1f - t / dur));
                t += Time.deltaTime;
                yield return null;
            }
            if (_activo)
            {
                SetBarraProgreso(0f);
                bool exito = _indice >= _secuencia.Count;
                if (!exito)
                {
                    Debug.Log("[QTE] Timeout. Flechas acertadas: " + _indice + "/" + _secuencia.Count);
                    onTimeout?.Invoke();
                }
                Terminar(exito);
            }
        }

        private void SetBarraProgreso(float p)
        {
            if (_barraFillRt == null) return;
            _barraFillRt.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
        }

        private void Terminar(bool exito)
        {
            _activo = false;
            if (_timer != null) { StopCoroutine(_timer); _timer = null; }
            SetVisible(false);
            onResultado?.Invoke(exito);
        }

        // Umbral del stick: tiene que cruzarse para contar un "paso".
        private const float StickUmbral = 0.6f;
        private Vector2 _stickUltimo; // para detectar edge cross (como wasPressedThisFrame)

        private Flecha? LeerFlecha()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) return Flecha.Izquierda;
                if (kb.upArrowKey.wasPressedThisFrame) return Flecha.Arriba;
                if (kb.rightArrowKey.wasPressedThisFrame) return Flecha.Derecha;
                if (kb.downArrowKey.wasPressedThisFrame) return Flecha.Abajo;
                // Soporte extra WASD.
                if (kb.aKey.wasPressedThisFrame) return Flecha.Izquierda;
                if (kb.wKey.wasPressedThisFrame) return Flecha.Arriba;
                if (kb.dKey.wasPressedThisFrame) return Flecha.Derecha;
                if (kb.sKey.wasPressedThisFrame) return Flecha.Abajo;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                // D-pad (edge-triggered).
                if (gp.dpad.left.wasPressedThisFrame) return Flecha.Izquierda;
                if (gp.dpad.up.wasPressedThisFrame) return Flecha.Arriba;
                if (gp.dpad.right.wasPressedThisFrame) return Flecha.Derecha;
                if (gp.dpad.down.wasPressedThisFrame) return Flecha.Abajo;
                // Stick izquierdo: cuenta cuando cruza el umbral desde el centro.
                Vector2 stick = gp.leftStick.ReadValue();
                Flecha? disparo = StickCruzaUmbral(_stickUltimo, stick);
                _stickUltimo = stick;
                if (disparo.HasValue) return disparo;
            }
            return null;
#else
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return Flecha.Izquierda;
            if (Input.GetKeyDown(KeyCode.UpArrow)   || Input.GetKeyDown(KeyCode.W)) return Flecha.Arriba;
            if (Input.GetKeyDown(KeyCode.RightArrow)|| Input.GetKeyDown(KeyCode.D)) return Flecha.Derecha;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return Flecha.Abajo;
            return null;
#endif
        }

        private static Flecha? StickCruzaUmbral(Vector2 anterior, Vector2 actual)
        {
            // Horizontal.
            if (Mathf.Abs(anterior.x) < StickUmbral && actual.x <= -StickUmbral) return Flecha.Izquierda;
            if (Mathf.Abs(anterior.x) < StickUmbral && actual.x >=  StickUmbral) return Flecha.Derecha;
            // Vertical.
            if (Mathf.Abs(anterior.y) < StickUmbral && actual.y >=  StickUmbral) return Flecha.Arriba;
            if (Mathf.Abs(anterior.y) < StickUmbral && actual.y <= -StickUmbral) return Flecha.Abajo;
            return null;
        }

        private static string Simbolo(Flecha f)
        {
            switch (f)
            {
                case Flecha.Izquierda: return "←";
                case Flecha.Arriba: return "↑";
                case Flecha.Derecha: return "→";
                case Flecha.Abajo: return "↓";
            }
            return "?";
        }

        private static string Palabra(Flecha f)
        {
            switch (f)
            {
                case Flecha.Izquierda: return "izquierda";
                case Flecha.Arriba: return "arriba";
                case Flecha.Derecha: return "derecha";
                case Flecha.Abajo: return "abajo";
            }
            return "?";
        }

        private static Text CrearTexto(Transform parent, string name, Vector2 aMin, Vector2 aMax, TextAnchor align, int fontSize)
        {
            var t = new GameObject(name, typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = align;
            t.color = Color.white;
            t.fontSize = fontSize;
            if (aMin == default && aMax == default) return t;
            var rt = t.rectTransform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return t;
        }
    }
}
