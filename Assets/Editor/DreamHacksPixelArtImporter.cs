#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamHacks.EditorTools
{
    /// <summary>
    /// Normaliza los PNG importados dentro de Assets/_DreamHacks/Art con los
    /// ajustes de pixel art del proyecto: Sprite, PPU 16, Point filter, sin
    /// compresión, sin mipmaps, Alpha Is Transparency on. Se aplica
    /// automáticamente en cada import; también hay un menú para reaplicar en
    /// bloque cuando cambian las reglas o entra arte viejo.
    /// </summary>
    public class DreamHacksPixelArtImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_DreamHacks/Art";
        private const float PixelsPerUnit = 16f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            if (!assetPath.EndsWith(".png")) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
        }

        [MenuItem("DreamHacks/Reaplicar ajustes pixel art")]
        private static void ReapplyAll()
        {
            if (!AssetDatabase.IsValidFolder(ArtRoot))
            {
                Debug.LogWarning($"No existe la carpeta {ArtRoot}; nada que reimportar.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            int count = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (!path.EndsWith(".png")) continue;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"[DreamHacks] Reaplicados ajustes pixel art a {count} PNG bajo {ArtRoot}.");
        }
    }
}
#endif
