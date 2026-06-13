using IdleOnLike.Core;
using IdleOnLike.Data;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnLike.UI
{
    public sealed class WorldMapPanel
    {
        private readonly GameRuntime runtime;
        private readonly Canvas canvas;
        private readonly RectTransform root;
        private readonly RectTransform pointLayer;
        private Text titleText;

        public WorldMapPanel(GameRuntime runtime)
        {
            this.runtime = runtime;
            canvas = RuntimeUiFactory.CreateCanvas("World Map");
            Object.DontDestroyOnLoad(canvas.gameObject);

            root = RuntimeUiFactory.CreatePanel(canvas.transform, "Map Root", new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.96f));
            titleText = RuntimeUiFactory.CreateText(root, "Title", "World Map", 32, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(titleText.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.70f, 0.98f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.90f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(Toggle);

            var backdrop = RuntimeUiFactory.CreatePanel(root, "Map Backdrop", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero, new Color(0.10f, 0.12f, 0.16f, 1f));
            var hint = RuntimeUiFactory.CreateText(backdrop, "Hint", "Click a location to travel", 18, TextAnchor.LowerLeft, new Color(0.72f, 0.78f, 0.86f));
            RuntimeUiFactory.SetRect(hint.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.60f, 0.10f), Vector2.zero, Vector2.zero);

            pointLayer = RuntimeUiFactory.CreatePanel(backdrop, "Point Layer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            root.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (root == null)
            {
                return;
            }

            var show = !root.gameObject.activeSelf;
            root.gameObject.SetActive(show);
            if (show)
            {
                Refresh();
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void Refresh()
        {
            Clear(pointLayer);
            titleText.text = runtime.State != null && runtime.State.CurrentZone != null
                ? $"World Map    Current: {runtime.State.CurrentZone.DisplayName}"
                : "World Map";

            var count = runtime.Catalog.Zones.Count;
            for (var i = 0; i < count; i++)
            {
                var zone = runtime.Catalog.Zones[i];
                if (zone == null || string.IsNullOrEmpty(zone.SceneName))
                {
                    continue;
                }

                CreateZonePoint(zone, i, count);
            }
        }

        private void CreateZonePoint(ZoneDefinition zone, int index, int count)
        {
            var position = GetMapPoint(index, count);
            var unlocked = IsZoneUnlocked(zone);
            var current = runtime.State != null && runtime.State.CurrentZone == zone;
            var color = current
                ? new Color(0.22f, 0.56f, 0.88f, 1f)
                : unlocked ? new Color(0.20f, 0.42f, 0.28f, 1f) : new Color(0.22f, 0.23f, 0.27f, 1f);

            var button = RuntimeUiFactory.CreateButton(pointLayer, $"Map Point {zone.Id}", unlocked ? zone.DisplayName : $"{zone.DisplayName}\nLocked", color);
            RuntimeUiFactory.SetRect(button.GetComponent<RectTransform>(), position - new Vector2(0.085f, 0.055f), position + new Vector2(0.085f, 0.055f), Vector2.zero, Vector2.zero);
            button.interactable = unlocked && !current;
            button.onClick.AddListener(() =>
            {
                Hide();
                runtime.TravelToZone(zone);
            });

            var label = RuntimeUiFactory.CreateText(pointLayer, $"Map Label {zone.Id}", GetUnlockText(zone, unlocked, current), 16, TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.95f));
            RuntimeUiFactory.SetRect(label.rectTransform, position + new Vector2(-0.13f, -0.115f), position + new Vector2(0.13f, -0.065f), Vector2.zero, Vector2.zero);
        }

        private bool IsZoneUnlocked(ZoneDefinition zone)
        {
            if (runtime.State == null)
            {
                return false;
            }

            if (zone.RequiredCharacterLevel > 0 && runtime.State.SaveData.level < zone.RequiredCharacterLevel)
            {
                return false;
            }

            return zone.RequiredQuest == null || runtime.QuestService.IsQuestCompleted(zone.RequiredQuest.Id);
        }

        private string GetUnlockText(ZoneDefinition zone, bool unlocked, bool current)
        {
            if (current)
            {
                return "Current";
            }

            if (unlocked)
            {
                return "Travel";
            }

            if (zone.RequiredQuest != null)
            {
                return $"Requires {zone.RequiredQuest.Title}";
            }

            if (zone.RequiredCharacterLevel > 0)
            {
                return $"Lv. {zone.RequiredCharacterLevel}";
            }

            return "Locked";
        }

        private static Vector2 GetMapPoint(int index, int count)
        {
            if (count <= 1)
            {
                return new Vector2(0.50f, 0.48f);
            }

            var t = count <= 1 ? 0f : index / (float)(count - 1);
            var x = Mathf.Lerp(0.18f, 0.82f, t);
            var y = 0.52f + Mathf.Sin(t * Mathf.PI * 1.35f) * 0.20f;
            return new Vector2(x, y);
        }

        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
