using System;
using System.Collections.Generic;
using IdleOnLike.Data;

namespace IdleOnLike.Save
{
    [Serializable]
    public sealed class PlayerSaveData
    {
        public string characterId;
        public string characterName;
        public int level = 1;
        public int experience;
        public int coins;
        public string currentZoneId;
        public string lastSavedUtc;
        public List<SaveItemStack> inventory = new List<SaveItemStack>();
        public List<SaveEquipmentSlot> equipment = new List<SaveEquipmentSlot>();
        public List<string> activeQuestIds = new List<string>();
        public List<string> completedQuestIds = new List<string>();
        public List<SaveQuestProgress> questProgress = new List<SaveQuestProgress>();

        public void EnsureCollections()
        {
            if (inventory == null)
            {
                inventory = new List<SaveItemStack>();
            }

            if (equipment == null)
            {
                equipment = new List<SaveEquipmentSlot>();
            }

            if (activeQuestIds == null)
            {
                activeQuestIds = new List<string>();
            }

            if (completedQuestIds == null)
            {
                completedQuestIds = new List<string>();
            }

            if (questProgress == null)
            {
                questProgress = new List<SaveQuestProgress>();
            }
        }

        public static PlayerSaveData CreateNew(CharacterDefinition character, ZoneDefinition fallbackZone)
        {
            var startingZone = character.StartingZone != null ? character.StartingZone : fallbackZone;
            var saveData = new PlayerSaveData
            {
                characterId = character.Id,
                characterName = character.DisplayName,
                level = 1,
                experience = 0,
                coins = 0,
                currentZoneId = startingZone != null ? startingZone.Id : string.Empty,
                lastSavedUtc = DateTime.UtcNow.ToString("O")
            };

            if (character.StartingWeapon != null)
            {
                saveData.equipment.Add(new SaveEquipmentSlot
                {
                    slot = EquipmentSlot.Weapon,
                    itemId = character.StartingWeapon.Id
                });
            }

            return saveData;
        }
    }

    [Serializable]
    public sealed class SaveItemStack
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class SaveEquipmentSlot
    {
        public EquipmentSlot slot;
        public string itemId;
    }

    [Serializable]
    public sealed class SaveQuestProgress
    {
        public string questId;
        public int objectiveIndex;
        public int currentAmount;
    }
}
