using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Recipe", fileName = "Recipe_")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Craft")]
        [SerializeField] private SkillDefinition requiredSkill;
        [Min(1)]
        [SerializeField] private int requiredSkillLevel = 1;
        [SerializeField] private List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        [SerializeField] private ItemStackDefinition output = new ItemStackDefinition();

        [Header("Presentation")]
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public SkillDefinition RequiredSkill => requiredSkill;
        public int RequiredSkillLevel => requiredSkillLevel;
        public IReadOnlyList<RecipeIngredient> Ingredients => ingredients;
        public ItemStackDefinition Output => output;
        public Sprite Icon => icon;
    }
}
