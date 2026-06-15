using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Enemy", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Art")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite hitSprite;
        [SerializeField] private Sprite deathSprite;
        [SerializeField] private VisualAnimationClips animationClips = new VisualAnimationClips();
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float visualYOffset;
        [Min(0.1f)]
        [SerializeField] private float visualScale = 1f;

        [Header("Combat")]
        [Min(1)]
        [SerializeField] private int maxHp = 20;
        [Min(0)]
        [SerializeField] private int attackDamage = 2;
        [Min(0.1f)]
        [SerializeField] private float attackInterval = 1.5f;

        [Header("Rewards")]
        [Min(0)]
        [SerializeField] private int experienceReward = 8;
        [Min(0)]
        [SerializeField] private int minCoins = 1;
        [Min(0)]
        [SerializeField] private int maxCoins = 3;
        [SerializeField] private List<LootEntry> lootTable = new List<LootEntry>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite IdleSprite => idleSprite;
        public Sprite HitSprite => hitSprite;
        public Sprite DeathSprite => deathSprite;
        public VisualAnimationClips AnimationClips => animationClips;
        public GameObject EnemyPrefab => enemyPrefab;
        public float VisualYOffset => visualYOffset;
        public float VisualScale => visualScale;
        public int MaxHp => maxHp;
        public int AttackDamage => attackDamage;
        public float AttackInterval => attackInterval;
        public int ExperienceReward => experienceReward;
        public int MinCoins => minCoins;
        public int MaxCoins => maxCoins;
        public IReadOnlyList<LootEntry> LootTable => lootTable;
    }
}
