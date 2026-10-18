#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DreamHacks.EditorTools
{
    /// <summary>
    /// Fija MainMenu como escena de arranque de Play Mode cada vez que el
    /// editor carga. Así, al pulsar Play, siempre comienza en el Menú Principal
    /// antes de pasar al mapa top-down (Sandbox_Gino).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        private const string StartScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        static PlayModeStart()
        {
            EditorApplication.delayCall += Apply;
        }

        public static void Apply()
        {
            var start = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
            if (start != null)
            {
                EditorSceneManager.playModeStartScene = start;
            }
        }
    }
}
#endif
