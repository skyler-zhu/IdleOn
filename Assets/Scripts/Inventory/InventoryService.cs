using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Save;

namespace IdleOnLike.Inventory
{
    public sealed class InventoryService
    {
        private readonly GameState state;

        public InventoryService(GameState state)
        {
            this.state = state;
            this.state.SaveData.EnsureCollections();
        }

        public event Action Changed;
        public event Action<QuestObjectiveType, string, int> ItemAdded;

        public void AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return;
            }

            var existing = state.SaveData.inventory.Find(stack => stack.itemId == itemId);
            if (existing != null)
            {
                existing.quantity += quantity;
            }
            else
            {
                state.SaveData.inventory.Add(new SaveItemStack
                {
                    itemId = itemId,
                    quantity = quantity
                });
            }

            ItemAdded?.Invoke(QuestObjectiveType.CollectItem, itemId, quantity);
            Changed?.Invoke();
        }

        public bool RemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return false;
            }

            var existing = state.SaveData.inventory.Find(stack => stack.itemId == itemId);
            if (existing == null || existing.quantity < quantity)
            {
                return false;
            }

            existing.quantity -= quantity;
            if (existing.quantity <= 0)
            {
                state.SaveData.inventory.Remove(existing);
            }

            Changed?.Invoke();
            return true;
        }

        public int GetQuantity(string itemId)
        {
            var existing = state.SaveData.inventory.Find(stack => stack.itemId == itemId);
            return existing != null ? existing.quantity : 0;
        }

        public ItemDefinition GetItem(string itemId)
        {
            return state.Catalog.FindItem(itemId);
        }
    }
}
