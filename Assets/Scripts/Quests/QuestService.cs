using System;
using System.Collections.Generic;
using System.Linq;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Inventory;
using IdleOnLike.Progression;
using IdleOnLike.Save;

namespace IdleOnLike.Quests
{
    public sealed class QuestService
    {
        private const string SecondCharacterUnlockQuestId = "learn_to_chop";

        private readonly GameState state;
        private readonly InventoryService inventoryService;

        public QuestService(GameState state, InventoryService inventoryService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
            this.state.SaveData.EnsureCollections();
            ApplyCurrentCharacterProgressToActiveQuests();
        }

        public event Action Changed;
        public event Action<string> LogAdded;

        public bool AcceptQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId) || IsQuestActive(questId) || IsQuestCompleted(questId))
            {
                return false;
            }

            var quest = state.Catalog.FindQuest(questId);
            if (quest == null || !IsAvailable(quest))
            {
                return false;
            }

            state.SaveData.activeQuestIds.Add(questId);
            ApplyCurrentCharacterProgress(quest);
            LogAdded?.Invoke($"Quest accepted: {quest.Title}");
            if (CanComplete(questId))
            {
                CompleteQuest(questId);
                return true;
            }

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
            UnlockCharactersForQuest(quest);
            LogAdded?.Invoke($"Quest complete: {quest.Title}");

            if (quest.NextQuest != null)
            {
                AcceptQuest(quest.NextQuest.Id);
            }

            Changed?.Invoke();
            return true;
        }

        public void CompleteAutoCompletableSwitchCharacterQuests()
        {
            CompleteAutoCompletableSwitchCharacterQuests(state.SaveData.characterId);
        }

        public void CompleteAutoCompletableSwitchCharacterQuests(string switchedToCharacterId)
        {
            var quests = GetActiveQuests()
                .Where(HasSwitchCharacterObjective)
                .ToList();

            foreach (var quest in quests)
            {
                ApplyCurrentCharacterProgress(quest, switchedToCharacterId);
                if (CanComplete(quest.Id))
                {
                    CompleteQuest(quest.Id);
                }
            }
        }

        private void UnlockCharactersForQuest(QuestDefinition quest)
        {
            if (quest.Id != SecondCharacterUnlockQuestId || state.Catalog.PlayableCharacters.Count < 2)
            {
                return;
            }

            var character = state.Catalog.PlayableCharacters[1];
            state.AccountData.EnsureCollections();
            if (character == null || state.AccountData.unlockedCharacterIds.Contains(character.Id))
            {
                return;
            }

            state.AccountData.unlockedCharacterIds.Add(character.Id);
            LogAdded?.Invoke($"Character unlocked: {character.DisplayName}.");
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
            return GetNextActionableQuest();
        }

        public IReadOnlyList<QuestDefinition> GetAvailableQuests()
        {
            return state.Catalog.Quests
                .Where(IsAvailable)
                .ToList();
        }

        public IReadOnlyList<QuestDefinition> GetActiveQuests()
        {
            return state.SaveData.activeQuestIds
                .Select(questId => state.Catalog.FindQuest(questId))
                .Where(quest => quest != null)
                .ToList();
        }

        public IReadOnlyList<QuestDefinition> GetCompletedQuests()
        {
            return state.SaveData.completedQuestIds
                .Select(questId => state.Catalog.FindQuest(questId))
                .Where(quest => quest != null)
                .ToList();
        }

        public QuestDefinition GetNextActionableQuest()
        {
            foreach (var quest in GetActiveQuests())
            {
                if (CanComplete(quest.Id))
                {
                    return quest;
                }
            }

            var activeQuest = GetActiveQuests().FirstOrDefault();
            if (activeQuest != null)
            {
                return activeQuest;
            }

            return GetAvailableQuests().FirstOrDefault();
        }

        private bool IsPrerequisiteMet(QuestDefinition quest)
        {
            if (!string.IsNullOrEmpty(quest.PrerequisiteQuestId) && !IsQuestCompleted(quest.PrerequisiteQuestId))
            {
                return false;
            }

            foreach (var possiblePrevious in state.Catalog.Quests)
            {
                if (possiblePrevious != null && possiblePrevious.NextQuest == quest)
                {
                    return IsQuestCompleted(possiblePrevious.Id);
                }
            }

            return true;
        }

        private void ApplyCurrentCharacterProgressToActiveQuests()
        {
            var changed = false;
            foreach (var quest in GetActiveQuests())
            {
                changed |= ApplyCurrentCharacterProgress(quest);
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        private bool ApplyCurrentCharacterProgress(QuestDefinition quest)
        {
            return ApplyCurrentCharacterProgress(quest, state.SaveData.characterId);
        }

        private bool ApplyCurrentCharacterProgress(QuestDefinition quest, string switchedToCharacterId)
        {
            if (quest == null || string.IsNullOrEmpty(switchedToCharacterId))
            {
                return false;
            }

            var changed = false;
            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                if (objective.objectiveType != QuestObjectiveType.SwitchCharacter)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(objective.targetId) && objective.targetId != switchedToCharacterId)
                {
                    continue;
                }

                var progress = GetProgressEntry(quest.Id, i, true);
                var previous = progress.currentAmount;
                progress.currentAmount = Math.Min(objective.requiredAmount, progress.currentAmount + 1);
                changed |= previous != progress.currentAmount;
            }

            return changed;
        }

        private static bool HasSwitchCharacterObjective(QuestDefinition quest)
        {
            return quest != null && quest.Objectives.Any(objective => objective.objectiveType == QuestObjectiveType.SwitchCharacter);
        }

        private bool IsAvailable(QuestDefinition quest)
        {
            if (quest == null || IsQuestActive(quest.Id) || IsQuestCompleted(quest.Id))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(quest.RequiredCharacterId) && state.SaveData.characterId != quest.RequiredCharacterId)
            {
                return false;
            }

            return IsPrerequisiteMet(quest);
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

            state.Coins += reward.coins;
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
