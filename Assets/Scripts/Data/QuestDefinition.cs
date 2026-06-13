using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Quest", fileName = "Quest_")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string title;
        [TextArea]
        [SerializeField] private string description;

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private string giverName = "Scripticus";
        [SerializeField] private Sprite giverPortrait;

        [Header("Progression")]
        [SerializeField] private string prerequisiteQuestId;
        [SerializeField] private string requiredCharacterId;
        [SerializeField] private List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
        [SerializeField] private RewardDefinition rewards = new RewardDefinition();
        [SerializeField] private QuestDefinition nextQuest;
        [SerializeField] private ZoneDefinition unlockedZone;
        [SerializeField] private SkillDefinition unlockedSkill;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public string GiverName => giverName;
        public Sprite GiverPortrait => giverPortrait;
        public string PrerequisiteQuestId => prerequisiteQuestId;
        public string RequiredCharacterId => requiredCharacterId;
        public IReadOnlyList<QuestObjectiveDefinition> Objectives => objectives;
        public RewardDefinition Rewards => rewards;
        public QuestDefinition NextQuest => nextQuest;
        public ZoneDefinition UnlockedZone => unlockedZone;
        public SkillDefinition UnlockedSkill => unlockedSkill;
    }
}
