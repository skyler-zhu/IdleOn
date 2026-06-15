using System.Collections.Generic;
using IdleOnLike.Core;
using IdleOnLike.Data;
using UnityEngine;
using UnityEngine.UI;
using UiFactory = IdleOnLike.UI.RuntimeUiFactory;

namespace IdleOnLike.World
{
    public sealed class MineCaveController : MonoBehaviour
    {
        private const float MoveSpeed = 3.1f;
        private const float JumpDuration = 0.72f;
        private const float JumpHeight = 1.12f;
        private const float AttackSeconds = 0.5f;
        private const float AttackCooldownSeconds = 1f;
        private const float ClimbSeconds = 1.25f;
        private static readonly Vector3 LowerSpawn = new Vector3(5.35f, -1.35f, 0f);
        private static readonly Vector3 UpperSpawn = new Vector3(-1.2f, 1.15f, 0f);
        private static readonly Vector3 RopePosition = new Vector3(0.2f, -0.05f, 0f);
        private static readonly Vector3 LowerRopePoint = new Vector3(RopePosition.x, LowerSpawn.y, 0f);
        private static readonly Vector3 UpperRopePoint = new Vector3(RopePosition.x, UpperSpawn.y, 0f);
        private static readonly Vector3 VillagePortalPosition = new Vector3(6.15f, LowerSpawn.y + 0.13f, 0f);
        private static readonly Vector3 LowerRockPosition = new Vector3(-5.35f, -1.25f, 0f);
        private static readonly Vector3 UpperRockPosition = new Vector3(-5.75f, 1.25f, 0f);

        private readonly List<string> logLines = new List<string>();
        private GameRuntime runtime;
        private Transform playerTransform;
        private VisualAnimatorDriver playerVisual;
        private PlayerControllerRuntime playerController;
        private GameObject attackFlash;
        private Text statusText;
        private Text promptText;
        private Text logText;
        private Button autoButton;
        private Button actionButton;
        private bool isAutoMode;
        private bool upperFloor;
        private float nextManualMineTime;
        private bool isClimbing;
        private bool climbTargetUpperFloor;
        private float climbElapsedSeconds;
        private Vector3 climbStartPosition;
        private Vector3 climbEndPosition;

        public static MineCaveController Create(GameRuntime runtime)
        {
            var controllerObject = new GameObject("Mine Cave Controller");
            var controller = controllerObject.AddComponent<MineCaveController>();
            controller.Initialize(runtime);
            return controller;
        }

        private void Initialize(GameRuntime gameRuntime)
        {
            runtime = gameRuntime;
            ConfigureCamera();
            BuildWorld();
            BuildHud();
            RestoreSavedActivity();
            runtime.GatheringService.LogAdded += AddLog;
            runtime.GatheringService.Changed += RefreshHud;
            runtime.GatheringService.ResourceGathered += OnResourceGathered;
            RefreshHud();
        }

        private void Update()
        {
            if (runtime == null || runtime.State == null)
            {
                return;
            }

            if (isClimbing)
            {
                UpdateClimb();
                RefreshHud();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F) && IsNearVillagePortal())
            {
                runtime.ReturnToVillage();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F) && IsNearRope())
            {
                StartClimb();
                RefreshHud();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                playerController.TryJump(isClimbing);
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                if (playerController.TryAttack(isClimbing))
                {
                    runtime.PlayAttackSfx();
                }

                if (!isAutoMode)
                {
                    MineManual();
                }
            }

            if (!isAutoMode)
            {
                MoveManual(Input.GetAxisRaw("Horizontal"));
            }
            else
            {
                MoveAuto();
            }

            playerController.TickVisuals(upperFloor ? UpperSpawn.y : LowerSpawn.y, Time.deltaTime);
            if (isAutoMode && IsNearRock() && !runtime.GatheringService.IsMining)
            {
                runtime.GatheringService.StartGathering("rock", true);
            }

