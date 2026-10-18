#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DreamHacks.EditorTools
{
    /// <summary>
    /// Fija la escena de arranque de Play Mode (EditorSceneManager.playModeStartScene).
    /// Por defecto inicia en MainMenu (Assets/_Project/Scenes/MainMenu.unity).
    /// La ruta se guarda en EditorPrefs y se puede cambiar desde el menú:
    ///   DreamHacks > Start Scene > ...
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        private const string PrefKey = "DreamHacks.PlayModeStartScenePath";
        private const string DefaultStartScene = "Assets/_Project/Scenes/MainMenu.unity";

        static PlayModeStart()
        {
            EditorApplication.delayCall += Apply;
        }

        public static void Apply()
        {
            var path = EditorPrefs.GetString(PrefKey, DefaultStartScene);
            if (string.IsNullOrEmpty(path))
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }
            var start = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (start != null)
            {
                EditorSceneManager.playModeStartScene = start;
            }
        }

        private static void Set(string path)
        {
            EditorPrefs.SetString(PrefKey, path ?? "");
            Apply();
            UnityEngine.Debug.Log("[PlayModeStart] Arranque de Play Mode = " + (string.IsNullOrEmpty(path) ? "(escena activa)" : path));
        }

        [MenuItem("DreamHacks/Start Scene/MainMenu (Principal)")]
        private static void SetMainMenu() { Set("Assets/_Project/Scenes/MainMenu.unity"); }
        [MenuItem("DreamHacks/Start Scene/MainMenu (Principal)", true)]
        private static bool ValMainMenu() { Menu.SetChecked("DreamHacks/Start Scene/MainMenu (Principal)", EditorPrefs.GetString(PrefKey, DefaultStartScene) == "Assets/_Project/Scenes/MainMenu.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Escena activa")]
        private static void SetNone() { Set(""); }
        [MenuItem("DreamHacks/Start Scene/Escena activa", true)]
        private static bool ValNone() { Menu.SetChecked("DreamHacks/Start Scene/Escena activa", EditorPrefs.GetString(PrefKey, DefaultStartScene) == ""); return true; }

        [MenuItem("DreamHacks/Start Scene/ComicIntro")]
        private static void SetComic() { Set("Assets/_Project/Scenes/ComicIntro.unity"); }
        [MenuItem("DreamHacks/Start Scene/ComicIntro", true)]
        private static bool ValComic() { Menu.SetChecked("DreamHacks/Start Scene/ComicIntro", EditorPrefs.GetString(PrefKey, DefaultStartScene) == "Assets/_Project/Scenes/ComicIntro.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Hospital")]
        private static void SetHospital() { Set("Assets/MetroidvaniaBase/Scenes/Hospital.unity"); }
        [MenuItem("DreamHacks/Start Scene/Hospital", true)]
        private static bool ValHospital() { Menu.SetChecked("DreamHacks/Start Scene/Hospital", EditorPrefs.GetString(PrefKey, DefaultStartScene) == "Assets/MetroidvaniaBase/Scenes/Hospital.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Sandbox_Gino")]
        private static void SetSandbox() { Set("Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity"); }
        [MenuItem("DreamHacks/Start Scene/Sandbox_Gino", true)]
        private static bool ValSandbox() { Menu.SetChecked("DreamHacks/Start Scene/Sandbox_Gino", EditorPrefs.GetString(PrefKey, DefaultStartScene) == "Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity"); return true; }

        [MenuItem("DreamHacks/Start Scene/Bioma1")]
        private static void SetBioma1() { Set("Assets/_Project/Scenes/Bioma1.unity"); }
        [MenuItem("DreamHacks/Start Scene/Bioma1", true)]
        private static bool ValBioma1() { Menu.SetChecked("DreamHacks/Start Scene/Bioma1", EditorPrefs.GetString(PrefKey, DefaultStartScene) == "Assets/_Project/Scenes/Bioma1.unity"); return true; }
    }
}
#endif

