#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DreamHacks.EditorTools
{
    /// <summary>
    /// Fija la escena de arranque de Play Mode (EditorSceneManager.playModeStartScene).
    /// La ruta se guarda en EditorPrefs y se puede cambiar desde el men├║:
    ///   DreamHacks > Start Scene > ...
    /// Si est├í vac├¡a, Unity usa la escena actualmente abierta (comportamiento por defecto).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        private const string PrefKey = "DreamHacks.PlayModeStartScenePath";
        private const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";

        static PlayModeStart()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            var path = EditorPrefs.GetString(PrefKey, MainMenuPath);
            if (string.IsNullOrEmpty(path) || path == "-")
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }
            var start = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorSceneManager.playModeStartScene = start;
        }

        private static void Set(string path)
        {
            EditorPrefs.SetString(PrefKey, path ?? "");
            Apply();
            UnityEngine.Debug.Log("[PlayModeStart] Arranque de Play Mode = " + (string.IsNullOrEmpty(path) || path == "-" ? "(escena activa)" : path));
        }

                [MenuItem("DreamHacks/Start Scene/MainMenu (default)")]
        private static void SetMenu() { Set(MainMenuPath); }
        [MenuItem("DreamHacks/Start Scene/MainMenu (default)", true)]
        private static bool ValMenu() { Menu.SetChecked("DreamHacks/Start Scene/MainMenu (default)", EditorPrefs.GetString(PrefKey, MainMenuPath) == MainMenuPath); return true; }

        [MenuItem("DreamHacks/Start Scene/Escena activa")]
        private static void SetNone() { Set("-"); }
        [MenuItem("DreamHacks/Start Scene/Escena activa", true)]
        private static bool ValNone() { Menu.SetChecked("DreamHacks/Start Scene/Escena activa", EditorPrefs.GetString(PrefKey, MainMenuPath) == "-"); return true; }

        [MenuItem("DreamHacks/Start Scene/ComicIntro")]
        private static void SetComic() { Set("Assets/_Project/Scenes/ComicIntro.unity"); }
        [MenuItem("DreamHacks/Start Scene/ComicIntro", true)]
        private static bool ValComic() { Menu.SetChecked("DreamHacks/Start Scene/ComicIntro", EditorPrefs.GetString(PrefKey, MainMenuPath) == "Assets/_Project/Scenes/ComicIntro.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Hospital")]
        private static void SetHospital() { Set("Assets/MetroidvaniaBase/Scenes/Hospital.unity"); }
        [MenuItem("DreamHacks/Start Scene/Hospital", true)]
        private static bool ValHospital() { Menu.SetChecked("DreamHacks/Start Scene/Hospital", EditorPrefs.GetString(PrefKey, MainMenuPath) == "Assets/MetroidvaniaBase/Scenes/Hospital.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Sandbox_Gino")]
        private static void SetSandbox() { Set("Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity"); }
        [MenuItem("DreamHacks/Start Scene/Sandbox_Gino", true)]
        private static bool ValSandbox() { Menu.SetChecked("DreamHacks/Start Scene/Sandbox_Gino", EditorPrefs.GetString(PrefKey, MainMenuPath) == "Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Bioma1")]
        private static void SetBioma1() { Set("Assets/_Project/Scenes/Bioma1.unity"); }
        [MenuItem("DreamHacks/Start Scene/Bioma1", true)]
        private static bool ValBioma1() { Menu.SetChecked("DreamHacks/Start Scene/Bioma1", EditorPrefs.GetString(PrefKey, MainMenuPath) == "Assets/_Project/Scenes/Bioma1.unity"); return true; }
    }
}
#endif
