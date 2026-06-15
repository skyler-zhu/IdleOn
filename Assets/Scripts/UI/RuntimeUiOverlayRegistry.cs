using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.UI
{
    public static class RuntimeUiOverlayRegistry
    {
        private sealed class Entry
        {
            public RectTransform Root;
            public Action OnShow;
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static Entry questDetails;

        public static void Register(RectTransform root, Action onShow = null)
        {
            if (root == null || entries.Exists(entry => entry.Root == root))
            {
                return;
            }

            entries.Add(new Entry { Root = root, OnShow = onShow });
        }

        public static void RegisterQuestDetails(RectTransform root, Action onShow = null)
        {
            Register(root, onShow);
            questDetails = Find(root);
        }

        public static void Toggle(RectTransform root, Action onShow = null)
        {
            if (root == null)
            {
                return;
            }

            Register(root, onShow);
            if (root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(false);
                return;
            }

            Show(root, onShow);
        }

        public static void Show(RectTransform root, Action onShow = null)
        {
            if (root == null)
            {
                return;
            }

            Register(root, onShow);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            onShow?.Invoke();
        }

        public static bool CloseTop()
        {
            Cleanup();
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var root = entries[i].Root;
                if (root != null && root.gameObject.activeSelf)
                {
                    root.gameObject.SetActive(false);
                    return true;
                }
            }

            return false;
        }

        public static void ToggleQuestDetails()
        {
            Cleanup();
            if (questDetails?.Root == null)
            {
                return;
            }

            Toggle(questDetails.Root, questDetails.OnShow);
        }

        private static Entry Find(RectTransform root)
        {
            return entries.Find(entry => entry.Root == root);
        }

        private static void Cleanup()
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Root == null)
                {
                    entries.RemoveAt(i);
                }
            }

            if (questDetails?.Root == null)
            {
                questDetails = null;
            }
        }
    }
}
