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
        private const float ContactDistance = 1.2f;
        private const float MeleeAttackRange = 2.15f;
        private const float DesiredMeleeDistance = 1.55f;
        private const float PlayerMoveSpeed = 1.65f;
        private const float EnemyWanderSpeed = 0.58f;
        private const float EnemyWanderRadius = 1.05f;
        private const float EnemyVerticalWanderRadius = 0.08f;
        private const float ManualMoveMinX = -3.55f;
        private const float ManualMoveMaxX = 3.45f;
        private const float JumpDuration = 0.72f;
        private static readonly Vector3 PlayerPositionValue = new Vector3(-3.25f, -1.05f, 0f);
        private static readonly Vector3 TreeGatherPosition = new Vector3(2.55f, -1.05f, 0f);

        private readonly GameState state;
        private readonly InventoryService inventoryService;
        private readonly EquipmentService equipmentService;
        private readonly QuestService questService;
        private readonly System.Random random = new System.Random();
        private readonly List<CombatEnemyInstance> enemies = new List<CombatEnemyInstance>();

        private Vector3 playerPosition = PlayerPositionValue;
        private float restRemainingSeconds;
        private float jumpRemainingSeconds;

        public CombatService(GameState state, InventoryService inventoryService, EquipmentService equipmentService, QuestService questService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
            this.equipmentService = equipmentService;
            this.questService = questService;

            if (state.SaveData.currentHp <= 0)
            {
                state.SaveData.currentHp = MaxPlayerHp;
            }
        }

        public IReadOnlyList<CombatEnemyInstance> Enemies => enemies;
        public CombatEnemyInstance CurrentTarget { get; private set; }
        public Vector3 PlayerPosition => playerPosition;
        public int PlayerDamage => 5 + state.SaveData.level * 2 + equipmentService.GetAttackBonus();
        public int MaxPlayerHp => 50 + state.SaveData.level * 10;
        public int ExperienceRequired => ProgressionService.GetExperienceRequired(state.SaveData.level);
        public bool IsResting => restRemainingSeconds > 0f;
        public bool IsPlayerNearTree => Vector3.Distance(playerPosition, TreeGatherPosition) <= 0.85f;
        public bool IsJumping => jumpRemainingSeconds > 0f;
        public float JumpProgress => IsJumping ? Mathf.Clamp01(1f - jumpRemainingSeconds / JumpDuration) : 0f;

        public event Action Changed;
        public event Action<string> LogAdded;
        public event Action<CombatEnemyInstance> EnemySpawned;
        public event Action<CombatEnemyInstance> EnemyDamaged;
        public event Action<CombatEnemyInstance> EnemyDefeated;
        public event Action PlayerAttacked;
        public event Action PlayerDamaged;
        public event Action PlayerRestStarted;
        public event Action PlayerRestEnded;

        public void SpawnInitialEnemies(int count)
        {
            enemies.Clear();
            for (var i = 0; i < count; i++)
            {
                SpawnEnemyAtIndex(i, null);
            }

            SelectCurrentTarget();
            NotifyChanged();
        }

        public CombatEnemyInstance ReplaceEnemy(CombatEnemyInstance previousEnemy)
        {
            var index = enemies.IndexOf(previousEnemy);
            if (index < 0)
            {
                index = enemies.Count;
            }

            return SpawnEnemyAtIndex(index, previousEnemy);
        }

        public bool AttackCurrentTarget()
        {
            if (IsResting)
            {
                AddLog("Resting...");
                NotifyChanged();
                return false;
            }

            SelectCurrentTarget();
            if (CurrentTarget == null)
            {
                AddLog("No target.");
                NotifyChanged();
                return false;
            }

            if (Vector3.Distance(PlayerPosition, CurrentTarget.currentPosition) > MeleeAttackRange)
            {
                PlayerAttacked?.Invoke();
                AddLog($"{CurrentTarget.enemyDefinition.DisplayName} is out of melee range.");
                NotifyChanged();
                return false;
            }

            PlayerAttacked?.Invoke();
            CurrentTarget.currentHp = Mathf.Max(0, CurrentTarget.currentHp - PlayerDamage);
            EnemyDamaged?.Invoke(CurrentTarget);
            AddLog($"You hit {CurrentTarget.enemyDefinition.DisplayName} for {PlayerDamage}.");

            if (CurrentTarget.currentHp > 0)
            {
                NotifyChanged();
                return false;
            }

            AwardEnemyRewards(CurrentTarget.enemyDefinition);
            questService.AddProgress(QuestObjectiveType.KillEnemy, CurrentTarget.enemyDefinition.Id, 1);
            EnemyDefeated?.Invoke(CurrentTarget);
            SelectCurrentTarget();
            NotifyChanged();
            return true;
        }

        public void Tick(float deltaTime, float time, bool fightingActive, bool autoMode)
        {
            if (jumpRemainingSeconds > 0f)
            {
                jumpRemainingSeconds -= deltaTime;
                if (jumpRemainingSeconds <= 0f)
                {
                    jumpRemainingSeconds = 0f;
                    NotifyChanged();
                }
            }

            if (IsResting)
            {
                restRemainingSeconds -= deltaTime;
                if (restRemainingSeconds <= 0f)
                {
                    state.SaveData.currentHp = MaxPlayerHp;
                    AddLog("Rested up. Back to work.");
                    PlayerRestEnded?.Invoke();
                    NotifyChanged();
                }

                return;
            }

            if (autoMode && fightingActive)
            {
                MovePlayerTowardTarget(deltaTime);
            }
            else if (autoMode)
            {
                playerPosition = Vector3.MoveTowards(playerPosition, TreeGatherPosition, PlayerMoveSpeed * deltaTime);
            }

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                WanderEnemy(enemy, deltaTime, time);

                if (fightingActive && !IsJumping && Vector3.Distance(enemy.currentPosition, PlayerPosition) <= ContactDistance && time >= enemy.nextAttackTime)
                {
                    enemy.nextAttackTime = time + enemy.enemyDefinition.AttackInterval;
                    DamagePlayer(enemy.enemyDefinition.AttackDamage, enemy.enemyDefinition.DisplayName);
                }
            }

            SelectCurrentTarget();
            NotifyChanged();
        }

        public void MovePlayerManual(float horizontal, float deltaTime)
        {
            if (Mathf.Abs(horizontal) <= 0.01f || IsResting)
            {
                return;
            }

            playerPosition.x = Mathf.Clamp(playerPosition.x + horizontal * PlayerMoveSpeed * deltaTime, ManualMoveMinX, ManualMoveMaxX);
            NotifyChanged();
        }

        public void Jump()
        {
            if (IsResting || IsJumping)
            {
                return;
            }

            jumpRemainingSeconds = JumpDuration;
            AddLog("Jumped over danger.");
            NotifyChanged();
        }

        private void MovePlayerTowardTarget(float deltaTime)
        {
            SelectCurrentTarget();
            if (CurrentTarget == null)
            {
                playerPosition = Vector3.MoveTowards(playerPosition, PlayerPositionValue, PlayerMoveSpeed * deltaTime);
                return;
            }

            var direction = CurrentTarget.currentPosition - playerPosition;
            var distance = direction.magnitude;
            if (distance <= DesiredMeleeDistance)
            {
                return;
            }

            var targetPosition = CurrentTarget.currentPosition - direction.normalized * DesiredMeleeDistance;
            playerPosition = Vector3.MoveTowards(playerPosition, targetPosition, PlayerMoveSpeed * deltaTime);
        }

        public void PushEnemyBack(CombatEnemyInstance enemy, float amount)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.currentPosition += Vector3.right * amount;
        }

        private void WanderEnemy(CombatEnemyInstance enemy, float deltaTime, float time)
        {
            var offsetX = Mathf.Sin(time * 0.9f + enemy.spawnPosition.x) * EnemyWanderRadius;
            var offsetY = Mathf.Cos(time * 0.7f + enemy.spawnPosition.y) * EnemyVerticalWanderRadius;
            var target = enemy.spawnPosition + new Vector3(offsetX, offsetY, 0f);
            enemy.currentPosition = Vector3.MoveTowards(enemy.currentPosition, target, EnemyWanderSpeed * deltaTime);
        }

        private CombatEnemyInstance SpawnEnemyAtIndex(int index, CombatEnemyInstance replace)
        {
            var definition = PickEnemy();
            if (definition == null)
            {
                AddLog("No enemies are configured for this zone yet.");
                return null;
            }

            var spawnPosition = GetSpawnPosition(index);
            var enemy = replace ?? new CombatEnemyInstance();
            enemy.enemyDefinition = definition;
            enemy.currentHp = definition.MaxHp;
            enemy.spawnPosition = spawnPosition;
            enemy.currentPosition = spawnPosition;
            enemy.nextAttackTime = 0f;

            if (replace == null)
            {
                enemies.Add(enemy);
            }

            AddLog($"A {definition.DisplayName} appears.");
            EnemySpawned?.Invoke(enemy);
            return enemy;
        }

        private Vector3 GetSpawnPosition(int index)
        {
            var row = index % 3;
            return new Vector3(0.35f + row * 1.15f, -1.15f + row * 0.06f, 0f);
        }

        private void SelectCurrentTarget()
        {
            CombatEnemyInstance best = null;
            var bestDistance = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(PlayerPosition, enemy.currentPosition);
                if (distance < bestDistance)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            CurrentTarget = best;
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

        private void DamagePlayer(int damage, string enemyName)
        {
            state.SaveData.currentHp = Mathf.Max(0, state.SaveData.currentHp - damage);
            AddLog($"{enemyName} hits you for {damage}.");
            PlayerDamaged?.Invoke();

            if (state.SaveData.currentHp <= 0)
            {
                restRemainingSeconds = 3f;
                AddLog("You are resting for 3 seconds.");
                PlayerRestStarted?.Invoke();
            }
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
                state.SaveData.currentHp = MaxPlayerHp;
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
