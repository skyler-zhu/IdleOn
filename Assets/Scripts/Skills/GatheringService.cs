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
        private const string RockNodeId = "rock";
        private const string OreItemId = "ore";

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

        public bool IsChopping => saveData.currentActivity == ZoneActivity.Chopping.ToString();
        public bool IsMining => saveData.currentActivity == ZoneActivity.Mining.ToString();
        public bool IsGathering => IsChopping || IsMining;

        public bool StartGathering(string nodeId, bool isNearNode = true)
        {
            if (!TryGetNode(nodeId, out var activity, out _, out _, out var actionLabel))
            {
                return false;
            }

            if (!isNearNode)
            {
                gatherTimer = 0f;
                saveData.currentActivity = activity.ToString();
                LogAdded?.Invoke($"Move near {nodeId}.");
                Changed?.Invoke();
                return true;
            }

            gatherTimer = 0f;
            saveData.currentActivity = activity.ToString();
            LogAdded?.Invoke(activity == ZoneActivity.Mining ? "Started mining." : "Started chopping.");
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
            GatherCurrentResource();
        }

        public bool GatherOnce(bool canGather)
        {
            if (!IsGathering)
            {
                return false;
            }

            if (!canGather)
            {
                gatherTimer = 0f;
                LogAdded?.Invoke(IsMining ? "Move near rock." : "Move near tree.");
                Changed?.Invoke();
                return false;
            }

            gatherTimer = 0f;
            GatherCurrentResource();
            return true;
        }

        private void GatherCurrentResource()
        {
            var nodeId = IsMining ? RockNodeId : TreeNodeId;
            if (!TryGetNode(nodeId, out _, out var itemId, out var itemName, out var actionLabel))
            {
                return;
            }

            inventoryService.AddItem(itemId, 1);
            questService.AddProgress(QuestObjectiveType.GatherResource, nodeId, 1);
            LogAdded?.Invoke($"{actionLabel} {itemName} x1.");
            Changed?.Invoke();
        }

        private static bool TryGetNode(string nodeId, out ZoneActivity activity, out string itemId, out string itemName, out string actionLabel)
        {
            if (nodeId == RockNodeId)
            {
                activity = ZoneActivity.Mining;
                itemId = OreItemId;
                itemName = "Ore";
                actionLabel = "Mined";
                return true;
            }

            if (nodeId == TreeNodeId)
            {
                activity = ZoneActivity.Chopping;
                itemId = WoodItemId;
                itemName = "Wood";
                actionLabel = "Chopped";
                return true;
            }

            activity = ZoneActivity.Fighting;
            itemId = string.Empty;
            itemName = string.Empty;
            actionLabel = string.Empty;
            return false;
        }
    }
}
