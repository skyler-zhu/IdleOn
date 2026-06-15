using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Inventory;
using IdleOnLike.Shop;
using UnityEngine;

namespace IdleOnLike.UI
{
    public sealed class ShopPanel
    {
        private static readonly string[] BuyItemIds =
        {
            "duct_tape",
            "scrap_metal"
        };

        private readonly GameRuntime runtime;
        private readonly InventoryService inventoryService;
        private readonly ShopService shopService;
        private readonly RectTransform root;
        private readonly RectTransform buyList;
        private readonly RectTransform sellList;
        private bool isDisposed;

        public ShopPanel(GameRuntime runtime, Transform parent)
        {
            this.runtime = runtime;
            inventoryService = runtime.InventoryService;
            shopService = runtime.ShopService;
            root = RuntimeUiFactory.CreatePanel(parent, "Shop Panel", new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.80f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.08f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Merchant", 28, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.70f, 0.98f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.90f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(Toggle);

            var buyHeader = RuntimeUiFactory.CreateText(root, "Buy Header", "Buy", 22, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(buyHeader.rectTransform, new Vector2(0.06f, 0.80f), new Vector2(0.46f, 0.88f), Vector2.zero, Vector2.zero);
            var sellHeader = RuntimeUiFactory.CreateText(root, "Sell Header", "Sell", 22, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(sellHeader.rectTransform, new Vector2(0.54f, 0.80f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);

            buyList = RuntimeUiFactory.CreateScrollContent(root, "Buy List", new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.80f), new Color(0.11f, 0.12f, 0.14f, 1f));
            sellList = RuntimeUiFactory.CreateScrollContent(root, "Sell List", new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.80f), new Color(0.11f, 0.12f, 0.14f, 1f));

            inventoryService.Changed += Refresh;
            shopService.Changed += Refresh;
            parent.GetComponentInParent<RuntimeUiLifetime>()?.Register(Dispose);

            root.gameObject.SetActive(false);
            RuntimeUiOverlayRegistry.Register(root, Refresh);
            Refresh();
        }

        public void Toggle()
        {
            if (isDisposed || root == null)
            {
                return;
            }

            RuntimeUiOverlayRegistry.Toggle(root, Refresh);
        }

        private void Refresh()
        {
            if (isDisposed || root == null || runtime == null || runtime.State == null || buyList == null || sellList == null)
            {
                Dispose();
                return;
            }

            Clear(buyList);
            Clear(sellList);
            for (var i = 0; i < BuyItemIds.Length; i++)
            {
                BuildBuyRow(runtime.Catalog.FindItem(BuyItemIds[i]), i);
            }

            RuntimeUiFactory.SetScrollContentHeight(buyList, 24f + BuyItemIds.Length * 76f);
            var sellRows = BuildSellRows();
            RuntimeUiFactory.SetScrollContentHeight(sellList, 24f + sellRows * 76f);
        }

        private void BuildBuyRow(ItemDefinition item, int index)
        {
            if (item == null)
            {
                return;
            }

            var row = CreateRow(buyList, $"Buy {item.Id}", index);
            var price = shopService.GetBuyPrice(item);
            var label = RuntimeUiFactory.CreateText(row, "Label", $"{item.DisplayName}\n{price} coins", 18, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.68f, 1f), Vector2.zero, Vector2.zero);

            var button = RuntimeUiFactory.CreateButton(row, "Buy Button", "Buy", new Color(0.22f, 0.42f, 0.72f, 1f));
            RuntimeUiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.72f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
            button.interactable = runtime.State.Coins >= price;
            button.onClick.AddListener(() =>
            {
                if (shopService.Buy(item.Id))
                {
                    runtime.Save();
                    Refresh();
                }
            });
        }

        private int BuildSellRows()
        {
            var index = 0;
            runtime.State.AccountData.EnsureCollections();
            foreach (var stack in runtime.State.AccountData.sharedInventory)
            {
                var item = runtime.Catalog.FindItem(stack.itemId);
                if (item == null || item.SellValue <= 0)
                {
                    continue;
                }

                var row = CreateRow(sellList, $"Sell {item.Id}", index++);
                var label = RuntimeUiFactory.CreateText(row, "Label", $"{item.DisplayName} x{stack.quantity}\nSell: {item.SellValue}", 18, TextAnchor.MiddleLeft, Color.white);
                RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.68f, 1f), Vector2.zero, Vector2.zero);

                var itemId = item.Id;
                var button = RuntimeUiFactory.CreateButton(row, "Sell Button", "Sell", new Color(0.42f, 0.30f, 0.18f, 1f));
                RuntimeUiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.72f, 0.20f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
                button.onClick.AddListener(() =>
                {
                    if (shopService.Sell(itemId))
                    {
                        runtime.Save();
                        Refresh();
                    }
                });
            }

            return index;
        }

        private static RectTransform CreateRow(Transform parent, string name, int index)
        {
            var top = -14f - index * 76f;
            return RuntimeUiFactory.CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, top - 66f), new Vector2(-12f, top), new Color(0.17f, 0.18f, 0.21f, 1f));
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

            if (shopService != null)
            {
                shopService.Changed -= Refresh;
            }
        }
    }
}
