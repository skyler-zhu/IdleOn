using System.Collections.Generic;
using IdleOnLike.Core;
using UnityEngine;
using UnityEngine.UI;
using UiFactory = IdleOnLike.UI.RuntimeUiFactory;

namespace IdleOnLike.World
{
    public sealed class MineCaveController : MonoBehaviour
    {
        private const float MoveSpeed = 3.1f;
        private const float ManualMineSeconds = 2f;
        private const float JumpDuration = 0.72f;
        private const float JumpHeight = 1.12f;
        private const float AttackSeconds = 0.16f;
        private const float ClimbSeconds = 1.25f;
        private static readonly Vector3 LowerSpawn = new Vector3(-3.2f, -1.15f, 0f);
        private static readonly Vector3 UpperSpawn = new Vector3(-1.2f, 1.05f, 0f);
        private static readonly Vector3 RopePosition = new Vector3(0.2f, -0.05f, 0f);
        private static readonly Vector3 LowerRopePoint = new Vector3(RopePosition.x, LowerSpawn.y, 0f);
        private static readonly Vector3 UpperRopePoint = new Vector3(RopePosition.x, UpperSpawn.y, 0f);
        private static readonly Vector3 LowerRockPosition = new Vector3(2.75f, -1.05f, 0f);
        private static readonly Vector3 UpperRockPosition = new Vector3(3.15f, 1.15f, 0f);

        private readonly List<string> logLines = new List<string>();
        private GameRuntime runtime;
        private Transform playerTransform;
        private GameObject attackFlash;
        private Text statusText;
        private Text promptText;
        private Text logText;
        private Button autoButton;
        private Button actionButton;
        private bool isAutoMode = true;
        private bool upperFloor;
        private float nextManualMineTime;
        private float jumpRemainingSeconds;
        private float attackRemainingSeconds;
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
            BuildWorld();
            BuildHud();
            runtime.GatheringService.LogAdded += AddLog;
            runtime.GatheringService.Changed += RefreshHud;
            runtime.GatheringService.StartGathering("rock", IsNearRock());
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

            if (Input.GetKeyDown(KeyCode.F) && IsNearRope())
            {
                StartClimb();
                RefreshHud();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                Attack();
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

            UpdateActionVisuals();
            runtime.GatheringService.Tick(isAutoMode ? Time.deltaTime : 0f, isAutoMode && IsNearRock());
            RefreshHud();
        }

        private void MoveManual(float horizontal)
        {
            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                return;
            }

            var position = playerTransform.position;
            position.x = Mathf.Clamp(position.x + horizontal * MoveSpeed * Time.deltaTime, -3.8f, 3.6f);
            playerTransform.position = position;
            playerTransform.localScale = new Vector3(horizontal < 0f ? -1f : 1f, 1f, 1f);
        }

        private void MoveAuto()
        {
            var target = upperFloor ? UpperRockPosition : LowerRockPosition;
            var position = playerTransform.position;
            position.x = Vector3.MoveTowards(position, target, MoveSpeed * Time.deltaTime).x;
            playerTransform.position = position;
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
            jumpRemainingSeconds = 0f;
            attackRemainingSeconds = 0f;
            if (attackFlash != null)
            {
                attackFlash.SetActive(false);
            }

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

            runtime.GatheringService.GatherOnce(IsNearRock());
            nextManualMineTime = Time.time + ManualMineSeconds;
        }

        private bool IsNearRope()
        {
            return Mathf.Abs(playerTransform.position.x - RopePosition.x) <= 0.75f;
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
            CreateSprite("Cave Backdrop", new Vector3(0f, 0f, 0.5f), new Vector3(9f, 5.5f, 1f), new Color32(39, 42, 52, 255), -20);
            CreateSprite("Lower Platform", new Vector3(0f, -1.55f, 0f), new Vector3(8.2f, 0.28f, 1f), new Color32(86, 80, 78, 255), -8);
            CreateSprite("Upper Platform", new Vector3(1.1f, 0.65f, 0f), new Vector3(5.4f, 0.24f, 1f), new Color32(93, 86, 84, 255), -8);
            CreateSprite("Rope", RopePosition, new Vector3(0.12f, 2.5f, 1f), new Color32(176, 139, 82, 255), 0);
            CreateSprite("Lower Rock", LowerRockPosition, new Vector3(0.78f, 0.62f, 1f), new Color32(113, 128, 151, 255), 1);
            CreateSprite("Upper Rock", UpperRockPosition, new Vector3(0.62f, 0.54f, 1f), new Color32(121, 138, 166, 255), 1);

            var player = CreateSprite("Mine Player", LowerSpawn, new Vector3(1f, 1f, 1f), new Color32(82, 168, 255, 255), 5);
            var renderer = player.GetComponent<SpriteRenderer>();
            renderer.sprite = runtime.State.Character != null ? runtime.State.Character.IdleSprite : renderer.sprite;
            playerTransform = player.transform;

            attackFlash = CreateSprite("Mine Attack Flash", new Vector3(0.62f, 0.10f, 0f), new Vector3(0.34f, 0.10f, 1f), new Color32(255, 236, 128, 255), 6);
            attackFlash.transform.SetParent(player.transform, false);
            attackFlash.SetActive(false);
        }

