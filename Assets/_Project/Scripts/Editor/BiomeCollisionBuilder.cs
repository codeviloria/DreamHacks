// Menú: DreamHacks > Generar Colisiones de Bioma
// Lee el arte del bioma (o una máscara B/N) y llena un Tilemap invisible
// con bloques en todo lo que NO es sendero. El jugador solo podrá caminar por el path.
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using DreamHacks.World;

namespace DreamHacks.EditorTools
{
    public class BiomeCollisionBuilder : EditorWindow
    {
        SpriteRenderer fondo;          // el arte del bioma en la escena
        Texture2D mascara;             // opcional: blanco = caminable, negro = bloqueado
        Color colorSendero = new Color(0.55f, 0.36f, 0.55f);
        float tolerancia = 0.15f;
        int pxPorCelda = 8;
        float porcentajeCaminable = 0.5f;

        const string Carpeta = "Assets/DreamHacks_Colisiones";

        [MenuItem("DreamHacks/Generar Colisiones de Bioma")]
        static void Abrir() => GetWindow<BiomeCollisionBuilder>("Colisiones Bioma");

        void OnGUI()
        {
            EditorGUILayout.HelpBox("1) Arrastra el fondo del bioma.\n2) Usa una máscara B/N (exacto) o elige el color del sendero con el gotero.\n3) Generar. Luego retoca pintando con el Tile Palette.", MessageType.Info);
            fondo = (SpriteRenderer)EditorGUILayout.ObjectField("Fondo del bioma", fondo, typeof(SpriteRenderer), true);
            mascara = (Texture2D)EditorGUILayout.ObjectField("Máscara B/N (opcional)", mascara, typeof(Texture2D), false);
            if (mascara == null)
            {
                colorSendero = EditorGUILayout.ColorField("Color del sendero", colorSendero);
                tolerancia = EditorGUILayout.Slider("Tolerancia de color", tolerancia, 0.02f, 0.5f);
            }
            pxPorCelda = Mathf.Max(1, EditorGUILayout.IntField("Píxeles por celda", pxPorCelda));
            porcentajeCaminable = EditorGUILayout.Slider("% de sendero para caminar", porcentajeCaminable, 0.1f, 1f);

            GUI.enabled = fondo != null && fondo.sprite != null;
            if (GUILayout.Button("Generar colisiones", GUILayout.Height(32))) Generar();
            GUI.enabled = true;
        }

        void Generar()
        {
            Sprite spr = fondo.sprite;
            Texture2D tex = mascara != null ? mascara : spr.texture;
            HacerLegible(tex);

            Rect r = spr.rect;
            int w = (int)r.width, h = (int)r.height;
            Color[] px;
            int tw, th;
            if (mascara != null) { px = mascara.GetPixels(); tw = mascara.width; th = mascara.height; }
            else { px = tex.GetPixels((int)r.x, (int)r.y, w, h); tw = w; th = h; }

            float ppu = spr.pixelsPerUnit;
            float celda = pxPorCelda / ppu;

            Tilemap tm = ObtenerTilemap(celda);
            Undo.RegisterCompleteObjectUndo(tm, "Generar colisiones");
            tm.ClearAllTiles();
            Tile bloqueo = ObtenerTileBloqueo(celda);

            int cols = Mathf.CeilToInt(w / (float)pxPorCelda);
            int filas = Mathf.CeilToInt(h / (float)pxPorCelda);
            int bloqueadas = 0;

            for (int cy = 0; cy < filas; cy++)
            for (int cx = 0; cx < cols; cx++)
            {
                int caminables = 0, total = 0;
                for (int y = cy * pxPorCelda; y < Mathf.Min(h, (cy + 1) * pxPorCelda); y++)
                for (int x = cx * pxPorCelda; x < Mathf.Min(w, (cx + 1) * pxPorCelda); x++)
                {
                    int mx = x * tw / w, my = y * th / h;   // permite máscara de otro tamaño
                    Color c = px[my * tw + mx];
                    total++;
                    if (EsCaminable(c)) caminables++;
                }
                if (total > 0 && caminables / (float)total < porcentajeCaminable)
                {
                    tm.SetTile(new Vector3Int(cx, cy, 0), bloqueo);
                    bloqueadas++;
                }
            }

            // Borde exterior para que nadie salga del mapa
            for (int x = -1; x <= cols; x++) { tm.SetTile(new Vector3Int(x, -1, 0), bloqueo); tm.SetTile(new Vector3Int(x, filas, 0), bloqueo); }
            for (int y = 0; y < filas; y++) { tm.SetTile(new Vector3Int(-1, y, 0), bloqueo); tm.SetTile(new Vector3Int(cols, y, 0), bloqueo); }

            EditorSceneManager.MarkSceneDirty(tm.gameObject.scene);
            Debug.Log($"[Colisiones] {bloqueadas} de {cols * filas} celdas bloqueadas. Revisa en la Scene view y retoca con el Tile Palette.");
        }

