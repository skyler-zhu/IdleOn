using UnityEditor;
using UnityEditor.SceneManagement;

namespace IdleOnLike.EditorTools
{
    [InitializeOnLoad]
    public static class PlayModeStartSceneSetter
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";

        static PlayModeStartSceneSetter()
        {
            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene != null)
            {
                EditorSceneManager.playModeStartScene = bootScene;
            }
        }
    }
}
