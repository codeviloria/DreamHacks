using UnityEngine;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Marca un punto de aparición del jugador dentro de una escena. El id es
    /// el que un SceneTransition deja en TransitionState.NextSpawnId antes de
    /// cambiar de escena, y el PlayerSpawner usa para posicionar al player al
    /// cargar. Un id "default" sirve de fallback cuando no hay coincidencia.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Identificador único dentro de esta escena. Ej: \"bioma1_inicio\", \"salida_cueva\", \"default\".")]
        [SerializeField] private string spawnId;

        public string SpawnId => spawnId;

        private void OnDrawGizmos()
        {
            // Cruz amarilla visible en Scene View, incluso sin seleccionar.
            Gizmos.color = Color.yellow;
            var p = transform.position;
            Gizmos.DrawLine(p + Vector3.left * 0.3f, p + Vector3.right * 0.3f);
            Gizmos.DrawLine(p + Vector3.up * 0.3f, p + Vector3.down * 0.3f);
            Gizmos.DrawWireSphere(p, 0.25f);
        }
    }
}
