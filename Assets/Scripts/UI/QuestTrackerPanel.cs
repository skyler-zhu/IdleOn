using IdleOnLike.Core;
using IdleOnLike.Data;
using UnityEngine;

namespace IdleOnLike.UI
{
    public sealed class QuestTrackerPanel
    {
        private readonly GameRuntime runtime;
        private readonly Quests.QuestService questService;
        private readonly Inventory.InventoryService inventoryService;
        private readonly RectTransform root;
        private readonly bool allowActions;
        private UnityEngine.UI.Text bodyText;
        private UnityEngine.UI.Button actionButton;
        private bool isDisposed;

        public QuestTrackerPanel(GameRuntime runtime, Transform parent, bool allowActions)
        {
            this.runtime = runtime;
            questService = runtime.QuestService;
            inventoryService = runtime.InventoryService;
            this.allowActions = allowActions;

            root = RuntimeUiFactory.CreatePanel(
                parent,
                "Quest Tracker",
                new Vector2(0.01f, 0.12f),
                new Vector2(0.22f, 0.58f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.08f, 0.10f, 0.92f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Quest", 19, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            bodyText = RuntimeUiFactory.CreateText(root, "Body", string.Empty, 14, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.96f));
            RuntimeUiFactory.SetRect(bodyText.rectTransform, new Vector2(0.08f, allowActions ? 0.22f : 0.06f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);

            if (allowActions)
            {
                actionButton = RuntimeUiFactory.CreateButton(root, "Action Button", "Accept", new Color(0.22f, 0.42f, 0.72f, 1f));
                RuntimeUiFactory.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.18f), Vector2.zero, Vector2.zero);
            }

            questService.Changed += Refresh;
            inventoryService.Changed += Refresh;

            var lifetime = parent.GetComponentInParent<RuntimeUiLifetime>();
            lifetime?.Register(Dispose);
            Refresh();
        }

        private void Refresh()
        {
            if (isDisposed || runtime == null || runtime.State == null || root == null || bodyText == null)
            {
                Dispose();
                return;
            }

            var actionQuest = questService.GetNextActionableQuest();
            bodyText.text = BuildQuestWindow();

            if (actionQuest == null)
            {
                SetAction(null, string.Empty, false);
                return;
            }

            if (!allowActions)
            {
                return;
            }

            if (questService.IsQuestCompleted(actionQuest.Id))
            {
                SetAction(null, "Done", false);
            }
            else if (!questService.IsQuestActive(actionQuest.Id))
            {
                SetAction(() => questService.AcceptQuest(actionQuest.Id), "Accept", true);
            }
            else
            {
                var canComplete = questService.CanComplete(actionQuest.Id);
                SetAction(() => questService.CompleteQuest(actionQuest.Id), "Complete", canComplete);
            }
        }

        private string BuildQuestWindow()
        {
            if (!allowActions)
            {
                return BuildQuestSection("Active Quests", questService.GetActiveQuests(), true);
            }

            var text = BuildQuestSection("Available Quests", questService.GetAvailableQuests(), false);
            text += "\n\n" + BuildQuestSection("Active Quests", questService.GetActiveQuests(), true);
            text += "\n\n" + BuildQuestSection("Completed Quests", questService.GetCompletedQuests(), false);
            return text;
        }

        private string BuildQuestSection(string title, System.Collections.Generic.IReadOnlyList<QuestDefinition> quests, bool showProgress)
        {
            var text = $"{title}";
            if (quests.Count == 0)
            {
                return text + "\n- None";
            }

            foreach (var quest in quests)
            {
                text += $"\n- {quest.Title}";
                if (showProgress)
                {
                    text += BuildQuestProgress(quest);
                }
            }

            return text;
        }

        private string BuildQuestProgress(QuestDefinition quest)
        {
            var text = string.Empty;

            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var progress = questService.GetObjectiveProgress(quest.Id, i);
                var label = string.IsNullOrEmpty(objective.displayText)
                    ? $"{objective.objectiveType}: {objective.targetId}"
                    : objective.displayText;
                text += $"\n  {label}: {progress}/{objective.requiredAmount}";
            }

            return text;
        }

        private void SetAction(UnityEngine.Events.UnityAction action, string label, bool interactable)
        {
            if (actionButton == null)
            {
                return;
            }

            actionButton.onClick.RemoveAllListeners();
            if (action != null)
            {
                actionButton.onClick.AddListener(() =>
                {
                    action();
                    runtime.Save();
                    Refresh();
                });
            }

            actionButton.interactable = interactable;
            actionButton.GetComponentInChildren<UnityEngine.UI.Text>().text = label;
        }

        private void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (questService != null)
            {
                questService.Changed -= Refresh;
            }

            if (inventoryService != null)
            {
                inventoryService.Changed -= Refresh;
            }
        }
    }
}
