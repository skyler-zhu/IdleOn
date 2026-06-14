using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Skill Node", fileName = "SkillNode_")]
    public sealed class SkillNodeDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private SkillType skillType = SkillType.Chopping;
        [Min(1)]
        [SerializeField] private int maxRank = 5;
        [SerializeField] private SkillNodeEffectType effectType;
        [SerializeField] private float valuePerRank = 0.05f;
        [SerializeField] private List<SkillNodeDefinition> prerequisites = new List<SkillNodeDefinition>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public SkillType SkillType => skillType;
        public int MaxRank => maxRank;
        public SkillNodeEffectType EffectType => effectType;
        public float ValuePerRank => valuePerRank;
        public IReadOnlyList<SkillNodeDefinition> Prerequisites => prerequisites;
    }
}
