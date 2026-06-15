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
        private AudioService audioService;
        private ExitConfirmationPanel exitConfirmationPanel;
        private bool spawnForestAtStonePortal;
        private static readonly TimeSpan MinimumOfflineElapsed = TimeSpan.FromSeconds(5);

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
        public AudioService AudioService => audioService;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (RuntimeUiOverlayRegistry.CloseTop())
                {
                    return;
                }

                EnsureExitConfirmationPanel();
                exitConfirmationPanel.Show();
                return;
            }

            if (State != null && Input.GetKeyDown(KeyCode.U))
            {
                RuntimeUiOverlayRegistry.ToggleQuestDetails();
            }

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
            audioService = new AudioService(transform);
            saveService = new SaveService();
            sceneLoader = new SceneLoaderService();
            SceneManager.sceneLoaded += OnSceneLoaded;

            var saveData = saveService.Load();
            if (saveData == null)
            {
                sceneLoader.LoadScene("CharacterSelect");
                return;
            }

            saveData.EnsureCollections();
            if (saveData.GetActiveCharacter() == null)
            {
                Debug.LogWarning("Loaded save data has no active character. Returning to character select.");
                sceneLoader.LoadScene("CharacterSelect");
                return;
            }

            State = new GameState(catalog, saveData);
            CreateRuntimeServices();
            EnsureWorldMap();
            var zone = EnsureValidCurrentZone();
            CompleteSwitchCharacterQuestsForAllCharacters(State.SaveData.characterId);
            TryApplyStartupOfflineGains();
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
            EnsureValidCurrentZone();
            CompleteSwitchCharacterQuestsForAllCharacters(character.Id);
            pendingOfflineGains = CalculateOfflineForActiveCharacter();
            saveService.Save(accountData);
            sceneLoader.LoadZone(EnsureValidCurrentZone());
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

        public void ReturnFromStoneSanctumToForest()
        {
            spawnForestAtStonePortal = true;
            TravelToForest();
        }

        public void TravelToMineCave()
        {
            if (State != null)
            {
                State.SaveData.currentActivity = ZoneActivity.Fighting.ToString();
            }

            TravelToZone(catalog.FindZone("mine_cave"));
        }

        public void TravelToStoneSanctum()
        {
            if (State != null)
            {
                State.SaveData.currentActivity = ZoneActivity.Fighting.ToString();
            }

            TravelToZone(catalog.FindZone("stone_sanctum"));
        }

        public bool ConsumeSpawnForestAtStonePortal()
        {
            var shouldSpawnAtPortal = spawnForestAtStonePortal;
            spawnForestAtStonePortal = false;
            return shouldSpawnAtPortal;
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

            if (!IsZoneUnlocked(zone))
            {
                Debug.Log($"Zone locked: {zone.DisplayName}");
                return;
            }

            State.SaveData.currentZoneId = zone.Id;
            Save();
            sceneLoader.LoadZone(zone);
        }

        public bool IsZoneUnlocked(ZoneDefinition zone)
        {
            if (zone == null)
            {
                return false;
            }

            if (State == null || QuestService == null)
            {
                return zone.RequiredQuest == null && zone.RequiredCharacterLevel <= 0;
            }

            if (zone.RequiredCharacterLevel > 0 && State.SaveData.level < zone.RequiredCharacterLevel)
            {
                return false;
            }

            return zone.RequiredQuest == null || QuestService.IsQuestCompleted(zone.RequiredQuest.Id);
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
            EnsureAudioListener();
            worldMapPanel?.Hide();
            if (scene.name == "CharacterSelect")
            {
                audioService?.PlayBgm(catalog.CharacterSelectBgmClip);
                CharacterSelectScreen.Build(catalog, State != null ? State.AccountData : saveService.Load(), StartNewGame);
                return;
            }

            var currentZone = EnsureValidCurrentZone();
            if (State == null || currentZone == null || scene.name != currentZone.SceneName)
            {
                return;
            }

            audioService?.PlayBgm(currentZone.BgmClip);
            if (currentZone == catalog.ForestZone || currentZone.Id == "stone_sanctum")
            {
                CombatController.Create(this);
                ShowPendingOfflineGainsIfAny();
                return;
            }

            if (currentZone.Id == "mine_cave")
            {
                MineCaveController.Create(this);
                ShowPendingOfflineGainsIfAny();
                return;
            }

            VillageView.Create(this);
            VillageHudScreen.Build(this, TravelToForest, TravelToMineCave, ReturnToCharacterSelect, DeleteSaveAndReturnToCharacterSelect);
            ShowPendingOfflineGainsIfAny();
        }

        private ZoneDefinition EnsureValidCurrentZone()
        {
            if (State == null)
            {
                return null;
            }

            if (State.SaveData == null)
            {
                Debug.LogWarning("Current account has no active character save data.");
                return null;
            }

            var zone = State.CurrentZone;
            if (zone != null && !string.IsNullOrWhiteSpace(zone.SceneName))
            {
                return zone;
            }

            zone = catalog != null ? catalog.VillageZone : null;
            if (zone == null)
            {
                Debug.LogError("Cannot recover current zone because GameCatalog has no Village zone.");
                return null;
            }

            var oldZoneId = State.SaveData != null ? State.SaveData.currentZoneId : string.Empty;
            State.SaveData.currentZoneId = zone.Id;
            Debug.LogWarning($"Recovered missing current zone '{oldZoneId}' by returning to {zone.DisplayName}.");
            Save();
            return zone;
        }

        private static void EnsureAudioListener()
        {
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length == 0)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    camera.gameObject.AddComponent<AudioListener>();
                }

                return;
            }

            for (var i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }

            listeners[0].enabled = true;
        }

        public void PlayAttackSfx()
        {
            var character = State != null ? State.Character : null;
            audioService?.PlaySfx(character != null ? character.AttackSfx : null);
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

        private void EnsureExitConfirmationPanel()
        {
            if (exitConfirmationPanel == null)
            {
                exitConfirmationPanel = new ExitConfirmationPanel(this);
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
                if (elapsed < MinimumOfflineElapsed)
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
