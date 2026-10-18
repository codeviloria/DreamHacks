#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DreamHacks.EditorTools
{
    /// <summary>
    /// AssetPostprocessor que configura automáticamente los PNG de mapas como
    /// sprites pixel-art: Point, sin compresión, PPU 16, pivote centro, FullRect,
    /// alpha transparente. Se dispara para cualquier PNG cuyo nombre empiece
    /// por "lab_", "mapa_" o "colision_".
    /// </summary>
    public class MapaImporter : AssetPostprocessor
    {
        const float PPU = 16f;

        void OnPreprocessTexture()
        {
            string file = System.IO.Path.GetFileName(assetPath).ToLower();
            if (!(file.StartsWith("lab_") || file.StartsWith("mapa_") || file.StartsWith("colision_")
                 || file.StartsWith("transicion_") || file.StartsWith("comic_") || file.StartsWith("hacker_")
                 || file.StartsWith("hannah ") || file.StartsWith("hannah_") || file.StartsWith("iker ") || file.StartsWith("iker_")
                 || file.StartsWith("corazon") || file.StartsWith("goblin") || file.StartsWith("skel") || file.StartsWith("potion"))) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            // Respeta Multiple si ya fue sliceado como spritesheet.
            if (ti.spriteImportMode != SpriteImportMode.Multiple)
                ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = UnityEngine.FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 4096;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
#endif
