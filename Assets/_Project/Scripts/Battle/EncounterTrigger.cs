using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamHacks.Battle
{
    /// <summary>
    /// Trigger que vive como hijo del enemigo (BoxCollider2D isTrigger).
    /// Al colisionar con el Player: setea BattleState con el id del enemigo
    /// (su padre), la escena actual y la posición del Player para el regreso,
    /// y carga la escena "Battle" en modo Single.
    /// Si ya hay una batalla en curso, no hace nada (evita doble disparo).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EncounterTrigger : MonoBehaviour
    {
        [Tooltip("Clave del enemigo en stats.json. Si vacío, usa el nombre del GO padre.")]
        [SerializeField] private string enemyId;
        [Tooltip("Nombre de la escena de batalla en Build Settings.")]
        [SerializeField] private string battleScene = "Battle";

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (BattleState.InBattle) return;
            if (!other.CompareTag("Player")) return;

            string id = string.IsNullOrEmpty(enemyId) ? "Slime" : enemyId;
            string goName = transform.parent != null ? transform.parent.name : gameObject.name;

            BattleState.StartEncounter(
                id,
                goName,
                SceneManager.GetActiveScene().name,
                other.transform.position);

            SceneManager.LoadScene(battleScene);
        }
    }
}
