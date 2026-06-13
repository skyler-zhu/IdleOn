using System;
using IdleOnLike.Data;
using IdleOnLike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnLike.UI
{
    public static class CharacterSelectScreen
    {
        public static void Build(GameCatalog catalog, AccountSaveData saveData, Action<CharacterDefinition> onCharacterSelected)
        {
            saveData?.EnsureCollections();
            var canvas = RuntimeUiFactory.CreateCanvas("Character Select UI");
            var root = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "Root",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.08f, 0.11f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Choose Your Idler", 38, TextAnchor.MiddleCenter, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.15f, 0.82f), new Vector2(0.85f, 0.95f), Vector2.zero, Vector2.zero);

            var subtitle = RuntimeUiFactory.CreateText(root, "Subtitle", "Complete Learn to Chop to unlock the second character.", 20, TextAnchor.MiddleCenter, new Color(0.75f, 0.80f, 0.88f));
            RuntimeUiFactory.SetRect(subtitle.rectTransform, new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.82f), Vector2.zero, Vector2.zero);

            var characters = catalog.PlayableCharacters;
            var count = Mathf.Max(1, characters.Count);
            var cardWidth = 220f;
            var gap = 32f;
            var totalWidth = count * cardWidth + (count - 1) * gap;
            var startX = -totalWidth * 0.5f;

            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                var unlocked = IsCharacterUnlocked(saveData, characters, i, character);
                var card = RuntimeUiFactory.CreatePanel(root, $"Character Card {i + 1}", new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.70f), new Vector2(startX + i * (cardWidth + gap), 0f), new Vector2(startX + i * (cardWidth + gap) + cardWidth, 0f), new Color(0.16f, 0.18f, 0.23f, 1f));

                if (character.Portrait != null)
                {
                    var portraitObject = new GameObject("Portrait");
                    portraitObject.transform.SetParent(card, false);
                    var portrait = portraitObject.AddComponent<Image>();
                    portrait.sprite = character.Portrait;
                    portrait.preserveAspect = true;
                    RuntimeUiFactory.SetRect(portrait.rectTransform, new Vector2(0.20f, 0.42f), new Vector2(0.80f, 0.88f), Vector2.zero, Vector2.zero);
                }

                var nameText = RuntimeUiFactory.CreateText(card, "Name", character.DisplayName, 24, TextAnchor.MiddleCenter, Color.white);
                RuntimeUiFactory.SetRect(nameText.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.40f), Vector2.zero, Vector2.zero);

                var roleText = RuntimeUiFactory.CreateText(card, "Role", unlocked ? character.Role.ToString() : "Locked", 18, TextAnchor.MiddleCenter, unlocked ? new Color(0.80f, 0.86f, 0.92f) : new Color(0.95f, 0.70f, 0.42f));
                RuntimeUiFactory.SetRect(roleText.rectTransform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.25f), Vector2.zero, Vector2.zero);

                var button = RuntimeUiFactory.CreateButton(card, "Select Button", unlocked ? "Start" : "Complete Learn to Chop", unlocked ? new Color(0.24f, 0.45f, 0.85f, 1f) : new Color(0.32f, 0.32f, 0.36f, 1f));
                RuntimeUiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.16f, 0.04f), new Vector2(0.84f, 0.14f), Vector2.zero, Vector2.zero);
                button.interactable = unlocked;
                if (unlocked)
                {
                    button.onClick.AddListener(() => onCharacterSelected(character));
                }
            }
        }

        private static bool IsCharacterUnlocked(AccountSaveData saveData, System.Collections.Generic.IReadOnlyList<CharacterDefinition> characters, int index, CharacterDefinition character)
        {
            if (character == null)
            {
                return false;
            }

            if (index == 0)
            {
                return true;
            }

            return saveData != null && saveData.unlockedCharacterIds.Contains(character.Id);
        }
    }
}
