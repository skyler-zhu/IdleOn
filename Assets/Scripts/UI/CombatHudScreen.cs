using System;
using System.Collections.Generic;
using IdleOnLike.Combat;
using IdleOnLike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnLike.UI
{
    public static class CombatHudScreen
    {
        private const int MaxLogLines = 4;

        public static void Build(GameRuntime runtime, CombatService combatService, Func<bool> canStartChopping, Func<bool> isAutoMode, Action onAutoModeToggleRequested, Func<bool> canManualAction, Action onManualActionRequested)
        {
            var state = runtime.State;
            var inventoryService = runtime.InventoryService;
            var equipmentService = runtime.EquipmentService;
            var gatheringService = runtime.GatheringService;
            var logLines = new List<string>();
            var canvas = RuntimeUiFactory.CreateCanvas("Combat HUD");
            var lifetime = canvas.GetComponent<RuntimeUiLifetime>();

            var topBar = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Top Bar",
                new Vector2(0f, 0.88f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.09f, 0.11f, 0.92f));

            var status = RuntimeUiFactory.CreateText(topBar, "Status", string.Empty, 19, TextAnchor.MiddleLeft, Color.white);
            RuntimeUiFactory.SetRect(status.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.50f, 1f), Vector2.zero, Vector2.zero);

            var inventoryPanel = new InventoryEquipmentPanel(runtime, canvas.transform);
            _ = new QuestTrackerPanel(runtime, canvas.transform, false);

            var inventoryButton = RuntimeUiFactory.CreateButton(topBar, "Inventory Button", "Inventory", new Color(0.26f, 0.30f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.20f), new Vector2(0.61f, 0.80f), Vector2.zero, Vector2.zero);
            inventoryButton.onClick.AddListener(inventoryPanel.Toggle);

            var autoButton = RuntimeUiFactory.CreateButton(topBar, "Auto Manual Button", "Auto", new Color(0.22f, 0.36f, 0.48f, 1f));
            RuntimeUiFactory.SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(0.62f, 0.20f), new Vector2(0.73f, 0.80f), Vector2.zero, Vector2.zero);
            autoButton.onClick.AddListener(() => onAutoModeToggleRequested());

            var actionButton = RuntimeUiFactory.CreateButton(topBar, "Manual Action Button", "Action (J)", new Color(0.48f, 0.34f, 0.18f, 1f));
            RuntimeUiFactory.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.20f), new Vector2(0.84f, 0.80f), Vector2.zero, Vector2.zero);
            actionButton.onClick.AddListener(() => onManualActionRequested());

            var offlineButton = RuntimeUiFactory.CreateButton(topBar, "Offline Button", "Sim 1h", new Color(0.22f, 0.35f, 0.42f, 1f));
            RuntimeUiFactory.SetRect(offlineButton.GetComponent<RectTransform>(), new Vector2(0.85f, 0.20f), new Vector2(0.98f, 0.80f), Vector2.zero, Vector2.zero);
            offlineButton.onClick.AddListener(() => OfflineGainsPanel.Show(runtime.SimulateOfflineHour()));

            var enemyPanel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Enemy Panel",
                new Vector2(0.32f, 0.70f),
                new Vector2(0.62f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.16f, 0.14f, 0.12f, 0.88f));

            var enemyText = RuntimeUiFactory.CreateText(enemyPanel, "Enemy Text", string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
            RuntimeUiFactory.Stretch(enemyText.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -18f));

            var logPanel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Combat Log Panel",
                new Vector2(0.24f, 0.02f),
                new Vector2(0.76f, 0.16f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.08f, 0.09f, 0.88f));

            var logText = RuntimeUiFactory.CreateText(logPanel, "Combat Log", string.Empty, 14, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.96f));
            RuntimeUiFactory.Stretch(logText.rectTransform, new Vector2(18f, 14f), new Vector2(-18f, -14f));

            combatService.Changed += Refresh;
            combatService.LogAdded += AddLog;
            gatheringService.LogAdded += AddLog;
            inventoryService.Changed += Refresh;
            equipmentService.Changed += Refresh;
            gatheringService.Changed += Refresh;
            lifetime?.Register(() =>
            {
                combatService.Changed -= Refresh;
                combatService.LogAdded -= AddLog;
                gatheringService.LogAdded -= AddLog;
                inventoryService.Changed -= Refresh;
                equipmentService.Changed -= Refresh;
                gatheringService.Changed -= Refresh;
            });

            Refresh();

            void Refresh()
            {
                if (status == null || enemyText == null)
                {
                    return;
                }

                var character = state.Character;
                var characterName = character != null ? character.DisplayName : state.SaveData.characterName;
                var resting = combatService.IsResting ? "    Resting" : string.Empty;
                var jumping = combatService.IsJumping ? "    Jumping" : string.Empty;
                var activity = state.SaveData.currentActivity;
                var gatheringHint = activity == IdleOnLike.Data.ZoneActivity.Chopping.ToString() && !canStartChopping() ? "    Move near tree" : string.Empty;
                var controlMode = isAutoMode() ? "Auto" : "Manual";
                var manualHint = isAutoMode() ? string.Empty : "    A/D Move    Space Jump";
                status.text = $"{characterName}    {controlMode}    Last: {activity}    Lv. {state.SaveData.level}    HP: {state.SaveData.currentHp}/{combatService.MaxPlayerHp}    XP: {state.SaveData.experience}/{combatService.ExperienceRequired}    Coins: {state.Coins}    DMG: {combatService.PlayerDamage}    M: Map{resting}{jumping}{gatheringHint}{manualHint}";
                autoButton.GetComponentInChildren<Text>().text = isAutoMode() ? "Auto" : "Manual";
                actionButton.GetComponentInChildren<Text>().text = "Action (J)";
                actionButton.interactable = canManualAction();

                if (combatService.CurrentTarget == null)
                {
                    enemyText.text = "Looking for enemies...";
                    return;
                }

                enemyText.text = $"{combatService.CurrentTarget.enemyDefinition.DisplayName}\nHP: {combatService.CurrentTarget.currentHp}/{combatService.CurrentTarget.enemyDefinition.MaxHp}";
            }

            void AddLog(string message)
            {
                if (logText == null)
                {
                    return;
                }

                logLines.Insert(0, message);
                if (logLines.Count > MaxLogLines)
                {
                    logLines.RemoveAt(logLines.Count - 1);
                }

                logText.text = string.Join("\n", logLines);
            }
        }
    }
}
