using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Talent", fileName = "Talent_")]
    public sealed class TalentDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [Min(1)]
        [SerializeField] private int maxRank = 5;
        [SerializeField] private TalentStatType statType;
        [SerializeField] private float valuePerRank = 1f;
        [SerializeField] private List<TalentDefinition> prerequisites = new List<TalentDefinition>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int MaxRank => maxRank;
        public TalentStatType StatType => statType;
        public float ValuePerRank => valuePerRank;
        public IReadOnlyList<TalentDefinition> Prerequisites => prerequisites;
    }
}