        private void BuildHud()
        {
            var canvas = UiFactory.CreateCanvas("Mine Cave HUD");
            var topBar = UiFactory.CreatePanel(canvas.transform, "Top Bar", new Vector2(0f, 0.88f), Vector2.one, Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.10f, 0.92f));
            statusText = UiFactory.CreateText(topBar, "Status", string.Empty, 19, TextAnchor.MiddleLeft, Color.white);
            UiFactory.SetRect(statusText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.44f, 1f), Vector2.zero, Vector2.zero);

            autoButton = UiFactory.CreateButton(topBar, "Auto Manual Button", "Auto", new Color(0.22f, 0.36f, 0.48f, 1f));
            UiFactory.SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(0.45f, 0.20f), new Vector2(0.56f, 0.80f), Vector2.zero, Vector2.zero);
            autoButton.onClick.AddListener(ToggleAutoMode);

            actionButton = UiFactory.CreateButton(topBar, "Mine Button", "Mine (J)", new Color(0.48f, 0.34f, 0.18f, 1f));
            UiFactory.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.57f, 0.20f), new Vector2(0.68f, 0.80f), Vector2.zero, Vector2.zero);
            actionButton.onClick.AddListener(() =>
            {
                Attack();
                MineManual();
            });

            var inventoryPanel = new IdleOnLike.UI.InventoryEquipmentPanel(runtime, canvas.transform);
            var inventoryButton = UiFactory.CreateButton(topBar, "Inventory Button", "Inventory", new Color(0.26f, 0.30f, 0.46f, 1f));
            UiFactory.SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0.20f), new Vector2(0.80f, 0.80f), Vector2.zero, Vector2.zero);
            inventoryButton.onClick.AddListener(inventoryPanel.Toggle);

            var returnButton = UiFactory.CreateButton(topBar, "Return Village Button", "Village", new Color(0.24f, 0.38f, 0.58f, 1f));
            UiFactory.SetRect(returnButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.20f), new Vector2(0.97f, 0.80f), Vector2.zero, Vector2.zero);
            returnButton.onClick.AddListener(() => runtime.ReturnToVillage());

            promptText = UiFactory.CreateText(canvas.transform, "Rope Prompt", string.Empty, 22, TextAnchor.MiddleCenter, Color.white);
            UiFactory.SetRect(promptText.rectTransform, new Vector2(0.40f, 0.64f), new Vector2(0.60f, 0.72f), Vector2.zero, Vector2.zero);

            var logPanel = UiFactory.CreatePanel(canvas.transform, "Mine Log Panel", new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.16f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.09f, 0.88f));
            logText = UiFactory.CreateText(logPanel, "Mine Log", string.Empty, 14, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.96f));
            UiFactory.Stretch(logText.rectTransform, new Vector2(18f, 14f), new Vector2(-18f, -14f));
        }

        private void RefreshHud()
        {
            if (statusText == null)
            {
                return;
            }

            var ore = runtime.InventoryService.GetQuantity("ore");
            var mode = isAutoMode ? "Auto" : "Manual";
            var floor = isClimbing ? "Climbing" : upperFloor ? "Upper" : "Lower";
            var nearRock = IsNearRock() || isClimbing ? string.Empty : "    Move near rock";
            statusText.text = $"{mode} Mining    {floor} Floor    Ore: {ore}{nearRock}";
            promptText.text = isClimbing ? "Climbing..." : IsNearRope() ? "Press F" : string.Empty;
            autoButton.GetComponentInChildren<Text>().text = isAutoMode ? "Auto" : "Manual";
            actionButton.interactable = !isClimbing && !isAutoMode && Time.time >= nextManualMineTime;
        }

        private void Jump()
        {
            if (isClimbing)
            {
                return;
            }

            if (jumpRemainingSeconds > 0f)
            {
                return;
            }

            jumpRemainingSeconds = JumpDuration;
        }

        private void Attack()
        {
            if (isClimbing)
            {
                return;
            }

            attackRemainingSeconds = AttackSeconds;
            if (attackFlash != null)
            {
                attackFlash.SetActive(true);
            }
        }

        private void UpdateActionVisuals()
        {
            if (isClimbing)
            {
                if (attackFlash != null)
                {
                    attackFlash.SetActive(false);
                }

                return;
            }

            if (jumpRemainingSeconds > 0f)
            {
                jumpRemainingSeconds = Mathf.Max(0f, jumpRemainingSeconds - Time.deltaTime);
            }

            if (attackRemainingSeconds > 0f)
            {
                attackRemainingSeconds = Mathf.Max(0f, attackRemainingSeconds - Time.deltaTime);
            }

            if (attackFlash != null)
            {
                attackFlash.SetActive(attackRemainingSeconds > 0f);
            }

            var position = playerTransform.position;
            var baseY = upperFloor ? UpperSpawn.y : LowerSpawn.y;
            var jumpProgress = jumpRemainingSeconds > 0f ? Mathf.Clamp01(1f - jumpRemainingSeconds / JumpDuration) : 0f;
            position.y = baseY + Mathf.Sin(jumpProgress * Mathf.PI) * JumpHeight;
            playerTransform.position = position;
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

        private static GameObject CreateSprite(string name, Vector3 position, Vector3 scale, Color32 color, int sortingOrder)
        {
            var instance = new GameObject(name);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            var renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(color);
            renderer.sortingOrder = sortingOrder;
            return instance;
        }

        private static Sprite CreateSolidSprite(Color32 color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void OnDestroy()
        {
            if (runtime == null || runtime.GatheringService == null)
            {
                return;
            }

            runtime.GatheringService.LogAdded -= AddLog;
            runtime.GatheringService.Changed -= RefreshHud;
        }
    }
}
