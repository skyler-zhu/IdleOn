using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Inventory;

namespace IdleOnLike.Crafting
{
    public sealed class CraftingService
    {
        private readonly GameState state;
        private readonly InventoryService inventoryService;

        public CraftingService(GameState state, InventoryService inventoryService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
        }

        public event Action Changed;
        public event Action<string> LogAdded;
        public event Action<QuestObjectiveType, string, int> ItemCrafted;

        public bool CanCraft(string recipeId)
        {
            var recipe = state.Catalog.FindRecipe(recipeId);
            if (recipe == null || recipe.Output == null || recipe.Output.item == null)
            {
                return false;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient?.item == null || inventoryService.GetQuantity(ingredient.item.Id) < ingredient.quantity)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Craft(string recipeId)
        {
            var recipe = state.Catalog.FindRecipe(recipeId);
            if (recipe == null || !CanCraft(recipeId))
            {
                return false;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                inventoryService.RemoveItem(ingredient.item.Id, ingredient.quantity);
            }

            inventoryService.AddItem(recipe.Output.item.Id, recipe.Output.quantity);
            ItemCrafted?.Invoke(QuestObjectiveType.CraftItem, recipe.Output.item.Id, recipe.Output.quantity);
            LogAdded?.Invoke($"Crafted {recipe.Output.item.DisplayName} x{recipe.Output.quantity}.");
            Changed?.Invoke();
            return true;
        }
    }
}
