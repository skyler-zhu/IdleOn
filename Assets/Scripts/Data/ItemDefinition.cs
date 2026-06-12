using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Item", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Art")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite worldSprite;
        [SerializeField] private GameObject worldDropPrefab;

        [Header("Rules")]
        [SerializeField] private ItemType itemType = ItemType.Material;
        [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.None;
        [Min(1)]
        [SerializeField] private int maxStack = 99;
        [Min(0)]
        [SerializeField] private int sellValue;

        [Header("Equipment")]
        [SerializeField] private StatBlock equipStats;

        [Header("Crafting")]
        [SerializeField] private List<RecipeIngredient> craftingIngredients = new List<RecipeIngredient>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Sprite WorldSprite => worldSprite;
        public GameObject WorldDropPrefab => worldDropPrefab;
        public ItemType ItemType => itemType;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public int MaxStack => maxStack;
        public int SellValue => sellValue;
        public StatBlock EquipStats => equipStats;
        public IReadOnlyList<RecipeIngredient> CraftingIngredients => craftingIngredients;
        public bool IsEquipment => itemType == ItemType.Equipment && equipmentSlot != EquipmentSlot.None;
    }
}
