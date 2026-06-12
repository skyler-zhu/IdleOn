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
                new Vector2(0.02f, 0.18f),
                new Vector2(0.25f, 0.78f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.08f, 0.10f, 0.92f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Quest", 23, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            bodyText = RuntimeUiFactory.CreateText(root, "Body", string.Empty, 17, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.96f));
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
            if (isDisposed || root == null || bodyText == null)
            {
                Dispose();
                return;
            }

            var quest = questService.GetPrimaryQuest();
            if (quest == null)
            {
                bodyText.text = "All demo quests complete.";
                SetAction(null, string.Empty, false);
                return;
            }

            var active = questService.IsQuestActive(quest.Id);
            var completed = questService.IsQuestCompleted(quest.Id);
            bodyText.text = BuildQuestText(quest, active, completed);

            if (!allowActions)
            {
                return;
            }

            if (completed)
            {
                SetAction(null, "Done", false);
            }
            else if (!active)
            {
                SetAction(() => questService.AcceptQuest(quest.Id), "Accept", true);
            }
            else
            {
                var canComplete = questService.CanComplete(quest.Id);
                SetAction(() => questService.CompleteQuest(quest.Id), "Complete", canComplete);
            }
        }

        private string BuildQuestText(QuestDefinition quest, bool active, bool completed)
        {
            var state = completed ? "Complete" : active ? "Active" : "Available";
            var text = $"{quest.Title}\n{state}\n\n{quest.Description}\n";

            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var progress = questService.GetObjectiveProgress(quest.Id, i);
                var label = string.IsNullOrEmpty(objective.displayText)
                    ? $"{objective.objectiveType}: {objective.targetId}"
                    : objective.displayText;
                text += $"\n{label}: {progress}/{objective.requiredAmount}";
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
