using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Game Catalog", fileName = "GameCatalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [Header("Demo Flow")]
        [SerializeField] private CharacterDefinition defaultCharacter;
        [SerializeField] private ZoneDefinition villageZone;
        [SerializeField] private ZoneDefinition forestZone;
        [Tooltip("Optional third area slot for a later demo expansion.")]
        [SerializeField] private ZoneDefinition expansionZone;

        [Header("Expandable Rosters")]
        [Tooltip("Keep at least two entries available in UI so a second character can be added without changing code.")]
        [SerializeField] private List<CharacterDefinition> playableCharacters = new List<CharacterDefinition>(2);
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [SerializeField] private List<QuestDefinition> quests = new List<QuestDefinition>();
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();
        [SerializeField] private List<TalentDefinition> talents = new List<TalentDefinition>();
        [SerializeField] private List<SkillNodeDefinition> skillNodes = new List<SkillNodeDefinition>();
        [SerializeField] private List<ZoneDefinition> zones = new List<ZoneDefinition>();
        [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        public CharacterDefinition DefaultCharacter => defaultCharacter;
        public ZoneDefinition VillageZone => villageZone;
        public ZoneDefinition ForestZone => forestZone;
        public ZoneDefinition ExpansionZone => expansionZone;
        public IReadOnlyList<CharacterDefinition> PlayableCharacters => playableCharacters;
        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public IReadOnlyList<QuestDefinition> Quests => quests;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public IReadOnlyList<TalentDefinition> Talents => talents;
        public IReadOnlyList<SkillNodeDefinition> SkillNodes => skillNodes;
        public IReadOnlyList<ZoneDefinition> Zones => zones;
        public IReadOnlyList<RecipeDefinition> Recipes => recipes;

        public CharacterDefinition FindCharacter(string id)
        {
            return playableCharacters.FirstOrDefault(character => character != null && character.Id == id);
        }

        public ItemDefinition FindItem(string id)
        {
            return items.FirstOrDefault(item => item != null && item.Id == id);
        }

        public EnemyDefinition FindEnemy(string id)
        {
            return enemies.FirstOrDefault(enemy => enemy != null && enemy.Id == id);
        }

        public QuestDefinition FindQuest(string id)
        {
            return quests.FirstOrDefault(quest => quest != null && quest.Id == id);
        }

        public SkillDefinition FindSkill(string id)
        {
            return skills.FirstOrDefault(skill => skill != null && skill.Id == id);
        }

        public TalentDefinition FindTalent(string id)
        {
            return talents.FirstOrDefault(talent => talent != null && talent.Id == id);
        }

        public SkillNodeDefinition FindSkillNode(string id)
        {
            return skillNodes.FirstOrDefault(node => node != null && node.Id == id);
        }

        public ZoneDefinition FindZone(string id)
        {
            return zones.FirstOrDefault(zone => zone != null && zone.Id == id);
        }

        public RecipeDefinition FindRecipe(string id)
        {
            return recipes.FirstOrDefault(recipe => recipe != null && recipe.Id == id);
        }
    }
}