        bool EsCaminable(Color c)
        {
            if (c.a < 0.5f) return false;                       // transparente = fuera del mapa
            if (mascara != null) return c.grayscale > 0.5f;     // blanco = caminable
            float d = Mathf.Sqrt((c.r - colorSendero.r) * (c.r - colorSendero.r) +
                                 (c.g - colorSendero.g) * (c.g - colorSendero.g) +
                                 (c.b - colorSendero.b) * (c.b - colorSendero.b));
            return d <= tolerancia;
        }

        Tilemap ObtenerTilemap(float celda)
        {
            GameObject gridGO = GameObject.Find("Grid_Colisiones");
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid_Colisiones");
                gridGO.AddComponent<Grid>();
                Undo.RegisterCreatedObjectUndo(gridGO, "Crear Grid colisiones");
            }
            gridGO.GetComponent<Grid>().cellSize = new Vector3(celda, celda, 0);
            Vector3 origen = fondo.bounds.min;                  // esquina inferior izquierda del arte
            gridGO.transform.position = new Vector3(origen.x, origen.y, 0);

            Transform t = gridGO.transform.Find("Colisiones");
            GameObject tmGO;
            if (t == null)
            {
                tmGO = new GameObject("Colisiones");
                tmGO.transform.SetParent(gridGO.transform, false);
                tmGO.AddComponent<Tilemap>();
                var rend = tmGO.AddComponent<TilemapRenderer>();
                rend.sortingOrder = 100;
                tmGO.AddComponent<TilemapCollider2D>();
                tmGO.AddComponent<OcultarEnJuego>();
            }
            else tmGO = t.gameObject;
            return tmGO.GetComponent<Tilemap>();
        }

        Tile ObtenerTileBloqueo(float celda)
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "DreamHacks_Colisiones");
            string pngPath = Carpeta + "/Bloqueo.png";
            string tilePath = Carpeta + "/Bloqueo.asset";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath) == null)
            {
                var t = new Texture2D(16, 16);
                var cols = new Color[256];
                for (int i = 0; i < cols.Length; i++) cols[i] = new Color(1f, 0f, 0f, 0.35f);
                t.SetPixels(cols); t.Apply();
                System.IO.File.WriteAllBytes(pngPath, t.EncodeToPNG());
                AssetDatabase.ImportAsset(pngPath);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            float ppuDeseado = 16f / celda;                     // el sprite rojo mide exactamente una celda
            if (imp.textureType != TextureImporterType.Sprite || !Mathf.Approximately(imp.spritePixelsPerUnit, ppuDeseado))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = ppuDeseado;
                imp.filterMode = FilterMode.Point;
                imp.SaveAndReimport();
            }

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        static void HacerLegible(Texture2D tex)
        {
            string p = AssetDatabase.GetAssetPath(tex);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && (!imp.isReadable || imp.textureCompression != TextureImporterCompression.Uncompressed))
            {
                imp.isReadable = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed; // colores exactos en pixel art
                imp.SaveAndReimport();
            }
        }
    }
}
#endif
