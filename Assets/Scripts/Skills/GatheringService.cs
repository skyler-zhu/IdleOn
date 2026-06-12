using System;
using IdleOnLike.Data;
using IdleOnLike.Inventory;
using IdleOnLike.Quests;
using IdleOnLike.Save;

namespace IdleOnLike.Skills
{
    public sealed class GatheringService
    {
        private const string TreeNodeId = "tree";
        private const string WoodItemId = "wood";

        private readonly PlayerSaveData saveData;
        private readonly InventoryService inventoryService;
        private readonly QuestService questService;

        private float gatherTimer;

        public GatheringService(PlayerSaveData saveData, InventoryService inventoryService, QuestService questService)
        {
            this.saveData = saveData;
            this.inventoryService = inventoryService;
            this.questService = questService;
            this.saveData.EnsureCollections();
        }

        public event Action Changed;
        public event Action<string> LogAdded;

        public bool IsGathering => saveData.currentActivity == ZoneActivity.Chopping.ToString();

        public void StartGathering(string nodeId)
        {
            if (nodeId != TreeNodeId)
            {
                return;
            }

            gatherTimer = 0f;
            saveData.currentActivity = ZoneActivity.Chopping.ToString();
            LogAdded?.Invoke("Started chopping.");
            Changed?.Invoke();
        }

        public void StopGathering()
        {
            gatherTimer = 0f;
            saveData.currentActivity = ZoneActivity.Fighting.ToString();
            LogAdded?.Invoke("Returned to fighting.");
            Changed?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (!IsGathering)
            {
                return;
            }

            gatherTimer += deltaTime;
            if (gatherTimer < 2f)
            {
                return;
            }

            gatherTimer -= 2f;
            inventoryService.AddItem(WoodItemId, 1);
            questService.AddProgress(QuestObjectiveType.GatherResource, TreeNodeId, 1);
            LogAdded?.Invoke("Chopped Wood x1.");
            Changed?.Invoke();
        }
    }
}
