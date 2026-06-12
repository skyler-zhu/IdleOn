using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Skill", fileName = "Skill_")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Art")]
        [SerializeField] private Sprite icon;

        [Header("Rules")]
        [SerializeField] private SkillType skillType;
        [Min(1)]
        [SerializeField] private int baseExperienceToLevel = 50;
        [Min(1f)]
        [SerializeField] private float levelExperienceMultiplier = 1.35f;
        [SerializeField] private QuestDefinition unlockedByQuest;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public SkillType SkillType => skillType;
        public QuestDefinition UnlockedByQuest => unlockedByQuest;

        public int GetExperienceRequiredForLevel(int level)
        {
            var clampedLevel = Mathf.Max(1, level);
            return Mathf.RoundToInt(baseExperienceToLevel * Mathf.Pow(levelExperienceMultiplier, clampedLevel - 1));
        }
    }
}
