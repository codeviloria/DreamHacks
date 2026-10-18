#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DreamHacks.EditorTools
{
    [InitializeOnLoad]
    public static class FinalBossImporterHelper
    {
        static FinalBossImporterHelper()
        {
            EditorApplication.delayCall += EnsureImported;
        }

        [MenuItem("DreamHacks/Reimport Final Boss Sprites")]
        public static void EnsureImported()
        {
            string path = "Assets/_Project/Prefabs/FINAL_BOSS_JORGE_EL_CURIOSO.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log("[DreamHacks] FINAL_BOSS_JORGE_EL_CURIOSO importado y rebanado correctamente.");
            }
        }
    }
}
#endif
