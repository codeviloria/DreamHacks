using UnityEngine;
using DreamHacks.Combate;

namespace DreamHacks.UI
{
    // En Start, busca al Player por tag y conecta su VidaJugador al HUDCorazones
    // de la misma escena (HUDCanvas/BarraCorazones). Si no existe, no hace nada.
    public class HUDWiring : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private string hudPath = "HUDCanvas/BarraCorazones";

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;
            var vida = player.GetComponent<VidaJugador>();
            if (vida == null) vida = player.GetComponentInChildren<VidaJugador>();
            if (vida == null) return;

            var hudGO = GameObject.Find(hudPath);
            if (hudGO == null) return;
            var hud = hudGO.GetComponent<HUDCorazones>();
            if (hud == null) return;
            hud.SetVida(vida);
        }
    }
}
