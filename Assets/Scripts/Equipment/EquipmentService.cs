using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Inventory;
using IdleOnLike.Save;

namespace IdleOnLike.Equipment
{
    public sealed class EquipmentService
    {
        private readonly GameState state;
        private readonly InventoryService inventoryService;

        public EquipmentService(GameState state, InventoryService inventoryService)
        {
            this.state = state;
            this.inventoryService = inventoryService;
            this.state.SaveData.EnsureCollections();
        }

        public event Action Changed;
        public event Action<QuestObjectiveType, string, int> ItemEquipped;

        public bool Equip(string itemId)
        {
            var item = state.Catalog.FindItem(itemId);
            if (item == null || !item.IsEquipment)
            {
                return false;
            }

            if (!inventoryService.RemoveItem(itemId, 1))
            {
                return false;
            }

            var current = GetEquipped(item.EquipmentSlot);
            if (current != null)
            {
                inventoryService.AddItem(current.Id, 1);
            }

            var slot = state.SaveData.equipment.Find(entry => entry.slot == item.EquipmentSlot);
            if (slot == null)
            {
                state.SaveData.equipment.Add(new SaveEquipmentSlot
                {
                    slot = item.EquipmentSlot,
                    itemId = item.Id
                });
            }
            else
            {
                slot.itemId = item.Id;
            }

            ItemEquipped?.Invoke(QuestObjectiveType.EquipItem, item.Id, 1);
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(EquipmentSlot equipmentSlot)
        {
            var slot = state.SaveData.equipment.Find(entry => entry.slot == equipmentSlot);
            if (slot == null || string.IsNullOrEmpty(slot.itemId))
            {
                return false;
            }

            inventoryService.AddItem(slot.itemId, 1);
            slot.itemId = string.Empty;
            Changed?.Invoke();
            return true;
        }

        public ItemDefinition GetEquipped(EquipmentSlot equipmentSlot)
        {
            var slot = state.SaveData.equipment.Find(entry => entry.slot == equipmentSlot);
            if (slot == null || string.IsNullOrEmpty(slot.itemId))
            {
                return null;
            }

            return state.Catalog.FindItem(slot.itemId);
        }

        public int GetAttackBonus()
        {
            var attack = 0;
            foreach (var slot in state.SaveData.equipment)
            {
                var item = state.Catalog.FindItem(slot.itemId);
                if (item != null)
                {
                    attack += item.EquipStats.attack;
                }
            }

            return attack;
        }
    }
}
