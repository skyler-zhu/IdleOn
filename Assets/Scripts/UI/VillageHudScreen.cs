using System;
using IdleOnLike.Core;
using UnityEngine;

namespace IdleOnLike.UI
{
    public static class VillageHudScreen
    {
        public static void Build(GameRuntime runtime, Action onGoForestRequested, Action onNewGameRequested)
        {
            var state = runtime.State;
            var canvas = RuntimeUiFactory.CreateCanvas("Village HUD");
            var topBar = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Top Bar",
                new Vector2(0f, 0.88f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.09f, 0.11f, 0.92f));

            var zone = state.CurrentZone;
            var character = state.Character;
            var characterName = character != null ? character.DisplayName : state.SaveData.characterName;
            var zoneName = zone != null ? zone.DisplayName : state.SaveData.currentZoneId;

            var status = RuntimeUiFactory.CreateText(
                topBar,
                "Status",
                $"{characterName}    Lv. {state.SaveData.level}    Coins: {state.SaveData.coins}    Zone: {zoneName}",
                24,
                TextAnchor.MiddleLeft,
                Color.white);
            RuntimeUiFactory.SetRect(status.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.49f, 1f), Vector2.zero, Vector2.zero);

            var inventoryPanel = new InventoryEquipmentPanel(runtime, canvas.transform);
            _ = new QuestTrackerPanel(runtime, canvas.transform, true);

            var inventoryButton = RuntimeUiFactory.CreateButton(topBar, "Inventory Button", "Inventory", new Color(0.26f, 0.30f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.20f), new Vector2(0.63f, 0.80f), Vector2.zero, Vector2.zero);
            inventoryButton.onClick.AddListener(inventoryPanel.Toggle);

            var forestButton = RuntimeUiFactory.CreateButton(topBar, "Go Forest Button", "Go Forest", new Color(0.18f, 0.42f, 0.24f, 1f));
            RuntimeUiFactory.SetRect(forestButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0.20f), new Vector2(0.79f, 0.80f), Vector2.zero, Vector2.zero);
            forestButton.onClick.AddListener(() => onGoForestRequested());

            var newGameButton = RuntimeUiFactory.CreateButton(topBar, "New Game Button", "New Save", new Color(0.42f, 0.18f, 0.18f, 1f));
            RuntimeUiFactory.SetRect(newGameButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.20f), new Vector2(0.97f, 0.80f), Vector2.zero, Vector2.zero);
            newGameButton.onClick.AddListener(() => onNewGameRequested());

            var centerPanel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Demo Panel",
                new Vector2(0.28f, 0.34f),
                new Vector2(0.72f, 0.66f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.13f, 0.20f, 0.16f, 0.82f));

            var message = RuntimeUiFactory.CreateText(
                centerPanel,
                "Message",
                "Village loaded from Boot. Save data is active.\nUse Go Forest to test the idle combat loop.",
                24,
                TextAnchor.MiddleCenter,
                Color.white);
            RuntimeUiFactory.Stretch(message.rectTransform, new Vector2(24f, 24f), new Vector2(-24f, -24f));
        }
    }
}
