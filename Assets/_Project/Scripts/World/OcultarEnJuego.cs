using UnityEngine;
using UnityEngine.Tilemaps;

namespace DreamHacks.World
{
    /// <summary>
    /// Deja visible el tilemap de colisiones en el editor para poder retocarlo
    /// con el Tile Palette, pero oculta su renderer al entrar en Play.
    /// Pensado para los bloques rojos del BiomeCollisionBuilder.
    /// </summary>
    public class OcultarEnJuego : MonoBehaviour
    {
        private void Awake()
        {
            var r = GetComponent<TilemapRenderer>();
            if (r != null) r.enabled = false;
        }
    }
}
