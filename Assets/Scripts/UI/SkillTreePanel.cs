using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Progression;
using UnityEngine;

namespace IdleOnLike.UI
{
    public sealed class SkillTreePanel
    {
        private readonly GameRuntime runtime;
        private readonly SkillTreeService skillTreeService;
        private readonly RectTransform root;
        private readonly RectTransform listRoot;
        private readonly UnityEngine.UI.Text pointsText;
        private bool isDisposed;

        public SkillTreePanel(GameRuntime runtime, Transform parent)
        {
            this.runtime = runtime;
            skillTreeService = runtime.SkillTreeService;
            root = RuntimeUiFactory.CreatePanel(parent, "Skill Tree Panel", new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.84f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.08f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Skills", 28, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.65f, 0.98f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.91f), new Vector2(0.96f, 0.975f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(Toggle);

            var pointsBar = RuntimeUiFactory.CreatePanel(root, "Skill Points Bar", new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero, new Color(0.14f, 0.17f, 0.22f, 1f));
            pointsText = RuntimeUiFactory.CreateText(pointsBar, "Skill Points Text", string.Empty, 19, TextAnchor.MiddleLeft, new Color(1f, 0.94f, 0.62f));
            RuntimeUiFactory.Stretch(pointsText.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));

            listRoot = RuntimeUiFactory.CreateScrollContent(root, "Skill Node List", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.74f), new Color(0.11f, 0.12f, 0.14f, 1f));
            skillTreeService.Changed += Refresh;
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
            var chopping = skillTreeService.GetProgress(SkillType.Chopping);
            var mining = skillTreeService.GetProgress(SkillType.Mining);
            var choppingRequired = ProgressionService.GetSkillExperienceRequired(chopping.level);
            var miningRequired = ProgressionService.GetSkillExperienceRequired(mining.level);
            pointsText.text = $"Chopping Skill Points: {chopping.points}    Lv.{chopping.level} XP {chopping.experience}/{choppingRequired}\nMining Skill Points: {mining.points}    Lv.{mining.level} XP {mining.experience}/{miningRequired}";

            for (var i = 0; i < runtime.Catalog.SkillNodes.Count; i++)
            {
                BuildRow(runtime.Catalog.SkillNodes[i], i);
            }

            RuntimeUiFactory.SetScrollContentHeight(listRoot, 20f + runtime.Catalog.SkillNodes.Count * 82f);
        }

        private void BuildRow(SkillNodeDefinition node, int index)
        {
            if (node == null)
            {
                return;
            }

            var row = RuntimeUiFactory.CreatePanel(listRoot, $"Skill Node {node.Id}", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -72f - index * 82f), new Vector2(-14f, -12f - index * 82f), new Color(0.17f, 0.18f, 0.21f, 1f));
            var rank = skillTreeService.GetRank(node.Id);
            var label = RuntimeUiFactory.CreateText(row, "Label", $"{node.DisplayName}  {rank}/{node.MaxRank}  [{node.SkillType}]\n{node.Description}\n{node.EffectType}: +{node.ValuePerRank:0.##} per rank", 16, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero);

            var button = RuntimeUiFactory.CreateButton(row, "Rank Button", "+", new Color(0.22f, 0.42f, 0.72f, 1f));
            RuntimeUiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.78f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
            button.interactable = skillTreeService.CanRankUp(node);
            button.onClick.AddListener(() =>
            {
                if (skillTreeService.RankUp(node.Id))
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
            if (skillTreeService != null)
            {
                skillTreeService.Changed -= Refresh;
            }
        }
    }
}
