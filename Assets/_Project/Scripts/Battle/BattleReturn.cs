using UnityEngine;

namespace DreamHacks.Battle
{
    /// <summary>
    /// Vive en el overworld (p.ej. Hospital). Cuando la escena se carga tras
    /// una batalla, destruye al enemigo correspondiente (si BattleState marcó
    /// uno para destruir) y teletransporta al Player a la posición guardada
    /// como "ReturnPosition".
    /// </summary>
    public class BattleReturn : MonoBehaviour
    {
        [SerializeField] private string enemiesRootName = "Enemigos";
        [Tooltip("Posición spawn para cuando el Iker pierde la batalla.")]
        [SerializeField] private Vector3 posicionInicio = new Vector3(40f, -18.94f, 0f);

        private void Start()
        {
            // Mover al Player a la posición de retorno.
            Vector3? destino = null;
            if (BattleState.VolverAInicio) destino = posicionInicio;
            else if (BattleState.ReturnPosition != Vector3.zero) destino = BattleState.ReturnPosition;

            if (destino.HasValue)
            {
                var pGO = GameObject.FindGameObjectWithTag("Player");
                if (pGO != null)
                {
                    var rb = pGO.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.position = destino.Value;
                    pGO.transform.position = destino.Value;
                }
            }
            // Consumir el flag para que no afecte al próximo retorno.
            BattleState.VolverAInicio = false;

            // Destruir el enemigo tocado si se ganó la batalla.
            if (!string.IsNullOrEmpty(BattleState.PendingDestroyEnemyName))
            {
                var root = GameObject.Find(enemiesRootName);
                if (root != null)
                {
                    var t = root.transform.Find(BattleState.PendingDestroyEnemyName);
                    if (t != null) Destroy(t.gameObject);
                }
                BattleState.PendingDestroyEnemyName = null;
            }
        }
    }
}
