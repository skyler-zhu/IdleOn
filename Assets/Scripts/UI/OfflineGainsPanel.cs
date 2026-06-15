using IdleOnLike.Progression;
using UnityEngine;

namespace IdleOnLike.UI
{
    public static class OfflineGainsPanel
    {
        public static void Show(OfflineGainsResult result)
        {
            if (result == null)
            {
                return;
            }

            var canvas = RuntimeUiFactory.CreateCanvas("Offline Gains UI");
            var root = RuntimeUiFactory.CreatePanel(canvas.transform, "Offline Gains Panel", new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.78f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.09f, 0.97f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Offline Gains", 30, TextAnchor.MiddleCenter, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            var body = RuntimeUiFactory.CreateText(root, "Body", BuildText(result), 18, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.96f));
            RuntimeUiFactory.SetRect(body.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.25f, 0.36f, 0.55f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.15f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(() => Object.Destroy(canvas.gameObject));
            RuntimeUiOverlayRegistry.Register(root);
            RuntimeUiOverlayRegistry.Show(root);
        }

        private static string BuildText(OfflineGainsResult result)
        {
            var text = $"Time: {result.elapsed.TotalMinutes:0} minutes\n";
            text += $"Activity: {result.activity}\n";
            text += $"XP: +{result.experience}\n";
            text += $"Coins: +{result.coins}\n";
            text += $"Levels: +{result.levelsGained}\n\nItems:";

            if (result.items.Count == 0)
            {
                text += "\n- None";
            }
            else
            {
                foreach (var item in result.items)
                {
                    text += $"\n- {item.displayName} x{item.quantity}";
                }
            }

            text += "\n\nProgress:";
            if (result.questProgress.Count == 0)
            {
                text += "\n- None";
            }
            else
            {
                foreach (var progress in result.questProgress)
                {
                    text += $"\n- {progress}";
                }
            }

            return text;
        }
    }
}