            runtime.GatheringService.Tick(isAutoMode ? Time.deltaTime : 0f, isAutoMode && IsNearRock());
            RefreshHud();
        }

        private void MoveManual(float horizontal)
        {
            playerController.MoveHorizontal(horizontal, GetMoveSpeed(), -6.6f, 6.4f, Time.deltaTime);
        }

        private void RestoreSavedActivity()
        {
            if (runtime.State.SaveData.currentActivity != ZoneActivity.Mining.ToString())
            {
                return;
            }

            isAutoMode = true;
            upperFloor = false;
            playerTransform.position = LowerRockPosition + new Vector3(0.45f, -0.10f, 0f);
            playerController.FaceFromDelta(LowerRockPosition.x - playerTransform.position.x);
            runtime.GatheringService.StartGathering("rock", true);
        }

        private void MoveAuto()
        {
            if (!runtime.GatheringService.IsMining)
            {
                playerController.StopMoving();
                return;
            }

            var target = upperFloor ? UpperRockPosition : LowerRockPosition;
            playerController.FaceFromDelta(target.x - playerTransform.position.x);
            playerController.MoveTowardX(target.x, GetMoveSpeed(), Time.deltaTime);
        }

        private float GetMoveSpeed()
        {
            return MoveSpeed * runtime.TalentService.GetMoveSpeedMultiplier();
        }

        private void StartClimb()
        {
            if (isClimbing)
            {
                return;
            }

            climbTargetUpperFloor = !upperFloor;
            climbElapsedSeconds = 0f;
            climbStartPosition = upperFloor ? UpperRopePoint : LowerRopePoint;
            climbEndPosition = climbTargetUpperFloor ? UpperRopePoint : LowerRopePoint;
            playerTransform.position = climbStartPosition;
            playerController.ResetActionState();
            isClimbing = true;
            AddLog(climbTargetUpperFloor ? "Climbing to upper vein..." : "Climbing to lower vein...");
        }

        private void UpdateClimb()
        {
            climbElapsedSeconds = Mathf.Min(ClimbSeconds, climbElapsedSeconds + Time.deltaTime);
            var progress = Mathf.SmoothStep(0f, 1f, climbElapsedSeconds / ClimbSeconds);
            playerTransform.position = Vector3.Lerp(climbStartPosition, climbEndPosition, progress);

            if (climbElapsedSeconds < ClimbSeconds)
            {
                return;
            }

            isClimbing = false;
            upperFloor = climbTargetUpperFloor;
            playerTransform.position = upperFloor ? UpperRopePoint : LowerRopePoint;
            AddLog(upperFloor ? "Reached upper vein." : "Reached lower vein.");
        }

        private void MineManual()
        {
            if (isClimbing || isAutoMode || Time.time < nextManualMineTime)
            {
                return;
            }

            if (!runtime.GatheringService.IsMining)
            {
                if (!IsNearRock())
                {
                    runtime.State.SaveData.currentActivity = ZoneActivity.Fighting.ToString();
                    runtime.Save();
                    return;
                }

                runtime.GatheringService.StartGathering("rock", true);
            }

            runtime.GatheringService.GatherOnce(IsNearRock());
            nextManualMineTime = Time.time + runtime.SkillTreeService.GetGatherSeconds(SkillType.Mining, 2f);
        }

        private bool IsNearRope()
        {
            return Mathf.Abs(playerTransform.position.x - RopePosition.x) <= 0.75f;
        }

        private bool IsNearVillagePortal()
        {
            return !upperFloor && Vector3.Distance(playerTransform.position, VillagePortalPosition) <= 1.25f;
        }

        private bool IsNearRock()
        {
            if (isClimbing)
            {
                return false;
            }

            var rock = upperFloor ? UpperRockPosition : LowerRockPosition;
            return Vector3.Distance(playerTransform.position, rock) <= 1.0f;
        }

        private void ToggleAutoMode()
        {
            isAutoMode = !isAutoMode;
            AddLog(isAutoMode ? "Mining set to Auto." : "Mining set to Manual.");
            RefreshHud();
        }

        private void BuildWorld()
        {
            var visual = GetZoneVisual();
            CreateTilemapOrSprite(visual, "Cave Backdrop", ZoneVisualSlotType.Background, new Vector3(0f, 0f, 0.5f), new Vector2(16f, 6.6f), new Color32(39, 42, 52, 255), -20);
            CreateTilemapOrSprite(visual, "Lower Platform", ZoneVisualSlotType.LowerGround, new Vector3(0f, -1.85f, 0f), new Vector2(14.2f, 0.28f), new Color32(86, 80, 78, 255), -8);
            CreateTilemapOrSprite(visual, "Upper Platform", ZoneVisualSlotType.UpperPlatform, new Vector3(0f, 0.75f, 0f), new Vector2(15.2f, 0.24f), new Color32(93, 86, 84, 255), -8);
            CreateSprite("Rope", RopePosition, new Vector3(0.12f, 2.75f, 1f), new Color32(176, 139, 82, 255), 0, GetVisualSprite(visual, ZoneVisualSlotType.Rope));
            CreatePortal();
            CreateResourceSprite("Lower Rock", "rock", LowerRockPosition, new Vector3(0.78f, 0.62f, 1f), new Color32(113, 128, 151, 255), 1);
            CreateResourceSprite("Upper Rock", "rock", UpperRockPosition, new Vector3(0.62f, 0.54f, 1f), new Color32(121, 138, 166, 255), 1);

            var player = CreateSprite("Mine Player", LowerSpawn, new Vector3(1f, 1f, 1f), new Color32(82, 168, 255, 255), 5);
            playerTransform = player.transform;
            var character = runtime.State.Character;
            playerVisual = new VisualAnimatorDriver(
                player,
                character != null ? character.IdleSprite : null,
                character != null ? character.AnimationClips : null,
                character != null ? character.VisualScale : 1f,
                5,
                new Color32(82, 168, 255, 255));
            playerVisual.SetFacing(1f);

            attackFlash = CreateSprite("Mine Attack Flash", new Vector3(0.62f, 0.10f, 0f), new Vector3(0.34f, 0.10f, 1f), new Color32(255, 236, 128, 255), 6);
            attackFlash.transform.SetParent(player.transform, false);
            attackFlash.SetActive(false);
            playerController = new PlayerControllerRuntime(playerTransform, playerVisual, attackFlash, JumpDuration, JumpHeight, AttackSeconds, AttackCooldownSeconds);
        }

        private void BuildHud()
        {
            var canvas = UiFactory.CreateCanvas("Mine Cave HUD");
            var topBar = UiFactory.CreatePanel(canvas.transform, "Top Bar", new Vector2(0f, 0.88f), Vector2.one, Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.10f, 0.92f));
            statusText = UiFactory.CreateText(topBar, "Status", string.Empty, 19, TextAnchor.MiddleLeft, Color.white);
            UiFactory.SetRect(statusText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.31f, 1f), Vector2.zero, Vector2.zero);

            autoButton = UiFactory.CreateButton(topBar, "Auto Manual Button", "Auto", new Color(0.22f, 0.36f, 0.48f, 1f));
            UiFactory.SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(0.32f, 0.20f), new Vector2(0.41f, 0.80f), Vector2.zero, Vector2.zero);
            autoButton.onClick.AddListener(ToggleAutoMode);

            actionButton = UiFactory.CreateButton(topBar, "Action Button", "Action (J)", new Color(0.48f, 0.34f, 0.18f, 1f));
            UiFactory.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.42f, 0.20f), new Vector2(0.52f, 0.80f), Vector2.zero, Vector2.zero);
            actionButton.onClick.AddListener(() =>
            {
                if (playerController.TryAttack(isClimbing))
                {
                    runtime.PlayAttackSfx();
                }

                MineManual();
            });

            var inventoryPanel = new IdleOnLike.UI.InventoryEquipmentPanel(runtime, canvas.transform);
            var talentPanel = new IdleOnLike.UI.TalentTreePanel(runtime, canvas.transform);
            var skillPanel = new IdleOnLike.UI.SkillTreePanel(runtime, canvas.transform);
            var inventoryButton = UiFactory.CreateButton(topBar, "Inventory Button", "Inventory", new Color(0.26f, 0.30f, 0.46f, 1f));
            UiFactory.SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(0.53f, 0.20f), new Vector2(0.64f, 0.80f), Vector2.zero, Vector2.zero);
            inventoryButton.onClick.AddListener(inventoryPanel.Toggle);
            var talentButton = UiFactory.CreateButton(topBar, "Talents Button", "Talents", new Color(0.30f, 0.28f, 0.46f, 1f));
            UiFactory.SetRect(talentButton.GetComponent<RectTransform>(), new Vector2(0.65f, 0.20f), new Vector2(0.75f, 0.80f), Vector2.zero, Vector2.zero);
            talentButton.onClick.AddListener(talentPanel.Toggle);
            var skillButton = UiFactory.CreateButton(topBar, "Skills Button", "Skills", new Color(0.24f, 0.38f, 0.32f, 1f));
            UiFactory.SetRect(skillButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.20f), new Vector2(0.85f, 0.80f), Vector2.zero, Vector2.zero);
            skillButton.onClick.AddListener(skillPanel.Toggle);
            var charactersButton = UiFactory.CreateButton(topBar, "Characters Button", "Chars", new Color(0.30f, 0.28f, 0.46f, 1f));
            UiFactory.SetRect(charactersButton.GetComponent<RectTransform>(), new Vector2(0.86f, 0.20f), new Vector2(0.98f, 0.80f), Vector2.zero, Vector2.zero);
            charactersButton.onClick.AddListener(runtime.ReturnToCharacterSelect);
            _ = new IdleOnLike.UI.QuestTrackerPanel(runtime, canvas.transform, false);

            promptText = UiFactory.CreateText(canvas.transform, "Rope Prompt", string.Empty, 22, TextAnchor.MiddleCenter, Color.white);
            UiFactory.SetRect(promptText.rectTransform, new Vector2(0.40f, 0.64f), new Vector2(0.60f, 0.72f), Vector2.zero, Vector2.zero);

            logText = UiFactory.CreateText(canvas.transform, "Mine Log", string.Empty, 14, TextAnchor.LowerLeft, new Color(0.88f, 0.92f, 0.96f, 0.92f));
            UiFactory.SetRect(logText.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.42f, 0.17f), Vector2.zero, Vector2.zero);
        }

        private void RefreshHud()
        {
            if (statusText == null)
            {
                return;
            }

            var ore = runtime.InventoryService.GetQuantity("ore");
            var mode = isAutoMode ? "Auto" : "Manual";
            var activity = runtime.State.SaveData.currentActivity;
            var floor = isClimbing ? "Climbing" : upperFloor ? "Upper" : "Lower";
            var nearRock = activity == ZoneActivity.Mining.ToString() && !IsNearRock() && !isClimbing ? "    Move near rock" : string.Empty;
            statusText.text = $"{mode}    Last: {activity}    {floor} Floor    Ore: {ore}    U: Quests{nearRock}";
            promptText.text = isClimbing ? "Climbing..." : IsNearVillagePortal() ? "Press F: Village" : IsNearRope() ? "Press F" : string.Empty;
            autoButton.GetComponentInChildren<Text>().text = isAutoMode ? "Auto" : "Manual";
            actionButton.interactable = !isClimbing && !isAutoMode && Time.time >= nextManualMineTime;
        }

        private void AddLog(string message)
        {
            if (logText == null)
            {
                return;
            }

            logLines.Insert(0, message);
            if (logLines.Count > 4)
            {
                logLines.RemoveAt(logLines.Count - 1);
            }

            logText.text = string.Join("\n", logLines);
        }

        private void OnResourceGathered()
        {
            playerVisual?.PlayGather();
        }

        private static GameObject CreateSprite(string name, Vector3 position, Vector3 scale, Color32 color, int sortingOrder, Sprite sprite = null)
        {
            var instance = new GameObject(name);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            var renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : CreateSolidSprite(color);
            renderer.sortingOrder = sortingOrder;
            return instance;
        }

        private GameObject CreateResourceSprite(string name, string nodeId, Vector3 position, Vector3 scale, Color32 fallbackColor, int sortingOrder)
        {
            var visual = GetZoneVisual();
            var slotType = GetResourceVisualSlot(nodeId);
            var visualScale = GetVisualScale(visual, slotType);
            var sizeScale = GetVisualSizeScale(visual, slotType);
            var visualPosition = position + Vector3.up * GetVisualYOffset(visual, slotType);
            var visualSize = Vector3.Scale(scale * visualScale, new Vector3(sizeScale.x, sizeScale.y, 1f));
            var sprite = GetVisualSprite(visual, slotType);
            if (sprite == null)
            {
                sprite = GetResourceNodeSprite(nodeId);
            }

            return CreateSprite(name, visualPosition, visualSize, fallbackColor, sortingOrder, sprite);
        }

        private static ZoneVisualSlotType GetResourceVisualSlot(string nodeId)
        {
            return nodeId == "rock" ? ZoneVisualSlotType.RockResource : ZoneVisualSlotType.TreeResource;
        }

        private Sprite GetResourceNodeSprite(string nodeId)
        {
            var zone = runtime.State.CurrentZone;
            if (zone == null)
            {
                return null;
            }

            foreach (var node in zone.ResourceNodes)
            {
                if (node != null && node.nodeId == nodeId)
                {
                    return node.icon;
                }
            }

            return null;
        }

        private void CreatePortal()
        {
            var visual = GetZoneVisual();
            var portalSprite = GetVisualSprite(visual, ZoneVisualSlotType.Portal);
            var ring = CreateSprite("Village Portal", VillagePortalPosition, new Vector3(0.72f, 1.15f, 1f), new Color32(86, 139, 212, 255), 3, portalSprite);
            ring.transform.SetParent(transform, false);

            if (portalSprite == null)
            {
                var core = CreateSprite("Village Portal Core", VillagePortalPosition, new Vector3(0.46f, 0.88f, 1f), new Color32(45, 54, 78, 255), 4);
                core.transform.SetParent(transform, false);
            }

            CreateWorldLabel("Village Portal Label", "Village\nPress F", VillagePortalPosition + new Vector3(0f, 0.98f, 0f), 30, Color.white);
        }

        private static Sprite CreateSolidSprite(Color32 color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private ZoneVisualDefinition GetZoneVisual()
        {
            var zone = runtime != null && runtime.State != null ? runtime.State.CurrentZone : null;
            return zone != null ? zone.Visual : null;
        }

        private static Sprite GetVisualSprite(ZoneVisualDefinition visual, ZoneVisualSlotType slotType)
        {
            return visual != null ? visual.GetSprite(slotType) : null;
        }

        private void CreateTilemapOrSprite(ZoneVisualDefinition visual, string name, ZoneVisualSlotType slotType, Vector3 position, Vector2 size, Color32 fallbackColor, int sortingOrder)
        {
            var tilemap = visual != null ? visual.TilemapDefinition : null;
            var visualScale = GetVisualScale(visual, slotType);
            var sizeScale = GetVisualSizeScale(visual, slotType);
            var visualPosition = position + Vector3.up * GetVisualYOffset(visual, slotType);
            var scaledSize = Vector2.Scale(size * visualScale, sizeScale);
            var tilemapPosition = slotType == ZoneVisualSlotType.Background
                ? visualPosition
                : visualPosition + Vector3.up * GetGroundYOffset(visual);
            if (tilemap != null && RuntimeTilemapBuilder.TryCreateFilledTilemap(transform, $"{name} Tilemap", GetTile(tilemap, slotType), tilemapPosition, scaledSize, sortingOrder, tilemap.TileSize, slotType != ZoneVisualSlotType.Background))
            {
                return;
            }

            var sprite = GetVisualSprite(visual, slotType);
            CreateSprite(name, visualPosition, GetSpriteScale(slotType, sprite, scaledSize), fallbackColor, sortingOrder, sprite);
        }

        private static Vector3 GetSpriteScale(ZoneVisualSlotType slotType, Sprite sprite, Vector2 targetSize)
        {
            if (slotType != ZoneVisualSlotType.Background || sprite == null)
            {
                return new Vector3(targetSize.x, targetSize.y, 1f);
            }

            var spriteSize = sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return new Vector3(targetSize.x, targetSize.y, 1f);
            }

            var uniformScale = Mathf.Max(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y);
            return new Vector3(uniformScale, uniformScale, 1f);
        }

        private static float GetGroundYOffset(ZoneVisualDefinition visual)
        {
            return visual != null ? visual.GroundYOffset : 0f;
        }

        private static float GetVisualScale(ZoneVisualDefinition visual, ZoneVisualSlotType slotType)
        {
            return visual != null ? visual.GetScale(slotType) : 1f;
        }

        private static Vector2 GetVisualSizeScale(ZoneVisualDefinition visual, ZoneVisualSlotType slotType)
        {
            return visual != null ? visual.GetSizeScale(slotType) : Vector2.one;
        }

        private static float GetVisualYOffset(ZoneVisualDefinition visual, ZoneVisualSlotType slotType)
        {
            return visual != null ? visual.GetYOffset(slotType) : 0f;
        }

        private static UnityEngine.Tilemaps.TileBase GetTile(ZoneTilemapDefinition tilemap, ZoneVisualSlotType slotType)
        {
            if (slotType == ZoneVisualSlotType.Background)
            {
                return tilemap.BackgroundTile;
            }

            return slotType == ZoneVisualSlotType.UpperPlatform ? tilemap.UpperPlatformTile : tilemap.LowerGroundTile;
        }

        private static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 3.75f;
            camera.transform.position = new Vector3(0f, -0.25f, -10f);
        }

        private void CreateWorldLabel(string name, string text, Vector3 position, int fontSize, Color color)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.position = position;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = fontSize;
            label.characterSize = 0.035f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            label.GetComponent<MeshRenderer>().sortingOrder = 8;
        }

        private void OnDestroy()
        {
            playerVisual?.Dispose();

            if (runtime == null || runtime.GatheringService == null)
            {
                return;
            }

            runtime.GatheringService.LogAdded -= AddLog;
            runtime.GatheringService.Changed -= RefreshHud;
            runtime.GatheringService.ResourceGathered -= OnResourceGathered;
        }
    }
}
