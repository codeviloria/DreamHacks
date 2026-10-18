using UnityEngine;
using UnityEngine.Events;
using DreamHacks.Battle;

namespace DreamHacks.Combate
{
    public enum TipoPocion { Roja, Azul, Rosa }

    // Inventario de pociones de Iker. Se mantiene entre escenas via BattleState
    // (InvRojas/Azules/Rosas) para que las ganadas en overworld queden
    // disponibles en Battle y viceversa.
    public class Inventario : MonoBehaviour
    {
        [SerializeField] private UnityEvent onCambio = new UnityEvent();

        public UnityEvent OnCambio => onCambio;

        public int Rojas => BattleState.InvRojas;
        public int Azules => BattleState.InvAzules;
        public int Rosas => BattleState.InvRosas;

        private void Start()
        {
            onCambio?.Invoke();
        }

        public int Count(TipoPocion t)
        {
            switch (t)
            {
                case TipoPocion.Roja: return BattleState.InvRojas;
                case TipoPocion.Azul: return BattleState.InvAzules;
                case TipoPocion.Rosa: return BattleState.InvRosas;
            }
            return 0;
        }

        public void Agregar(TipoPocion t, int n = 1)
        {
            if (n <= 0) return;
            switch (t)
            {
                case TipoPocion.Roja: BattleState.InvRojas += n; break;
                case TipoPocion.Azul: BattleState.InvAzules += n; break;
                case TipoPocion.Rosa: BattleState.InvRosas += n; break;
            }
            onCambio?.Invoke();
        }

        public bool Consumir(TipoPocion t)
        {
            if (Count(t) <= 0) return false;
            switch (t)
            {
                case TipoPocion.Roja: BattleState.InvRojas--; break;
                case TipoPocion.Azul: BattleState.InvAzules--; break;
                case TipoPocion.Rosa: BattleState.InvRosas--; break;
            }
            onCambio?.Invoke();
            return true;
        }
    }
}
