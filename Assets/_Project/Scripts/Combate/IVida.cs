using UnityEngine.Events;

namespace DreamHacks.Combate
{
    // Contrato comun para VidaJugador y VidaEnemigo: permite reusar HUDCorazones
    // tanto para la barra roja del Iker como para la naranja del enemigo.
    public interface IVida
    {
        int CuartosMax { get; }
        int CuartosActual { get; }
        bool EstaVivo { get; }

        UnityEvent OnCambio { get; }
        UnityEvent OnDanio { get; }
        UnityEvent AlMorir { get; }

        void RecibirDanio(int cuartos);
        void Curar(int cuartos);
        void Set(int cuartos);
    }
}
