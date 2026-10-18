using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DreamHacks.Combate;

namespace DreamHacks.UI
{
    // Dibuja una barra de corazones basada en una fuente IVida en cuartos.
    // Construye los Image hijos en Awake. Soporta tint (naranja para enemigo).
    // Al OnDanio: shake + flash. Con <= 2 corazones hace latir el ultimo vivo.
    public class HUDCorazones : MonoBehaviour
    {
        [Header("Fuente de vida (opcional en inspector; se puede setear por codigo)")]
        [SerializeField] private MonoBehaviour vidaMB;

        [Header("Sprites de corazon (9x9)")]
        [SerializeField] private Sprite lleno;
        [SerializeField] private Sprite tresCuartos;
        [SerializeField] private Sprite medio;
        [SerializeField] private Sprite unCuarto;
        [SerializeField] private Sprite vacio;

        [Header("Layout")]
        [SerializeField, Min(4f)] private float tamano = 36f;
        [SerializeField, Min(0f)] private float separacion = 4f;
        [SerializeField, Min(1)] private int porFila = 10;
        [SerializeField] private Color tint = Color.white;

        [Header("Efectos")]
        [SerializeField, Min(0f)] private float shakeDuracion = 0.2f;
        [SerializeField, Min(0f)] private float shakeAmplitud = 6f;
        [SerializeField, Min(0f)] private float flashDuracion = 0.08f;

        private IVida _vida;
        private Image[] _slots;
        private RectTransform _rt;
        private Vector2 _basePos;
        private Coroutine _efecto;
        private Coroutine _latido;
        private int _ultimoCuartos = -1;

        public void SetVida(IVida v)
        {
            Desuscribir();
            _vida = v;
            Reconstruir();
            Suscribir();
            _ultimoCuartos = _vida != null ? _vida.CuartosActual : -1;
            ActualizarIconos();
        }

        private void Update()
        {
            if (_vida == null) return;
            int c = _vida.CuartosActual;
            if (c != _ultimoCuartos)
            {
                bool bajo = _ultimoCuartos >= 0 && c < _ultimoCuartos;
                _ultimoCuartos = c;
                ActualizarIconos();
                if (bajo) JugarEfectoDanio();
            }
        }

        [System.Serializable]
        public struct Config
        {
            public Sprite lleno, tresCuartos, medio, unCuarto, vacio;
            public Color tint;
            public float tamano, separacion;
            public int porFila;
        }

        public void Configurar(Config cfg)
        {
            if (cfg.lleno != null) lleno = cfg.lleno;
            if (cfg.tresCuartos != null) tresCuartos = cfg.tresCuartos;
            if (cfg.medio != null) medio = cfg.medio;
            if (cfg.unCuarto != null) unCuarto = cfg.unCuarto;
            if (cfg.vacio != null) vacio = cfg.vacio;
            if (cfg.tamano > 0f) tamano = cfg.tamano;
            if (cfg.separacion >= 0f) separacion = cfg.separacion;
            if (cfg.porFila > 0) porFila = cfg.porFila;
            if (cfg.tint.a > 0f) tint = cfg.tint;
            Reconstruir();
            ActualizarIconos();
        }

        private void Awake()
        {
            _rt = (RectTransform)transform;
            if (vidaMB is IVida iv) _vida = iv;
            Reconstruir();
            Suscribir();
            ActualizarIconos();
            _basePos = _rt.anchoredPosition;
        }

        private void OnEnable() { Suscribir(); ActualizarIconos(); }
        private void OnDisable() { Desuscribir(); }

        private void Suscribir()
        {
            if (_vida == null) return;
            _vida.OnCambio.AddListener(ActualizarIconos);
            _vida.OnDanio.AddListener(JugarEfectoDanio);
        }

        private void Desuscribir()
        {
            if (_vida == null) return;
            _vida.OnCambio.RemoveListener(ActualizarIconos);
            _vida.OnDanio.RemoveListener(JugarEfectoDanio);
        }

        private void Reconstruir()
        {
            foreach (Transform c in transform)
            {
                if (Application.isPlaying) Destroy(c.gameObject);
                else DestroyImmediate(c.gameObject);
            }
            if (_vida == null) { _slots = new Image[0]; return; }

            int corazones = Mathf.CeilToInt(_vida.CuartosMax / 4f);
            _slots = new Image[corazones];
            for (int i = 0; i < corazones; i++)
            {
                var go = new GameObject("H" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = tint;
                img.raycastTarget = false;
                var r = (RectTransform)go.transform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(tamano, tamano);
                int col = i % porFila;
                int row = i / porFila;
                r.anchoredPosition = new Vector2(col * (tamano + separacion), -row * (tamano + separacion));
                _slots[i] = img;
            }
        }

        private void ActualizarIconos()
        {
            if (_vida == null || _slots == null) return;
            int cuartos = _vida.CuartosActual;
            for (int i = 0; i < _slots.Length; i++)
            {
                int base4 = i * 4;
                int frac = Mathf.Clamp(cuartos - base4, 0, 4);
                Sprite s;
                switch (frac)
                {
                    case 4: s = lleno; break;
                    case 3: s = tresCuartos; break;
                    case 2: s = medio; break;
                    case 1: s = unCuarto; break;
                    default: s = vacio; break;
                }
                if (s != null) _slots[i].sprite = s;
                _slots[i].color = tint;
                _slots[i].enabled = s != null;
            }
            ReiniciarLatido();
        }

        private void JugarEfectoDanio()
        {
            if (_efecto != null) StopCoroutine(_efecto);
            _efecto = StartCoroutine(ShakeYFlash());
        }

        private IEnumerator ShakeYFlash()
        {
            float t = 0f;
            float dur = Mathf.Max(shakeDuracion, flashDuracion);
            // Flash blanco.
            if (_slots != null)
                foreach (var s in _slots) if (s != null) s.color = Color.white;

            while (t < dur)
            {
                if (_rt != null && t < shakeDuracion)
                    _rt.anchoredPosition = _basePos + Random.insideUnitCircle * shakeAmplitud;
                if (t >= flashDuracion && _slots != null)
                    foreach (var s in _slots) if (s != null) s.color = tint;
                t += Time.deltaTime;
                yield return null;
            }
            if (_rt != null) _rt.anchoredPosition = _basePos;
            if (_slots != null)
                foreach (var s in _slots) if (s != null) s.color = tint;
            _efecto = null;
        }

        private void ReiniciarLatido()
        {
            if (_vida == null) return;
            int corazonesVivos = Mathf.CeilToInt(_vida.CuartosActual / 4f);
            bool deberia = corazonesVivos > 0 && corazonesVivos <= 2 && _vida.EstaVivo;
            if (deberia && _latido == null) _latido = StartCoroutine(Latir());
            else if (!deberia && _latido != null) { StopCoroutine(_latido); _latido = null; ResetScales(); }
        }

        private IEnumerator Latir()
        {
            while (_vida != null && _vida.CuartosActual > 0)
            {
                int idx = Mathf.CeilToInt(_vida.CuartosActual / 4f) - 1;
                if (idx < 0 || idx >= _slots.Length) break;
                var r = (RectTransform)_slots[idx].transform;
                float s = 1f + 0.15f * Mathf.PingPong(Time.time * 2f, 1f);
                r.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            ResetScales();
            _latido = null;
        }

        private void ResetScales()
        {
            if (_slots == null) return;
            foreach (var s in _slots) if (s != null) s.transform.localScale = Vector3.one;
        }
    }
}
