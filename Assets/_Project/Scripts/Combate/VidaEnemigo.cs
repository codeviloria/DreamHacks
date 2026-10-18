using UnityEngine;
using UnityEngine.Events;

namespace DreamHacks.Combate
{
    // Vida del enemigo en cuartos. Sin persistencia a BattleState: el enemigo se
    // crea y destruye por combate. corazonesMax=3 por default (12 cuartos),
    // pero BattleManager puede llamar Reconfigurar(cuartosMax) para sobreescribir
    // ("10 cuartos para los enemigos de este nivel").
    public class VidaEnemigo : MonoBehaviour, IVida
    {
        [SerializeField, Min(1)] private int corazonesMax = 3;
        [SerializeField, Min(1)] private int cuartosMaxOverride = -1;

        [SerializeField] private UnityEvent onCambio = new UnityEvent();
        [SerializeField] private UnityEvent onDanio = new UnityEvent();
        [SerializeField] private UnityEvent alMorir = new UnityEvent();

        public int CuartosMax => cuartosMaxOverride > 0 ? cuartosMaxOverride : corazonesMax * 4;
        public int CuartosActual => _cuartos;
        public bool EstaVivo => _cuartos > 0;

        public UnityEvent OnCambio => onCambio;
        public UnityEvent OnDanio => onDanio;
        public UnityEvent AlMorir => alMorir;

        private int _cuartos;
        private bool _murioNotificado;

        private void Awake()
        {
            _cuartos = CuartosMax;
        }

        public void Reconfigurar(int cuartosMax)
        {
            cuartosMaxOverride = Mathf.Max(1, cuartosMax);
            _cuartos = CuartosMax;
            _murioNotificado = false;
            onCambio?.Invoke();
        }

        public void RecibirDanio(int cuartos)
        {
            if (cuartos <= 0) return;
            if (_cuartos <= 0) return;
            _cuartos = Mathf.Max(0, _cuartos - cuartos);
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
            _cuartos = Mathf.Min(CuartosMax, _cuartos + cuartos);
            onCambio?.Invoke();
        }

        public void Set(int cuartos)
        {
            _cuartos = Mathf.Clamp(cuartos, 0, CuartosMax);
            onCambio?.Invoke();
        }
    }
}
