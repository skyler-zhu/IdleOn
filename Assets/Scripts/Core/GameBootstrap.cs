using IdleOnLike.Data;
using UnityEngine;

namespace IdleOnLike.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;

        private void Awake()
        {
            if (catalog == null)
            {
                Debug.LogError("GameBootstrap is missing a GameCatalog reference.");
                return;
            }

            var runtimeObject = new GameObject("Game Runtime");
            var runtime = runtimeObject.AddComponent<GameRuntime>();
            runtime.Initialize(catalog);
        }
    }
}
