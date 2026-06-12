using IdleOnLike.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleOnLike.Core
{
    public sealed class SceneLoaderService
    {
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Cannot load an empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public void LoadZone(ZoneDefinition zone)
        {
            if (zone == null)
            {
                Debug.LogError("Cannot load a null zone.");
                return;
            }

            LoadScene(zone.SceneName);
        }
    }
}
