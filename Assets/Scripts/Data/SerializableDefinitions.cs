using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [Serializable]
    public sealed class ItemStackDefinition
    {
        public ItemDefinition item;
        [Min(1)] public int quantity = 1;
    }

    [Serializable]
    public sealed class LootEntry
    {
        public ItemDefinition item;
        [Range(0f, 1f)] public float dropChance = 1f;
        [Min(1)] public int minQuantity = 1;
        [Min(1)] public int maxQuantity = 1;
    }

    [Serializable]
    public sealed class RewardDefinition
    {
        [Min(0)] public int coins;
        [Min(0)] public int experience;
        public List<ItemStackDefinition> items = new List<ItemStackDefinition>();
    }

    [Serializable]
    public sealed class QuestObjectiveDefinition
    {
        public QuestObjectiveType objectiveType;
        public string targetId;
        [Min(1)] public int requiredAmount = 1;
        [TextArea] public string displayText;
    }

    [Serializable]
    public sealed class RecipeIngredient
    {
        public ItemDefinition item;
        [Min(1)] public int quantity = 1;
    }

    [Serializable]
    public sealed class ZoneEnemySpawn
    {
        public EnemyDefinition enemy;
        [Min(1)] public int weight = 1;
    }

    [Serializable]
    public sealed class ZoneResourceNode
    {
        public string nodeId;
        public string displayName;
        public SkillType skillType = SkillType.Chopping;
        public Sprite icon;
        public GameObject nodePrefab;
        public List<LootEntry> resourceDrops = new List<LootEntry>();
        [Min(0.1f)] public float gatherSeconds = 2f;
        [Min(0)] public int skillExperience = 5;
    }
}
