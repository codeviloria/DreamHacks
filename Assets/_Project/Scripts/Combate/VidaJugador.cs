using UnityEngine;
using UnityEngine.Events;
using DreamHacks.Battle;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DreamHacks.Combate
{
    // Vida del jugador en cuartos (1 corazon = 4 cuartos). Persistencia entre
    // overworld y Battle via BattleState.CurrentIkerHP (-1 = sin iniciar).
    public class VidaJugador : MonoBehaviour, IVida
    {
        [SerializeField, Min(1)] private int corazonesMax = 10;
        [Tooltip("Si true habilita K=1/4 danio, J=1 corazon danio, H=1 corazon cura.")]
        [SerializeField] private bool testKeys = true;

        [SerializeField] private UnityEvent onCambio = new UnityEvent();
        [SerializeField] private UnityEvent onDanio = new UnityEvent();
        [SerializeField] private UnityEvent alMorir = new UnityEvent();

        public int CuartosMax => corazonesMax * 4;
        public int CuartosActual => _cuartos;
        public bool EstaVivo => _cuartos > 0;

        public void SetTestKeysEnabled(bool v) { testKeys = v; }

        public UnityEvent OnCambio => onCambio;
        public UnityEvent OnDanio => onDanio;
        public UnityEvent AlMorir => alMorir;

        private int _cuartos;
        private bool _murioNotificado;

        private void Awake()
        {
            // Si BattleState tiene valor valido, lo adoptamos (vuelve del combate).
            if (BattleState.CurrentIkerHP > 0 && BattleState.CurrentIkerHP <= CuartosMax)
                _cuartos = BattleState.CurrentIkerHP;
            else
                _cuartos = CuartosMax;
            _murioNotificado = false;
        }

        private void Start()
        {
            onCambio?.Invoke();
        }

        private void Update()
        {
            if (!testKeys) return;
            if (DownKey(TestKey.Un) ) RecibirDanio(1);
            if (DownKey(TestKey.Corazon)) RecibirDanio(4);
            if (DownKey(TestKey.Cura)) Curar(4);
        }

        public void RecibirDanio(int cuartos)
        {
            if (cuartos <= 0) return;
            if (_cuartos <= 0) return;
            _cuartos = Mathf.Max(0, _cuartos - cuartos);
            BattleState.CurrentIkerHP = _cuartos;
            onDanio?.Invoke();
            onCambio?.Invoke();
            if (_cuartos == 0 && !_murioNotificado)
            {
                _murioNotificado = true;
                alMorir?.Invoke();
            }
        }

        public void Curar(int cuartos)
        {
            if (cuartos <= 0) return;
            if (_cuartos >= CuartosMax) return;
            _cuartos = Mathf.Min(CuartosMax, _cuartos + cuartos);
            BattleState.CurrentIkerHP = _cuartos;
            onCambio?.Invoke();
            if (_cuartos > 0) _murioNotificado = false;
        }

        public void Set(int cuartos)
        {
            _cuartos = Mathf.Clamp(cuartos, 0, CuartosMax);
            BattleState.CurrentIkerHP = _cuartos;
            onCambio?.Invoke();
        }

        public void Lleno()
        {
            Set(CuartosMax);
            _murioNotificado = false;
        }

        private enum TestKey { Un, Corazon, Cura }

        private static bool DownKey(TestKey k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case TestKey.Un: return kb.kKey.wasPressedThisFrame;
                case TestKey.Corazon: return kb.jKey.wasPressedThisFrame;
                case TestKey.Cura: return kb.hKey.wasPressedThisFrame;
            }
            return false;
#else
            switch (k)
            {
                case TestKey.Un: return Input.GetKeyDown(KeyCode.K);
                case TestKey.Corazon: return Input.GetKeyDown(KeyCode.J);
                case TestKey.Cura: return Input.GetKeyDown(KeyCode.H);
            }
            return false;
#endif
        }
    }
}
