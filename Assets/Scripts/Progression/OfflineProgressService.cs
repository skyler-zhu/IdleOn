using System;
using System.Collections.Generic;
using IdleOnLike.Data;
using IdleOnLike.Inventory;
using IdleOnLike.Quests;
using IdleOnLike.Save;
using UnityEngine;

namespace IdleOnLike.Progression
{
    public sealed class OfflineProgressService
    {
        private const int MaxOfflineHours = 8;
        private const int FightingKillsPerMinute = 4;
        private const int ChoppingWoodPerMinute = 8;
        private const int MiningOrePerMinute = 8;
        private readonly System.Random random = new System.Random();

        private readonly PlayerSaveData saveData;
        private readonly AccountSaveData accountData;
        private readonly GameCatalog catalog;
        private readonly InventoryService inventoryService;
        private readonly QuestService questService;
        private readonly SkillTreeService skillTreeService;

        public OfflineProgressService(PlayerSaveData saveData, AccountSaveData accountData, GameCatalog catalog, InventoryService inventoryService, QuestService questService, SkillTreeService skillTreeService)
        {
            this.saveData = saveData;
            this.accountData = accountData;
            this.catalog = catalog;
            this.inventoryService = inventoryService;
            this.questService = questService;
            this.skillTreeService = skillTreeService;
        }

        public OfflineGainsResult CalculateOfflineGains(TimeSpan elapsed)
        {
            var cappedElapsed = elapsed > TimeSpan.FromHours(MaxOfflineHours)
                ? TimeSpan.FromHours(MaxOfflineHours)
                : elapsed;
            var result = new OfflineGainsResult
            {
                elapsed = cappedElapsed,
                activity = ParseActivity(saveData.currentActivity)
            };

            var minutes = Mathf.Max(1, Mathf.FloorToInt((float)cappedElapsed.TotalMinutes));
            if (result.activity == ZoneActivity.Chopping)
            {
                ApplyChopping(minutes, result);
            }
            else if (result.activity == ZoneActivity.Mining)
            {
                ApplyMining(minutes, result);
            }
            else
            {
                ApplyFighting(minutes, result);
            }

            return result;
        }

        private void ApplyFighting(int minutes, OfflineGainsResult result)
        {
            var zone = catalog.FindZone(saveData.currentZoneId) ?? catalog.ForestZone;
            var kills = minutes * FightingKillsPerMinute;
            for (var i = 0; i < kills; i++)
            {
                var enemy = PickEnemy(zone);
                if (enemy == null)
                {
                    continue;
                }

                var coins = random.Next(enemy.MinCoins, enemy.MaxCoins + 1);
                accountData.coins += coins;
                result.coins += coins;
                result.experience += enemy.ExperienceReward;
                questService.AddProgress(QuestObjectiveType.KillEnemy, enemy.Id, 1);
                AddQuestSummary(result, $"Kill {enemy.DisplayName}");
                ApplyLoot(enemy, result);
            }

            result.levelsGained = ProgressionService.AddExperience(saveData, result.experience);
        }

        private void ApplyChopping(int minutes, OfflineGainsResult result)
        {
            var quantity = GetOfflineGatherQuantity(SkillType.Chopping, minutes, ChoppingWoodPerMinute);
            inventoryService.AddItem("wood", quantity);
            questService.AddProgress(QuestObjectiveType.GatherResource, "tree", quantity);
            skillTreeService.AddSkillExperience(SkillType.Chopping, quantity * 5);
            AddItem(result, "wood", "Wood", quantity);
            AddQuestSummary(result, "Gather tree");
        }

        private void ApplyMining(int minutes, OfflineGainsResult result)
        {
            var quantity = GetOfflineGatherQuantity(SkillType.Mining, minutes, MiningOrePerMinute);
            inventoryService.AddItem("ore", quantity);
            questService.AddProgress(QuestObjectiveType.GatherResource, "rock", quantity);
            skillTreeService.AddSkillExperience(SkillType.Mining, quantity * 5);
            AddItem(result, "ore", "Ore", quantity);
            AddQuestSummary(result, "Gather rock");
        }

        private int GetOfflineGatherQuantity(SkillType skillType, int minutes, int basePerMinute)
        {
            var gatherSeconds = skillTreeService.GetGatherSeconds(skillType, 2f);
            var speedMultiplier = 2f / gatherSeconds;
            var baseQuantity = Mathf.FloorToInt(minutes * basePerMinute * speedMultiplier);
            var extraChance = skillTreeService.GetExtraDropChance(skillType);
            return Mathf.Max(0, baseQuantity + Mathf.FloorToInt(baseQuantity * extraChance));
        }

        private EnemyDefinition PickEnemy(ZoneDefinition zone)
        {
            if (zone == null && catalog != null)
            {
                zone = catalog.ForestZone;
            }

            var spawns = zone != null ? zone.Enemies : Array.Empty<ZoneEnemySpawn>();
            var totalWeight = 0;
            foreach (var spawn in spawns)
            {
                if (spawn?.enemy != null && spawn.weight > 0)
                {
                    totalWeight += spawn.weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = random.Next(0, totalWeight);
            foreach (var spawn in spawns)
            {
                if (spawn?.enemy == null || spawn.weight <= 0)
                {
                    continue;
                }

                if (roll < spawn.weight)
                {
                    return spawn.enemy;
                }

                roll -= spawn.weight;
            }

            return null;
        }

        private void ApplyLoot(EnemyDefinition enemy, OfflineGainsResult result)
        {
            foreach (var loot in enemy.LootTable)
            {
                if (loot?.item == null || random.NextDouble() > loot.dropChance)
                {
                    continue;
                }

                var minQuantity = Mathf.Max(1, loot.minQuantity);
                var maxQuantity = Mathf.Max(minQuantity, loot.maxQuantity);
                var quantity = random.Next(minQuantity, maxQuantity + 1);
                inventoryService.AddItem(loot.item.Id, quantity);
                AddItem(result, loot.item.Id, loot.item.DisplayName, quantity);
            }
        }

        private static ZoneActivity ParseActivity(string value)
        {
            return Enum.TryParse(value, out ZoneActivity activity) ? activity : ZoneActivity.Fighting;
        }

        private static void AddItem(OfflineGainsResult result, string itemId, string displayName, int quantity)
        {
            var existing = result.items.Find(item => item.itemId == itemId);
            if (existing != null)
            {
                existing.quantity += quantity;
                return;
            }

            result.items.Add(new OfflineItemReward
            {
                itemId = itemId,
                displayName = displayName,
                quantity = quantity
            });
        }

        private static void AddQuestSummary(OfflineGainsResult result, string label)
        {
            if (!result.questProgress.Contains(label))
            {
                result.questProgress.Add(label);
            }
        }
    }
}
