using System;
using IdleOnLike.Core;
using UnityEngine;

namespace IdleOnLike.UI
{
    public static class VillageHudScreen
    {
        public static void Build(GameRuntime runtime, Action onGoForestRequested, Action onMineCaveRequested, Action onCharacterSelectRequested, Action onNewGameRequested)
        {
            var state = runtime.State;
            var canvas = RuntimeUiFactory.CreateCanvas("Village HUD");
            var lifetime = canvas.GetComponent<RuntimeUiLifetime>();
            var topBar = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Top Bar",
                new Vector2(0f, 0.92f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.09f, 0.11f, 0.92f));

            var status = RuntimeUiFactory.CreateText(
                topBar,
                "Status",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                Color.white);
            RuntimeUiFactory.SetRect(status.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.34f, 1f), Vector2.zero, Vector2.zero);

            var inventoryPanel = new InventoryEquipmentPanel(runtime, canvas.transform);
            var craftingPanel = new CraftingPanel(runtime, canvas.transform);
            var inventoryService = runtime.InventoryService;
            var equipmentService = runtime.EquipmentService;
            var questService = runtime.QuestService;
            var craftingService = runtime.CraftingService;

            var inventoryButton = RuntimeUiFactory.CreateButton(topBar, "Inventory Button", "Inventory", new Color(0.26f, 0.30f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.16f), new Vector2(0.45f, 0.84f), Vector2.zero, Vector2.zero);
            inventoryButton.onClick.AddListener(inventoryPanel.Toggle);

            var craftingButton = RuntimeUiFactory.CreateButton(topBar, "Crafting Button", "Crafting", new Color(0.40f, 0.30f, 0.18f, 1f));
            RuntimeUiFactory.SetRect(craftingButton.GetComponent<RectTransform>(), new Vector2(0.46f, 0.16f), new Vector2(0.55f, 0.84f), Vector2.zero, Vector2.zero);
            craftingButton.onClick.AddListener(craftingPanel.Toggle);

            var offlineButton = RuntimeUiFactory.CreateButton(topBar, "Offline Button", "Sim 1h", new Color(0.22f, 0.35f, 0.42f, 1f));
            RuntimeUiFactory.SetRect(offlineButton.GetComponent<RectTransform>(), new Vector2(0.56f, 0.16f), new Vector2(0.63f, 0.84f), Vector2.zero, Vector2.zero);
            offlineButton.onClick.AddListener(() => OfflineGainsPanel.Show(runtime.SimulateOfflineHour()));

            var forestButton = RuntimeUiFactory.CreateButton(topBar, "Go Forest Button", "Forest", new Color(0.18f, 0.42f, 0.24f, 1f));
            RuntimeUiFactory.SetRect(forestButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0.16f), new Vector2(0.71f, 0.84f), Vector2.zero, Vector2.zero);
            forestButton.onClick.AddListener(() => onGoForestRequested());

            var mineButton = RuntimeUiFactory.CreateButton(topBar, "Mine Cave Button", "Mine", new Color(0.34f, 0.34f, 0.42f, 1f));
            RuntimeUiFactory.SetRect(mineButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.16f), new Vector2(0.79f, 0.84f), Vector2.zero, Vector2.zero);
            mineButton.onClick.AddListener(() => onMineCaveRequested());

            var charactersButton = RuntimeUiFactory.CreateButton(topBar, "Characters Button", "Characters", new Color(0.30f, 0.28f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(charactersButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.16f), new Vector2(0.89f, 0.84f), Vector2.zero, Vector2.zero);
            charactersButton.onClick.AddListener(() => onCharacterSelectRequested());

            var newGameButton = RuntimeUiFactory.CreateButton(topBar, "New Game Button", "New Save", new Color(0.42f, 0.18f, 0.18f, 1f));
            RuntimeUiFactory.SetRect(newGameButton.GetComponent<RectTransform>(), new Vector2(0.90f, 0.16f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);
            newGameButton.onClick.AddListener(() => onNewGameRequested());

            inventoryService.Changed += Refresh;
            equipmentService.Changed += Refresh;
            questService.Changed += Refresh;
            craftingService.Changed += Refresh;
            lifetime?.Register(() =>
            {
                inventoryService.Changed -= Refresh;
                equipmentService.Changed -= Refresh;
                questService.Changed -= Refresh;
                craftingService.Changed -= Refresh;
            });

            Refresh();

            void Refresh()
            {
                if (status == null || runtime.State == null)
                {
                    return;
                }

                var zone = state.CurrentZone;
                var character = state.Character;
                var characterName = character != null ? character.DisplayName : state.SaveData.characterName;
                var zoneName = zone != null ? zone.DisplayName : state.SaveData.currentZoneId;
                status.text = $"{characterName}    Lv. {state.SaveData.level}    Coins: {state.SaveData.coins}    Zone: {zoneName}";
                var mineUnlocked = questService.IsQuestCompleted("learn_to_chop");
                mineButton.interactable = mineUnlocked;
                mineButton.GetComponentInChildren<UnityEngine.UI.Text>().text = mineUnlocked ? "Mine" : "Mine Locked";
            }
        }
    }
}
