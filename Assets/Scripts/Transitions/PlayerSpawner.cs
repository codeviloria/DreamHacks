using UnityEngine;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Al cargar la escena coloca al jugador en el SpawnPoint cuyo id coincida
    /// con TransitionState.NextSpawnId. Si no coincide ninguno, usa el que
    /// tenga id "default". Si no hay default, deja al jugador donde estaba.
    /// Pensado para ir en un GameObject vacío de cada escena jugable.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Tooltip("Tag con el que se busca al jugador. Por defecto 'Player'.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Id usado si TransitionState.NextSpawnId está vacío.")]
        [SerializeField] private string defaultSpawnId = "default";

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;

            var wanted = TransitionState.NextSpawnId;
            // Consumimos el valor para que no se reuse por accidente en la próxima carga.
            TransitionState.NextSpawnId = null;

            SpawnPoint chosen = FindById(wanted);
            if (chosen == null) chosen = FindById(defaultSpawnId);
            if (chosen == null) return;

            var p = chosen.transform.position;
            player.transform.position = new Vector3(p.x, p.y, player.transform.position.z);

            // Si el jugador trae físicas, reseteamos la velocidad para que no
            // llegue a la nueva escena con el impulso del trigger anterior.
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Sincroniza el checkpoint del PlayerRespawn (metroidvania) a la
            // nueva posición. Sin esto, cuando el jugador cae por un hueco en
            // Bioma1, revive en la posición del prefab original (1.5, 2), no
            // en el spawn por el que entró a la escena.
            var respawnType = System.Type.GetType("PlayerRespawn, Assembly-CSharp");
            if (respawnType != null)
            {
                var respawn = player.GetComponent(respawnType);
                if (respawn != null)
                {
                    var field = respawnType.GetField("checkpointPosition",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(respawn, (Vector2)player.transform.position);
                }
            }
        }

        private static SpawnPoint FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var sp in Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude))
            {
                if (sp.SpawnId == id) return sp;
            }
            return null;
        }
    }
}
