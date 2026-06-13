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
        public int currentHp;
        public string currentActivity;
        public string currentZoneId;
        public string lastSavedUtc;
        public List<SaveItemStack> inventory = new List<SaveItemStack>();
        public List<SaveEquipmentSlot> equipment = new List<SaveEquipmentSlot>();
        public List<string> activeQuestIds = new List<string>();
        public List<string> completedQuestIds = new List<string>();
        public List<SaveQuestProgress> questProgress = new List<SaveQuestProgress>();
        public List<string> unlockedCharacterIds = new List<string>();

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

            if (unlockedCharacterIds == null)
            {
                unlockedCharacterIds = new List<string>();
            }

            if (!string.IsNullOrEmpty(characterId) && unlockedCharacterIds.Count == 0)
            {
                unlockedCharacterIds.Add(characterId);
            }

            if (string.IsNullOrEmpty(currentActivity))
            {
                currentActivity = ZoneActivity.Fighting.ToString();
            }

            if (currentHp <= 0)
            {
                currentHp = 50 + level * 10;
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
                currentHp = 60,
                currentActivity = ZoneActivity.Fighting.ToString(),
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

            if (!string.IsNullOrEmpty(character.Id))
            {
                saveData.unlockedCharacterIds.Add(character.Id);
            }

            return saveData;
        }
    }

    [Serializable]
    public sealed class AccountSaveData
    {
        public int coins;
        public string activeCharacterId;
        public string lastSavedUtc;
        public List<SaveItemStack> sharedInventory = new List<SaveItemStack>();
        public List<string> unlockedCharacterIds = new List<string>();
        public List<PlayerSaveData> characters = new List<PlayerSaveData>();

        public void EnsureCollections()
        {
            if (sharedInventory == null)
            {
                sharedInventory = new List<SaveItemStack>();
            }

            if (unlockedCharacterIds == null)
            {
                unlockedCharacterIds = new List<string>();
            }

            if (characters == null)
            {
                characters = new List<PlayerSaveData>();
            }

            foreach (var character in characters)
            {
                character?.EnsureCollections();
            }

            if (string.IsNullOrEmpty(activeCharacterId) && characters.Count > 0)
            {
                activeCharacterId = characters[0].characterId;
            }

            if (string.IsNullOrEmpty(lastSavedUtc))
            {
                lastSavedUtc = DateTime.UtcNow.ToString("O");
            }
        }

        public PlayerSaveData GetActiveCharacter()
        {
            EnsureCollections();
            var active = characters.Find(character => character.characterId == activeCharacterId);
            if (active != null)
            {
                return active;
            }

            return characters.Count > 0 ? characters[0] : null;
        }

        public PlayerSaveData GetOrCreateCharacter(CharacterDefinition character, ZoneDefinition fallbackZone)
        {
            EnsureCollections();
            var existing = characters.Find(entry => entry.characterId == character.Id);
            if (existing != null)
            {
                activeCharacterId = existing.characterId;
                return existing;
            }

            var created = PlayerSaveData.CreateNew(character, fallbackZone);
            created.inventory.Clear();
            characters.Add(created);
            activeCharacterId = created.characterId;
            if (!unlockedCharacterIds.Contains(created.characterId))
            {
                unlockedCharacterIds.Add(created.characterId);
            }

            return created;
        }

        public static AccountSaveData FromLegacy(PlayerSaveData legacy)
        {
            legacy.EnsureCollections();
            var account = new AccountSaveData
            {
                coins = legacy.coins,
                activeCharacterId = legacy.characterId,
                lastSavedUtc = legacy.lastSavedUtc,
                sharedInventory = legacy.inventory != null ? legacy.inventory : new List<SaveItemStack>(),
                unlockedCharacterIds = legacy.unlockedCharacterIds != null ? legacy.unlockedCharacterIds : new List<string>(),
                characters = new List<PlayerSaveData> { legacy }
            };

            legacy.inventory = new List<SaveItemStack>();
            account.EnsureCollections();
            return account;
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
