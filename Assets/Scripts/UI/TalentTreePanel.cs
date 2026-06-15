using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Progression;
using UnityEngine;

namespace IdleOnLike.UI
{
    public sealed class TalentTreePanel
    {
        private readonly GameRuntime runtime;
        private readonly TalentService talentService;
        private readonly RectTransform root;
        private readonly RectTransform listRoot;
        private readonly UnityEngine.UI.Text pointsText;
        private bool isDisposed;

        public TalentTreePanel(GameRuntime runtime, Transform parent)
        {
            this.runtime = runtime;
            talentService = runtime.TalentService;
            root = RuntimeUiFactory.CreatePanel(parent, "Talent Tree Panel", new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.84f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.08f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Talents", 28, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.65f, 0.98f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.91f), new Vector2(0.96f, 0.975f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(Toggle);

            var pointsBar = RuntimeUiFactory.CreatePanel(root, "Talent Points Bar", new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero, new Color(0.14f, 0.17f, 0.22f, 1f));
            pointsText = RuntimeUiFactory.CreateText(pointsBar, "Talent Points Text", string.Empty, 22, TextAnchor.MiddleLeft, new Color(1f, 0.94f, 0.62f));
            RuntimeUiFactory.Stretch(pointsText.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));

            listRoot = RuntimeUiFactory.CreateScrollContent(root, "Talent List", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.78f), new Color(0.11f, 0.12f, 0.14f, 1f));
            talentService.Changed += Refresh;
            parent.GetComponentInParent<RuntimeUiLifetime>()?.Register(Dispose);
            root.gameObject.SetActive(false);
            RuntimeUiOverlayRegistry.Register(root, Refresh);
            Refresh();
        }

        public void Toggle()
        {
            RuntimeUiOverlayRegistry.Toggle(root, Refresh);
        }

        private void Refresh()
        {
            if (isDisposed || runtime == null || runtime.State == null || listRoot == null || pointsText == null)
            {
                Dispose();
                return;
            }

            Clear(listRoot);
            pointsText.text = $"Available Talent Points: {talentService.TalentPoints}";

            for (var i = 0; i < runtime.Catalog.Talents.Count; i++)
            {
                BuildRow(runtime.Catalog.Talents[i], i);
            }

            RuntimeUiFactory.SetScrollContentHeight(listRoot, 20f + runtime.Catalog.Talents.Count * 82f);
        }

        private void BuildRow(TalentDefinition talent, int index)
        {
            if (talent == null)
            {
                return;
            }

            var row = RuntimeUiFactory.CreatePanel(listRoot, $"Talent {talent.Id}", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -72f - index * 82f), new Vector2(-14f, -12f - index * 82f), new Color(0.17f, 0.18f, 0.21f, 1f));
            var rank = talentService.GetRank(talent.Id);
            var label = RuntimeUiFactory.CreateText(row, "Label", $"{talent.DisplayName}  {rank}/{talent.MaxRank}\n{talent.Description}\n{talent.StatType}: +{talent.ValuePerRank} per rank", 16, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero);

            var button = RuntimeUiFactory.CreateButton(row, "Rank Button", "+", new Color(0.22f, 0.42f, 0.72f, 1f));
            RuntimeUiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.78f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
            button.interactable = talentService.CanRankUp(talent);
            button.onClick.AddListener(() =>
            {
                if (talentService.RankUp(talent.Id))
                {
                    runtime.Save();
                    Refresh();
                }
            });
        }

        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (talentService != null)
            {
                talentService.Changed -= Refresh;
            }
        }
    }
}
