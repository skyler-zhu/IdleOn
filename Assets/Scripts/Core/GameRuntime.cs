using IdleOnLike.Combat;
using IdleOnLike.Data;
using IdleOnLike.Equipment;
using IdleOnLike.Inventory;
using IdleOnLike.Quests;
using IdleOnLike.Save;
using IdleOnLike.UI;
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

        public GameState State { get; private set; }
        public GameCatalog Catalog => catalog;
        public SaveService SaveService => saveService;
        public InventoryService InventoryService => inventoryService;
        public EquipmentService EquipmentService => equipmentService;
        public QuestService QuestService => questService;

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

            var saveData = PlayerSaveData.CreateNew(character, catalog.VillageZone);
            State = new GameState(catalog, saveData);
            CreateRuntimeServices();
            saveService.Save(saveData);
            sceneLoader.LoadZone(State.CurrentZone != null ? State.CurrentZone : catalog.VillageZone);
        }

        public void Save()
        {
            if (State != null)
            {
                saveService.Save(State.SaveData);
            }
        }

        public void TravelToForest()
        {
            TravelToZone(catalog.ForestZone);
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
            sceneLoader.LoadScene("CharacterSelect");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "CharacterSelect")
            {
                CharacterSelectScreen.Build(catalog, StartNewGame);
                return;
            }

            if (State == null || State.CurrentZone == null || scene.name != State.CurrentZone.SceneName)
            {
                return;
            }

            if (State.CurrentZone == catalog.ForestZone)
            {
                CombatController.Create(this);
                return;
            }

            VillageHudScreen.Build(this, TravelToForest, DeleteSaveAndReturnToCharacterSelect);
        }

        private void CreateRuntimeServices()
        {
            inventoryService = new InventoryService(State);
            equipmentService = new EquipmentService(State, inventoryService);
            questService = new QuestService(State, inventoryService);
            inventoryService.ItemAdded += questService.AddProgress;
            inventoryService.Changed += Save;
            equipmentService.Changed += Save;
            questService.Changed += Save;
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
