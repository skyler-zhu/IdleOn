using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.UI
{
    public sealed class RuntimeUiLifetime : MonoBehaviour
    {
        private readonly List<Action> cleanupActions = new List<Action>();

        public void Register(Action cleanup)
        {
            if (cleanup != null)
            {
                cleanupActions.Add(cleanup);
            }
        }

        private void OnDestroy()
        {
            for (var i = cleanupActions.Count - 1; i >= 0; i--)
            {
                cleanupActions[i]?.Invoke();
            }

            cleanupActions.Clear();
        }
    }
}
