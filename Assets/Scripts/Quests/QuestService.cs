using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Inventory;
using IdleOnLike.Progression;
using IdleOnLike.Save;

namespace IdleOnLike.Quests
{
    public sealed class QuestService
    {
        private readonly GameState state;
        private readonly InventoryService inventoryService;

        public QuestService(GameState state, InventoryService inventoryService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
            this.state.SaveData.EnsureCollections();
        }

        public event Action Changed;
        public event Action<string> LogAdded;

        public bool AcceptQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId) || IsQuestActive(questId) || IsQuestCompleted(questId))
            {
                return false;
            }

            if (state.Catalog.FindQuest(questId) == null)
            {
                return false;
            }

            state.SaveData.activeQuestIds.Add(questId);
            LogAdded?.Invoke($"Quest accepted: {state.Catalog.FindQuest(questId).Title}");
            Changed?.Invoke();
            return true;
        }

        public bool CompleteQuest(string questId)
        {
            var quest = state.Catalog.FindQuest(questId);
            if (quest == null || !CanComplete(questId))
            {
                return false;
            }

            state.SaveData.activeQuestIds.Remove(questId);
            if (!state.SaveData.completedQuestIds.Contains(questId))
            {
                state.SaveData.completedQuestIds.Add(questId);
            }

            AwardRewards(quest.Rewards);
            LogAdded?.Invoke($"Quest complete: {quest.Title}");

            if (quest.NextQuest != null)
            {
                AcceptQuest(quest.NextQuest.Id);
            }

            Changed?.Invoke();
            return true;
        }

        public bool IsQuestActive(string questId)
        {
            return state.SaveData.activeQuestIds.Contains(questId);
        }

        public bool IsQuestCompleted(string questId)
        {
            return state.SaveData.completedQuestIds.Contains(questId);
        }

        public int GetObjectiveProgress(string questId, int objectiveIndex)
        {
            var progress = GetProgressEntry(questId, objectiveIndex, false);
            return progress != null ? progress.currentAmount : 0;
        }

        public void AddProgress(QuestObjectiveType type, string targetId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var changed = false;
            foreach (var questId in state.SaveData.activeQuestIds)
            {
                var quest = state.Catalog.FindQuest(questId);
                if (quest == null)
                {
                    continue;
                }

                for (var i = 0; i < quest.Objectives.Count; i++)
                {
                    var objective = quest.Objectives[i];
                    if (objective.objectiveType != type || objective.targetId != targetId)
                    {
                        continue;
                    }

                    var progress = GetProgressEntry(quest.Id, i, true);
                    var previous = progress.currentAmount;
                    progress.currentAmount = Math.Min(objective.requiredAmount, progress.currentAmount + amount);
                    changed |= previous != progress.currentAmount;
                }
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public bool CanComplete(string questId)
        {
            var quest = state.Catalog.FindQuest(questId);
            if (quest == null || !IsQuestActive(questId))
            {
                return false;
            }

            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                if (GetObjectiveProgress(questId, i) < quest.Objectives[i].requiredAmount)
                {
                    return false;
                }
            }

            return true;
        }

        public QuestDefinition GetPrimaryQuest()
        {
            foreach (var questId in state.SaveData.activeQuestIds)
            {
                var quest = state.Catalog.FindQuest(questId);
                if (quest != null)
                {
                    return quest;
                }
            }

            foreach (var quest in state.Catalog.Quests)
            {
                if (quest != null && !IsQuestCompleted(quest.Id))
                {
                    return quest;
                }
            }

            return null;
        }

        private SaveQuestProgress GetProgressEntry(string questId, int objectiveIndex, bool create)
        {
            var progress = state.SaveData.questProgress.Find(entry => entry.questId == questId && entry.objectiveIndex == objectiveIndex);
            if (progress == null && create)
            {
                progress = new SaveQuestProgress
                {
                    questId = questId,
                    objectiveIndex = objectiveIndex,
                    currentAmount = 0
                };
                state.SaveData.questProgress.Add(progress);
            }

            return progress;
        }

        private void AwardRewards(RewardDefinition reward)
        {
            if (reward == null)
            {
                return;
            }

            state.SaveData.coins += reward.coins;
            var levels = ProgressionService.AddExperience(state.SaveData, reward.experience);
            if (reward.coins > 0 || reward.experience > 0)
            {
                LogAdded?.Invoke($"Quest reward: +{reward.experience} XP, +{reward.coins} coins.");
            }

            if (levels > 0)
            {
                LogAdded?.Invoke($"Level up! You are now level {state.SaveData.level}.");
            }

            foreach (var itemReward in reward.items)
            {
                if (itemReward?.item == null)
                {
                    continue;
                }

                inventoryService.AddItem(itemReward.item.Id, itemReward.quantity);
                LogAdded?.Invoke($"Quest reward: {itemReward.item.DisplayName} x{itemReward.quantity}.");
            }
        }
    }
}
