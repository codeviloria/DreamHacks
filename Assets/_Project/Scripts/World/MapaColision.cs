using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace DreamHacks.World
{
    /// <summary>
    /// Lee un JSON con polígonos de colisión y genera un EdgeCollider2D por polígono.
    /// Las coordenadas del JSON están en píxeles con origen arriba-izquierda y Y hacia abajo;
    /// aquí se traducen a mundo centrado en el pivote del sprite (que debe ser Center).
    /// Formato esperado: {"width":W,"height":H,"paths":[{"hole":false,"points":[[x,y],...]}]}
    /// Uso: el componente va en el mismo GameObject del SpriteRenderer del mapa (pivot Center).
    /// Se puede regenerar con ContextMenu -> Generar colisiones.
    /// </summary>
    public class MapaColision : MonoBehaviour
    {
        [SerializeField] private TextAsset poligonosJson;
        [Tooltip("Pixels Per Unit del sprite del mapa.")]
        [SerializeField] private float pixelsPerUnit = 16f;

        [System.Serializable]
        private class Path { public bool hole; public int[][] points; }

        [ContextMenu("Generar colisiones")]
        public void Generar()
        {
            foreach (var c in GetComponents<EdgeCollider2D>()) DestroyImmediate(c);
            if (poligonosJson == null) { Debug.LogWarning("[MapaColision] JSON no asignado"); return; }

            int w, h;
            var root = MiniJSON(poligonosJson.text, out w, out h);
            int count = 0;
            foreach (var poly in root)
            {
                var pts = new List<Vector2>();
                foreach (var p in poly)
                    pts.Add(new Vector2((p.x - w / 2f) / pixelsPerUnit, (h / 2f - p.y) / pixelsPerUnit));
                if (pts.Count == 0) continue;
                pts.Add(pts[0]); // cerrar polígono
                var edge = gameObject.AddComponent<EdgeCollider2D>();
                edge.points = pts.ToArray();
                edge.edgeRadius = 0.02f;
                count++;
            }
            Debug.Log($"[MapaColision] {count} EdgeCollider2D generados.");
        }

        private void Start()
        {
            // Al iniciar en runtime, también regenera por si fue instanciado sin tenerlos.
            if (GetComponent<EdgeCollider2D>() == null) Generar();
        }

        private static List<List<Vector2>> MiniJSON(string s, out int w, out int h)
        {
            // Usamos Newtonsoft (ya en el proyecto) para evitar locale/parse issues.
            var root = JObject.Parse(s);
            w = (int)root["width"];
            h = (int)root["height"];
            var result = new List<List<Vector2>>();
            var paths = (JArray)root["paths"];
            foreach (var path in paths)
            {
                var pts = (JArray)path["points"];
                var poly = new List<Vector2>(pts.Count);
                foreach (var p in pts)
                {
                    var arr = (JArray)p;
                    poly.Add(new Vector2((float)arr[0], (float)arr[1]));
                }
                result.Add(poly);
            }
            return result;
        }
    }
}
