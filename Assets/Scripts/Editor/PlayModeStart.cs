#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DreamHacks.EditorTools
{
    /// <summary>
    /// Fija Sandbox_Gino como escena de arranque de Play Mode cada vez que el
    /// editor carga. Así, al pulsar Play, siempre comienza en el mapa top-down
    /// aunque esté abierta Bioma1 (u otra). EditorSceneManager.playModeStartScene
    /// no se guarda entre sesiones por sí solo, por eso hace falta este hook.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        private const string StartScenePath = "Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity";

        static PlayModeStart()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            var start = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
            if (start != null) EditorSceneManager.playModeStartScene = start;
        }
    }
}
#endif
