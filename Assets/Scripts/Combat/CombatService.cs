using System;
using System.Collections.Generic;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Equipment;
using IdleOnLike.Inventory;
using IdleOnLike.Progression;
using IdleOnLike.Quests;
using UnityEngine;

namespace IdleOnLike.Combat
{
    public sealed class CombatService
    {
        private readonly GameState state;
        private readonly InventoryService inventoryService;
        private readonly EquipmentService equipmentService;
        private readonly QuestService questService;
        private readonly System.Random random = new System.Random();

        public CombatService(GameState state, InventoryService inventoryService, EquipmentService equipmentService, QuestService questService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
            this.equipmentService = equipmentService;
            this.questService = questService;
        }

        public EnemyDefinition CurrentEnemy { get; private set; }
        public int CurrentEnemyHp { get; private set; }

        public int PlayerDamage => 5 + state.SaveData.level * 2 + equipmentService.GetAttackBonus();
        public int ExperienceRequired => ProgressionService.GetExperienceRequired(state.SaveData.level);

        public event Action Changed;
        public event Action<string> LogAdded;
        public event Action EnemyDefeated;

        public void SpawnNextEnemy()
        {
            CurrentEnemy = PickEnemy();
            if (CurrentEnemy == null)
            {
                CurrentEnemyHp = 0;
                AddLog("No enemies are configured for this zone yet.");
                NotifyChanged();
                return;
            }

            CurrentEnemyHp = CurrentEnemy.MaxHp;
            AddLog($"A {CurrentEnemy.DisplayName} appears.");
            NotifyChanged();
        }

        public bool AttackCurrentEnemy()
        {
            if (CurrentEnemy == null)
            {
                SpawnNextEnemy();
                return false;
            }

            CurrentEnemyHp = Mathf.Max(0, CurrentEnemyHp - PlayerDamage);
            AddLog($"You hit {CurrentEnemy.DisplayName} for {PlayerDamage}.");

            if (CurrentEnemyHp > 0)
            {
                NotifyChanged();
                return false;
            }

            AwardEnemyRewards(CurrentEnemy);
            questService.AddProgress(QuestObjectiveType.KillEnemy, CurrentEnemy.Id, 1);
            EnemyDefeated?.Invoke();
            NotifyChanged();
            return true;
        }

        private EnemyDefinition PickEnemy()
        {
            var zone = state.CurrentZone;
            var fallbackZone = state.Catalog.ForestZone;
            var spawns = zone != null && zone.Enemies.Count > 0
                ? zone.Enemies
                : fallbackZone != null ? fallbackZone.Enemies : Array.Empty<ZoneEnemySpawn>();
            var validSpawns = new List<ZoneEnemySpawn>();
            var totalWeight = 0;

            foreach (var spawn in spawns)
            {
                if (spawn == null || spawn.enemy == null || spawn.weight <= 0)
                {
                    continue;
                }

                validSpawns.Add(spawn);
                totalWeight += spawn.weight;
            }

            if (validSpawns.Count == 0 || totalWeight <= 0)
            {
                return state.Catalog.Enemies.Count > 0 ? state.Catalog.Enemies[0] : null;
            }

            var roll = random.Next(0, totalWeight);
            foreach (var spawn in validSpawns)
            {
                if (roll < spawn.weight)
                {
                    return spawn.enemy;
                }

                roll -= spawn.weight;
            }

            return validSpawns[validSpawns.Count - 1].enemy;
        }

        private void AwardEnemyRewards(EnemyDefinition enemy)
        {
            var coins = random.Next(enemy.MinCoins, enemy.MaxCoins + 1);
            state.SaveData.coins += coins;
            var levelsGained = ProgressionService.AddExperience(state.SaveData, enemy.ExperienceReward);
            AddLog($"{enemy.DisplayName} defeated. +{enemy.ExperienceReward} XP, +{coins} coins.");
            AwardLoot(enemy);

            if (levelsGained > 0)
            {
                AddLog($"Level up! You are now level {state.SaveData.level}.");
            }
        }

        private void AwardLoot(EnemyDefinition enemy)
        {
            foreach (var loot in enemy.LootTable)
            {
                if (loot == null || loot.item == null)
                {
                    continue;
                }

                if (random.NextDouble() > loot.dropChance)
                {
                    continue;
                }

                var minQuantity = Mathf.Max(1, loot.minQuantity);
                var maxQuantity = Mathf.Max(minQuantity, loot.maxQuantity);
                var quantity = random.Next(minQuantity, maxQuantity + 1);
                inventoryService.AddItem(loot.item.Id, quantity);
                AddLog($"Found {loot.item.DisplayName} x{quantity}.");
            }
        }

        private void AddLog(string message)
        {
            LogAdded?.Invoke(message);
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
