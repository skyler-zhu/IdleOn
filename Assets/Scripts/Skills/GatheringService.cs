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

        public bool StartGathering(string nodeId, bool isNearNode = true)
        {
            if (nodeId != TreeNodeId)
            {
                return false;
            }

            if (!isNearNode)
            {
                gatherTimer = 0f;
                saveData.currentActivity = ZoneActivity.Chopping.ToString();
                LogAdded?.Invoke("Move near tree.");
                Changed?.Invoke();
                return true;
            }

            gatherTimer = 0f;
            saveData.currentActivity = ZoneActivity.Chopping.ToString();
            LogAdded?.Invoke("Started chopping.");
            Changed?.Invoke();
            return true;
        }

        public void StopGathering()
        {
            gatherTimer = 0f;
            saveData.currentActivity = ZoneActivity.Fighting.ToString();
            LogAdded?.Invoke("Returned to fighting.");
            Changed?.Invoke();
        }

        public void Tick(float deltaTime, bool canGather = true)
        {
            if (!IsGathering)
            {
                return;
            }

            if (!canGather)
            {
                gatherTimer = 0f;
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
