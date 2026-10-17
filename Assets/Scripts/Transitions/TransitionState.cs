using UnityEngine;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Estado global de las transiciones de escena. Un simple canal estático que
    /// un SceneTransition escribe antes de cargar la próxima escena y que el
    /// PlayerSpawner lee tras el LoadScene para saber en qué SpawnPoint aparecer.
    /// </summary>
    public static class TransitionState
    {
        public static string NextSpawnId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnEnterPlayMode()
        {
            // El proyecto usa Enter Play Mode Options con DisableDomainReload:
            // los static no se reinician solos entre sesiones.
            NextSpawnId = null;
        }
    }
}
