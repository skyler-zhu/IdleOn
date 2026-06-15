using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Inventory;

namespace IdleOnLike.Shop
{
    public sealed class ShopService
    {
        private const int BuyMarkup = 2;

        private readonly GameState state;
        private readonly InventoryService inventoryService;

        public ShopService(GameState state, InventoryService inventoryService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
        }

        public event Action Changed;
        public event Action<string> LogAdded;

        public int GetBuyPrice(ItemDefinition item)
        {
            if (item == null)
            {
                return 0;
            }

            return Math.Max(1, item.SellValue * BuyMarkup);
        }

        public bool Buy(string itemId, int quantity = 1)
        {
            var item = state.Catalog.FindItem(itemId);
            if (item == null || quantity <= 0)
            {
                return false;
            }

            var price = GetBuyPrice(item) * quantity;
            if (state.Coins < price)
            {
                LogAdded?.Invoke("Insufficient coins.");
                return false;
            }

            state.Coins -= price;
            inventoryService.AddItem(item.Id, quantity);
            LogAdded?.Invoke($"Bought {item.DisplayName} x{quantity}.");
            Changed?.Invoke();
            return true;
        }

        public bool Sell(string itemId, int quantity = 1)
        {
            var item = state.Catalog.FindItem(itemId);
            if (item == null || quantity <= 0 || item.SellValue <= 0)
            {
                return false;
            }

            if (!inventoryService.RemoveItem(item.Id, quantity))
            {
                return false;
            }

            state.Coins += item.SellValue * quantity;
            LogAdded?.Invoke($"Sold {item.DisplayName} x{quantity}.");
            Changed?.Invoke();
            return true;
        }
    }
}
