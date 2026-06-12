using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Equipment;
using IdleOnLike.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnLike.UI
{
    public sealed class InventoryEquipmentPanel
    {
        private readonly GameRuntime runtime;
        private readonly InventoryService inventoryService;
        private readonly EquipmentService equipmentService;
        private readonly RectTransform root;
        private readonly RectTransform inventoryList;
        private readonly RectTransform equipmentList;
        private bool isDisposed;

        public InventoryEquipmentPanel(GameRuntime runtime, Transform parent)
        {
            this.runtime = runtime;
            inventoryService = runtime.InventoryService;
            equipmentService = runtime.EquipmentService;

            root = RuntimeUiFactory.CreatePanel(
                parent,
                "Inventory Equipment Panel",
                new Vector2(0.08f, 0.12f),
                new Vector2(0.92f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.06f, 0.07f, 0.08f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Inventory & Equipment", 28, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.70f, 0.98f), Vector2.zero, Vector2.zero);

            var closeButton = RuntimeUiFactory.CreateButton(root, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            RuntimeUiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.91f), new Vector2(0.97f, 0.975f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(Toggle);

            var inventoryHeader = RuntimeUiFactory.CreateText(root, "Inventory Header", "Inventory", 23, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(inventoryHeader.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.48f, 0.89f), Vector2.zero, Vector2.zero);

            var equipmentHeader = RuntimeUiFactory.CreateText(root, "Equipment Header", "Equipment", 23, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(equipmentHeader.rectTransform, new Vector2(0.56f, 0.82f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero);

            inventoryList = RuntimeUiFactory.CreatePanel(root, "Inventory List", new Vector2(0.04f, 0.06f), new Vector2(0.50f, 0.82f), Vector2.zero, Vector2.zero, new Color(0.11f, 0.12f, 0.14f, 1f));
            equipmentList = RuntimeUiFactory.CreatePanel(root, "Equipment List", new Vector2(0.54f, 0.06f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero, new Color(0.11f, 0.12f, 0.14f, 1f));

            inventoryService.Changed += Refresh;
            equipmentService.Changed += Refresh;

            var lifetime = parent.GetComponentInParent<RuntimeUiLifetime>();
            if (lifetime != null)
            {
                lifetime.Register(Dispose);
            }

            root.gameObject.SetActive(false);
            Refresh();
        }

        public void Toggle()
        {
            root.gameObject.SetActive(!root.gameObject.activeSelf);
            if (root.gameObject.activeSelf)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (isDisposed || runtime == null || runtime.State == null || root == null || inventoryList == null || equipmentList == null)
            {
                Dispose();
                return;
            }

            ClearList(inventoryList);
            ClearList(equipmentList);
            BuildInventory();
            BuildEquipment();
        }

        private void BuildInventory()
        {
            var saveData = runtime.State.SaveData;
            saveData.EnsureCollections();

            if (saveData.inventory.Count == 0)
            {
                var empty = RuntimeUiFactory.CreateText(inventoryList, "Empty Inventory", "No items yet. Go Forest and fight for drops.", 18, TextAnchor.UpperLeft, new Color(0.78f, 0.82f, 0.88f));
                RuntimeUiFactory.Stretch(empty.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -18f));
                return;
            }

            for (var i = 0; i < saveData.inventory.Count; i++)
            {
                var stack = saveData.inventory[i];
                var item = runtime.Catalog.FindItem(stack.itemId);
                if (item == null)
                {
                    continue;
                }

                var row = CreateRow(inventoryList, $"Item Row {i}", i, 58f);
                AddItemIcon(row, item);

                var label = RuntimeUiFactory.CreateText(row, "Item Label", $"{item.DisplayName} x{stack.quantity}", 18, TextAnchor.MiddleLeft, Color.white);
                RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.16f, 0f), new Vector2(item.IsEquipment ? 0.66f : 0.96f, 1f), Vector2.zero, Vector2.zero);

                if (item.IsEquipment)
                {
                    var itemId = item.Id;
                    var equipButton = RuntimeUiFactory.CreateButton(row, "Equip Button", "Equip", new Color(0.22f, 0.42f, 0.72f, 1f));
                    RuntimeUiFactory.SetRect(equipButton.GetComponent<RectTransform>(), new Vector2(0.70f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);
                    equipButton.onClick.AddListener(() =>
                    {
                        if (equipmentService.Equip(itemId))
                        {
                            runtime.Save();
                            Refresh();
                        }
                    });
                }
            }
        }

        private void BuildEquipment()
        {
            var slots = new[]
            {
                EquipmentSlot.Weapon,
                EquipmentSlot.Helmet,
                EquipmentSlot.Chest,
                EquipmentSlot.Boots,
                EquipmentSlot.Pendant
            };

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var item = equipmentService.GetEquipped(slot);
                var row = CreateRow(equipmentList, $"{slot} Row", i, 58f);

                var label = RuntimeUiFactory.CreateText(row, "Slot Label", $"{slot}: {(item != null ? item.DisplayName : "Empty")}", 18, TextAnchor.MiddleLeft, Color.white);
                RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(item != null ? 0.68f : 0.96f, 1f), Vector2.zero, Vector2.zero);

                if (item != null)
                {
                    var equipmentSlot = slot;
                    var unequipButton = RuntimeUiFactory.CreateButton(row, "Unequip Button", "Unequip", new Color(0.42f, 0.28f, 0.22f, 1f));
                    RuntimeUiFactory.SetRect(unequipButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);
                    unequipButton.onClick.AddListener(() =>
                    {
                        if (equipmentService.Unequip(equipmentSlot))
                        {
                            runtime.Save();
                            Refresh();
                        }
                    });
                }
            }

            var attackBonus = RuntimeUiFactory.CreateText(equipmentList, "Attack Bonus", $"Equipment Attack: +{equipmentService.GetAttackBonus()}", 18, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 1f));
            RuntimeUiFactory.SetRect(attackBonus.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
        }

        private static RectTransform CreateRow(Transform parent, string name, int index, float rowHeight)
        {
            var top = -14f - index * (rowHeight + 8f);
            var row = RuntimeUiFactory.CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, top - rowHeight), new Vector2(-14f, top), new Color(0.17f, 0.18f, 0.21f, 1f));
            return row;
        }

        private static void AddItemIcon(Transform row, ItemDefinition item)
        {
            if (item.Icon == null)
            {
                return;
            }

            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row, false);
            var icon = iconObject.AddComponent<Image>();
            icon.sprite = item.Icon;
            icon.preserveAspect = true;
            RuntimeUiFactory.SetRect(icon.rectTransform, new Vector2(0.03f, 0.16f), new Vector2(0.13f, 0.84f), Vector2.zero, Vector2.zero);
        }

        private static void ClearList(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

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

            if (equipmentService != null)
            {
                equipmentService.Changed -= Refresh;
            }
        }
    }
}
