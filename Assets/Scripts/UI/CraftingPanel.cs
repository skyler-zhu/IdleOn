using IdleOnLike.Core;
using IdleOnLike.Crafting;
using IdleOnLike.Inventory;
using UnityEngine;

namespace IdleOnLike.UI
{
    public sealed class CraftingPanel
    {
        private readonly GameRuntime runtime;
        private readonly InventoryService inventoryService;
        private readonly CraftingService craftingService;
        private readonly RectTransform root;
        private readonly RectTransform recipeList;
        private bool isDisposed;

        public CraftingPanel(GameRuntime runtime, Transform parent)
        {
            this.runtime = runtime;
            inventoryService = runtime.InventoryService;
            craftingService = runtime.CraftingService;
            root = RuntimeUiFactory.CreatePanel(parent, "Crafting Panel", new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.78f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.09f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Crafting", 28, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.70f, 0.98f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.90f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(Toggle);

            recipeList = RuntimeUiFactory.CreateScrollContent(root, "Recipe List", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.86f), new Color(0.12f, 0.13f, 0.15f, 1f));
            inventoryService.Changed += Refresh;
            craftingService.Changed += Refresh;
            parent.GetComponentInParent<RuntimeUiLifetime>()?.Register(Dispose);

            root.gameObject.SetActive(false);
            RuntimeUiOverlayRegistry.Register(root, Refresh);
            Refresh();
        }

        public void Toggle()
        {
            RuntimeUiOverlayRegistry.Toggle(root, Refresh);
        }

        private void Refresh()
        {
            if (isDisposed || runtime == null || runtime.State == null || recipeList == null)
            {
                Dispose();
                return;
            }

            Clear(recipeList);
            var rowIndex = 0;
            for (var i = 0; i < runtime.Catalog.Recipes.Count; i++)
            {
                var recipe = runtime.Catalog.Recipes[i];
                if (recipe == null || recipe.Output?.item == null)
                {
                    continue;
                }

                var row = RuntimeUiFactory.CreatePanel(recipeList, $"Recipe {i}", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -88f - rowIndex * 96f), new Vector2(-14f, -14f - rowIndex * 96f), new Color(0.18f, 0.19f, 0.22f, 1f));
                var ingredients = string.Empty;
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient?.item != null)
                    {
                        ingredients += $"{ingredient.item.DisplayName} {inventoryService.GetQuantity(ingredient.item.Id)}/{ingredient.quantity}  ";
                    }
                }

                var label = RuntimeUiFactory.CreateText(row, "Label", $"{recipe.DisplayName}\n{ingredients}", 18, TextAnchor.MiddleLeft, Color.white);
                RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.70f, 1f), Vector2.zero, Vector2.zero);

                var recipeId = recipe.Id;
                var craftButton = RuntimeUiFactory.CreateButton(row, "Craft Button", "Craft", new Color(0.23f, 0.43f, 0.70f, 1f));
                RuntimeUiFactory.SetRect(craftButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.22f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
                craftButton.interactable = craftingService.CanCraft(recipeId);
                craftButton.onClick.AddListener(() =>
                {
                    if (craftingService.Craft(recipeId))
                    {
                        runtime.Save();
                        Refresh();
                    }
                });
                rowIndex++;
            }

            RuntimeUiFactory.SetScrollContentHeight(recipeList, 20f + rowIndex * 96f);
        }

        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (inventoryService != null)
            {
                inventoryService.Changed -= Refresh;
            }

            if (craftingService != null)
            {
                craftingService.Changed -= Refresh;
            }
        }
    }
}
