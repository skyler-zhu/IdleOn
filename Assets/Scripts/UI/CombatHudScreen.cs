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
            RuntimeUiFactory.SetRect(status.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.39f, 1f), Vector2.zero, Vector2.zero);

            var inventoryPanel = new InventoryEquipmentPanel(runtime, canvas.transform);
            var talentPanel = new TalentTreePanel(runtime, canvas.transform);
            var skillPanel = new SkillTreePanel(runtime, canvas.transform);
            _ = new QuestTrackerPanel(runtime, canvas.transform, false);

            var inventoryButton = RuntimeUiFactory.CreateButton(topBar, "Inventory Button", "Inventory", new Color(0.26f, 0.30f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(0.40f, 0.20f), new Vector2(0.50f, 0.80f), Vector2.zero, Vector2.zero);
            inventoryButton.onClick.AddListener(inventoryPanel.Toggle);

            var talentButton = RuntimeUiFactory.CreateButton(topBar, "Talents Button", "Talents", new Color(0.30f, 0.28f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(talentButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.20f), new Vector2(0.60f, 0.80f), Vector2.zero, Vector2.zero);
            talentButton.onClick.AddListener(talentPanel.Toggle);

            var skillButton = RuntimeUiFactory.CreateButton(topBar, "Skills Button", "Skills", new Color(0.24f, 0.38f, 0.32f, 1f));
            RuntimeUiFactory.SetRect(skillButton.GetComponent<RectTransform>(), new Vector2(0.61f, 0.20f), new Vector2(0.70f, 0.80f), Vector2.zero, Vector2.zero);
            skillButton.onClick.AddListener(skillPanel.Toggle);

            var charactersButton = RuntimeUiFactory.CreateButton(topBar, "Characters Button", "Chars", new Color(0.30f, 0.28f, 0.46f, 1f));
            RuntimeUiFactory.SetRect(charactersButton.GetComponent<RectTransform>(), new Vector2(0.71f, 0.20f), new Vector2(0.79f, 0.80f), Vector2.zero, Vector2.zero);
            charactersButton.onClick.AddListener(runtime.ReturnToCharacterSelect);

            var autoButton = RuntimeUiFactory.CreateButton(topBar, "Auto Manual Button", "Auto", new Color(0.22f, 0.36f, 0.48f, 1f));
            RuntimeUiFactory.SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.20f), new Vector2(0.88f, 0.80f), Vector2.zero, Vector2.zero);
            autoButton.onClick.AddListener(() => onAutoModeToggleRequested());

            var actionButton = RuntimeUiFactory.CreateButton(topBar, "Manual Action Button", "Action (J)", new Color(0.48f, 0.34f, 0.18f, 1f));
            RuntimeUiFactory.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.89f, 0.20f), new Vector2(0.98f, 0.80f), Vector2.zero, Vector2.zero);
            actionButton.onClick.AddListener(() => onManualActionRequested());

            var enemyPanel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Enemy Panel",
                new Vector2(0.35f, 0.70f),
                new Vector2(0.65f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                new Color(0f, 0f, 0f, 0f));

            var enemyNameText = RuntimeUiFactory.CreateText(enemyPanel, "Enemy Name", string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
            RuntimeUiFactory.SetRect(enemyNameText.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var hpBackground = RuntimeUiFactory.CreatePanel(enemyPanel, "Enemy HP Bar", new Vector2(0.14f, 0.18f), new Vector2(0.86f, 0.48f), Vector2.zero, Vector2.zero, new Color(0.30f, 0.08f, 0.08f, 0.58f));
            var hpFill = RuntimeUiFactory.CreatePanel(hpBackground, "Enemy HP Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.72f, 0.20f, 0.20f, 1f));
            var hpText = RuntimeUiFactory.CreateText(hpBackground, "Enemy HP Text", string.Empty, 18, TextAnchor.MiddleCenter, Color.white);
            RuntimeUiFactory.Stretch(hpText.rectTransform, Vector2.zero, Vector2.zero);

            var logText = RuntimeUiFactory.CreateText(canvas.transform, "Combat Log", string.Empty, 14, TextAnchor.LowerLeft, new Color(0.88f, 0.92f, 0.96f, 0.92f));
            RuntimeUiFactory.SetRect(logText.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.42f, 0.17f), Vector2.zero, Vector2.zero);

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
                if (status == null || enemyNameText == null || hpFill == null || hpText == null)
                {
                    return;
                }

                var character = state.Character;
                var characterName = character != null ? character.DisplayName : state.SaveData.characterName;
                var resting = combatService.IsResting ? "    Incapacitated" : string.Empty;
                var activity = state.SaveData.currentActivity;
                var gatheringHint = activity == IdleOnLike.Data.ZoneActivity.Chopping.ToString() && !canStartChopping() ? "    Move near tree" : string.Empty;
                var controlMode = isAutoMode() ? "Auto" : "Manual";
                var manualHint = isAutoMode() ? string.Empty : "    A/D Move    Space Jump";
                status.text = $"{characterName}    {controlMode}    Last: {activity}    Lv. {state.SaveData.level}    HP: {state.SaveData.currentHp}/{combatService.MaxPlayerHp}    XP: {state.SaveData.experience}/{combatService.ExperienceRequired}    Coins: {state.Coins}    DMG: {combatService.PlayerDamage}    M: Map    U: Quests{resting}{gatheringHint}{manualHint}";
                autoButton.GetComponentInChildren<Text>().text = isAutoMode() ? "Auto" : "Manual";
                actionButton.GetComponentInChildren<Text>().text = "Action (J)";
                actionButton.interactable = canManualAction();

                if (combatService.CurrentTarget == null)
                {
                    enemyNameText.text = "No Active Target";
                    hpFill.anchorMax = new Vector2(0f, 1f);
                    hpText.text = string.Empty;
                    return;
                }

                var target = combatService.CurrentTarget;
                enemyNameText.text = target.enemyDefinition.DisplayName;
                var maxHp = Mathf.Max(1, target.enemyDefinition.MaxHp);
                hpFill.anchorMax = new Vector2(Mathf.Clamp01(target.currentHp / (float)maxHp), 1f);
                hpText.text = $"{target.currentHp}/{maxHp}";
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
