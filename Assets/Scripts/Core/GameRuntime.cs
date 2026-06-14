using System;
using IdleOnLike.Combat;
using IdleOnLike.Crafting;
using IdleOnLike.Data;
using IdleOnLike.Equipment;
using IdleOnLike.Inventory;
using IdleOnLike.Progression;
using IdleOnLike.Quests;
using IdleOnLike.Save;
using IdleOnLike.Skills;
using IdleOnLike.Shop;
using IdleOnLike.UI;
using IdleOnLike.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleOnLike.Core
{
    public sealed class GameRuntime : MonoBehaviour
    {
        public static GameRuntime Instance { get; private set; }

        private GameCatalog catalog;
        private SaveService saveService;
        private SceneLoaderService sceneLoader;
        private InventoryService inventoryService;
        private EquipmentService equipmentService;
        private QuestService questService;
        private GatheringService gatheringService;
        private CraftingService craftingService;
        private ShopService shopService;
        private TalentService talentService;
        private SkillTreeService skillTreeService;
        private OfflineProgressService offlineProgressService;
        private OfflineGainsResult pendingOfflineGains;
        private WorldMapPanel worldMapPanel;

        public GameState State { get; private set; }
        public GameCatalog Catalog => catalog;
        public SaveService SaveService => saveService;
        public InventoryService InventoryService => inventoryService;
        public EquipmentService EquipmentService => equipmentService;
        public QuestService QuestService => questService;
        public GatheringService GatheringService => gatheringService;
        public CraftingService CraftingService => craftingService;
        public ShopService ShopService => shopService;
        public TalentService TalentService => talentService;
        public SkillTreeService SkillTreeService => skillTreeService;
        public OfflineGainsResult PendingOfflineGains => pendingOfflineGains;

        private void Update()
        {
            if (State != null && worldMapPanel != null && Input.GetKeyDown(KeyCode.M))
            {
                worldMapPanel.Toggle();
            }
        }

        public void Initialize(GameCatalog gameCatalog)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            catalog = gameCatalog;
            saveService = new SaveService();
            sceneLoader = new SceneLoaderService();
            SceneManager.sceneLoaded += OnSceneLoaded;

            var saveData = saveService.Load();
            if (saveData == null)
            {
                sceneLoader.LoadScene("CharacterSelect");
                return;
            }

            State = new GameState(catalog, saveData);
            CreateRuntimeServices();
            EnsureWorldMap();
            CompleteSwitchCharacterQuestsForAllCharacters(State.SaveData.characterId);
            TryApplyStartupOfflineGains();
            var zone = State.CurrentZone != null ? State.CurrentZone : catalog.VillageZone;
            sceneLoader.LoadZone(zone);
        }

        public void StartNewGame(CharacterDefinition character)
        {
            if (character == null)
            {
                Debug.LogError("Cannot start a new game without a character.");
                return;
            }

            AccountSaveData accountData;
            if (State != null)
            {
                accountData = State.AccountData;
            }
            else
            {
                accountData = saveService.Load();
            }

            if (accountData == null)
            {
                accountData = AccountSaveData.FromLegacy(PlayerSaveData.CreateNew(character, catalog.VillageZone));
            }

            accountData.GetOrCreateCharacter(character, catalog.VillageZone);
            State = new GameState(catalog, accountData);
            CreateRuntimeServices();
            EnsureWorldMap();
            CompleteSwitchCharacterQuestsForAllCharacters(character.Id);
            pendingOfflineGains = CalculateOfflineForActiveCharacter();
            saveService.Save(accountData);
            sceneLoader.LoadZone(State.CurrentZone != null ? State.CurrentZone : catalog.VillageZone);
        }

        public void Save()
        {
            if (State != null)
            {
                saveService.Save(State.AccountData);
            }
        }

        public void TravelToForest()
        {
            if (State != null)
            {
                State.SaveData.currentActivity = ZoneActivity.Fighting.ToString();
            }

            TravelToZone(catalog.ForestZone);
        }

        public void TravelToMineCave()
        {
            if (State != null)
            {
                State.SaveData.currentActivity = ZoneActivity.Fighting.ToString();
            }

            TravelToZone(catalog.FindZone("mine_cave"));
        }

        public void ReturnToVillage()
        {
            TravelToZone(catalog.VillageZone);
        }

        public void TravelToZone(ZoneDefinition zone)
        {
            if (State == null || zone == null)
            {
                Debug.LogError("Cannot travel without active save data and a target zone.");
                return;
            }

            State.SaveData.currentZoneId = zone.Id;
            Save();
            sceneLoader.LoadZone(zone);
        }

        public void DeleteSaveAndReturnToCharacterSelect()
        {
            saveService.DeleteSave();
            State = null;
            inventoryService = null;
            equipmentService = null;
            questService = null;
            gatheringService = null;
            craftingService = null;
            shopService = null;
            talentService = null;
            skillTreeService = null;
            offlineProgressService = null;
            pendingOfflineGains = null;
            worldMapPanel?.Hide();
            sceneLoader.LoadScene("CharacterSelect");
        }

        public void ReturnToCharacterSelect()
        {
            Save();
            sceneLoader.LoadScene("CharacterSelect");
        }

        public OfflineGainsResult SimulateOfflineHour()
        {
            if (offlineProgressService == null)
            {
                return null;
            }

            var result = offlineProgressService.CalculateOfflineGains(TimeSpan.FromHours(1));
            Save();
            return result;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            worldMapPanel?.Hide();
            if (scene.name == "CharacterSelect")
            {
                CharacterSelectScreen.Build(catalog, State != null ? State.AccountData : saveService.Load(), StartNewGame);
                return;
            }

            if (State == null || State.CurrentZone == null || scene.name != State.CurrentZone.SceneName)
            {
                return;
            }

            if (State.CurrentZone == catalog.ForestZone)
            {
                CombatController.Create(this);
                ShowPendingOfflineGainsIfAny();
                return;
            }

            if (State.CurrentZone.Id == "mine_cave")
            {
                MineCaveController.Create(this);
                ShowPendingOfflineGainsIfAny();
                return;
            }

            VillageView.Create(this);
            VillageHudScreen.Build(this, TravelToForest, TravelToMineCave, ReturnToCharacterSelect, DeleteSaveAndReturnToCharacterSelect);
            ShowPendingOfflineGainsIfAny();
        }

        private void CreateRuntimeServices()
        {
            inventoryService = new InventoryService(State);
            equipmentService = new EquipmentService(State, inventoryService);
            talentService = new TalentService(State);
            skillTreeService = new SkillTreeService(State);
            questService = new QuestService(State, inventoryService);
            gatheringService = new GatheringService(State.SaveData, inventoryService, questService, skillTreeService);
            craftingService = new CraftingService(State, inventoryService);
            shopService = new ShopService(State, inventoryService);
            offlineProgressService = new OfflineProgressService(State.SaveData, State.AccountData, catalog, inventoryService, questService, skillTreeService);
            inventoryService.ItemAdded += questService.AddProgress;
            equipmentService.ItemEquipped += questService.AddProgress;
            craftingService.ItemCrafted += questService.AddProgress;
            inventoryService.Changed += Save;
            equipmentService.Changed += Save;
            questService.Changed += Save;
            gatheringService.Changed += Save;
            craftingService.Changed += Save;
            shopService.Changed += Save;
            talentService.Changed += Save;
            skillTreeService.Changed += Save;
        }

        private void EnsureWorldMap()
        {
            if (worldMapPanel == null)
            {
                worldMapPanel = new WorldMapPanel(this);
            }
        }

        private void TryApplyStartupOfflineGains()
        {
            pendingOfflineGains = CalculateOfflineForAllCharacters(State.AccountData.activeCharacterId);
            Save();
        }

        private OfflineGainsResult CalculateOfflineForActiveCharacter()
        {
            if (State == null)
            {
                return null;
            }

            return CalculateOfflineForAllCharacters(State.AccountData.activeCharacterId);
        }

        private OfflineGainsResult CalculateOfflineForAllCharacters(string resultCharacterId)
        {
            if (State == null || State.AccountData == null)
            {
                return null;
            }

            var previousActiveId = State.AccountData.activeCharacterId;
            OfflineGainsResult selectedResult = null;
            foreach (var characterSave in State.AccountData.characters)
            {
                if (characterSave == null)
                {
                    continue;
                }

                if (!DateTime.TryParse(characterSave.lastSavedUtc, out var lastSavedUtc))
                {
                    characterSave.lastSavedUtc = DateTime.UtcNow.ToString("O");
                    continue;
                }

                var elapsed = DateTime.UtcNow - lastSavedUtc.ToUniversalTime();
                if (elapsed < TimeSpan.FromMinutes(1))
                {
                    continue;
                }

                State.AccountData.activeCharacterId = characterSave.characterId;
                var tempState = new GameState(catalog, State.AccountData);
                var tempInventory = new InventoryService(tempState);
                var tempQuest = new QuestService(tempState, tempInventory);
                var tempSkillTree = new SkillTreeService(tempState);
                tempInventory.ItemAdded += tempQuest.AddProgress;
                var tempOffline = new OfflineProgressService(characterSave, State.AccountData, catalog, tempInventory, tempQuest, tempSkillTree);
                var result = tempOffline.CalculateOfflineGains(elapsed);
                tempInventory.ItemAdded -= tempQuest.AddProgress;
                characterSave.lastSavedUtc = DateTime.UtcNow.ToString("O");
                if (characterSave.characterId == resultCharacterId)
                {
                    selectedResult = result;
                }
            }

            State.AccountData.activeCharacterId = previousActiveId;
            return selectedResult;
        }

        private void CompleteSwitchCharacterQuestsForAllCharacters(string switchedToCharacterId)
        {
            if (State == null || State.AccountData == null || string.IsNullOrEmpty(switchedToCharacterId))
            {
                return;
            }

            var previousActiveId = State.AccountData.activeCharacterId;
            foreach (var characterSave in State.AccountData.characters)
            {
                if (characterSave == null)
                {
                    continue;
                }

                State.AccountData.activeCharacterId = characterSave.characterId;
                var tempState = new GameState(catalog, State.AccountData);
                var tempInventory = new InventoryService(tempState);
                var tempQuest = new QuestService(tempState, tempInventory);
                tempInventory.ItemAdded += tempQuest.AddProgress;
                tempQuest.CompleteAutoCompletableSwitchCharacterQuests(switchedToCharacterId);
                tempInventory.ItemAdded -= tempQuest.AddProgress;
            }

            State.AccountData.activeCharacterId = previousActiveId;
            questService.CompleteAutoCompletableSwitchCharacterQuests(switchedToCharacterId);
        }

        private void ShowPendingOfflineGainsIfAny()
        {
            if (pendingOfflineGains == null)
            {
                return;
            }

            OfflineGainsPanel.Show(pendingOfflineGains);
            pendingOfflineGains = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Save();
                Instance = null;
            }
        }
    }
}
