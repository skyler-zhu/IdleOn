using IdleOnLike.Core;
using IdleOnLike.Data;
using UnityEngine;
using UnityEngine.UI;
using UiFactory = IdleOnLike.UI.RuntimeUiFactory;

namespace IdleOnLike.World
{
    public sealed class VillageView : MonoBehaviour
    {
        private const float MoveSpeed = 3.2f;
        private const float PlayerMinX = -7.2f;
        private const float PlayerMaxX = 7.2f;
        private const float NpcX = 2.35f;
        private const float AnvilX = 4.25f;
        private const float MerchantX = 5.65f;
        private const float InteractDistance = 1.05f;
        private const float LowerFloorY = -1.75f;
        private const float UpperFloorY = 1.05f;
        private const float RopeX = 0.35f;
        private const float ForestPortalX = -6.1f;
        private const float MinePortalX = -6.1f;
        private const float JumpDuration = 0.72f;
        private const float JumpHeight = 1.12f;
        private const float AttackSeconds = 0.16f;
        private const float ClimbSeconds = 1.2f;

        private GameRuntime runtime;
        private Transform playerTransform;
        private GameObject attackFlash;
        private GameObject promptCanvas;
        private Text promptText;
        private GameObject dialogueCanvas;
        private Text dialogueBody;
        private Button actionButton;
        private IdleOnLike.UI.CraftingPanel craftingPanel;
        private IdleOnLike.UI.ShopPanel shopPanel;
        private bool upperFloor;
        private bool isClimbing;
        private bool climbTargetUpperFloor;
        private float climbElapsedSeconds;
        private Vector3 climbStartPosition;
        private Vector3 climbEndPosition;
        private float jumpRemainingSeconds;
        private float attackRemainingSeconds;

        public static VillageView Create(GameRuntime runtime)
        {
            var viewObject = new GameObject("Village View");
            var view = viewObject.AddComponent<VillageView>();
            view.Initialize(runtime);
            return view;
        }

        private void Initialize(GameRuntime gameRuntime)
        {
            runtime = gameRuntime;
            ConfigureCamera();
            BuildGround();
            BuildPlayer();
            BuildNpc();
            BuildAnvilNpc();
            BuildMerchantNpc();
            BuildPortals();
            BuildPrompt();
            BuildDialogue();
            BuildUtilityPanels();
        }

        private void Update()
        {
            if (runtime == null || runtime.State == null || playerTransform == null)
            {
                return;
            }

            if (isClimbing)
            {
                UpdateClimb();
                UpdatePrompt();
                return;
            }

            var horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                var position = playerTransform.position;
                position.x = Mathf.Clamp(position.x + horizontal * MoveSpeed * Time.deltaTime, PlayerMinX, PlayerMaxX);
                playerTransform.position = position;
                playerTransform.localScale = new Vector3(horizontal < 0f ? -1f : 1f, 1f, 1f);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                Attack();
            }

            UpdateActionVisuals();

            UpdatePrompt();
            if (Input.GetKeyDown(KeyCode.F))
            {
                Interact();
            }
        }

        private bool IsNearNpc()
        {
            return !upperFloor && Mathf.Abs(playerTransform.position.x - NpcX) <= InteractDistance;
        }

        private bool IsNearAnvil()
        {
            return !upperFloor && Mathf.Abs(playerTransform.position.x - AnvilX) <= InteractDistance;
        }

        private bool IsNearMerchant()
        {
            return !upperFloor && Mathf.Abs(playerTransform.position.x - MerchantX) <= InteractDistance;
        }

        private bool IsNearRope()
        {
            return Mathf.Abs(playerTransform.position.x - RopeX) <= 0.75f;
        }

        private bool IsNearForestPortal()
        {
            return !upperFloor && Mathf.Abs(playerTransform.position.x - ForestPortalX) <= 0.95f;
        }

        private bool IsNearMinePortal()
        {
            return upperFloor && Mathf.Abs(playerTransform.position.x - MinePortalX) <= 0.95f;
        }

        private bool IsMineUnlocked()
        {
            var secondCharacter = runtime.Catalog.PlayableCharacters.Count > 1 ? runtime.Catalog.PlayableCharacters[1] : null;
            var isSecondCharacter = secondCharacter != null && runtime.State.SaveData.characterId == secondCharacter.Id;
            return isSecondCharacter || runtime.QuestService.IsQuestCompleted("learn_to_chop");
        }

        private void Interact()
        {
            if (IsNearForestPortal())
            {
                runtime.TravelToForest();
                return;
            }

            if (IsNearMinePortal())
            {
                if (IsMineUnlocked())
                {
                    runtime.TravelToMineCave();
                }

                return;
            }

            if (IsNearRope())
            {
                StartClimb();
                return;
            }

            if (IsNearNpc())
            {
                ToggleDialogue();
                return;
            }

            if (IsNearAnvil())
            {
                craftingPanel.Toggle();
                return;
            }

            if (IsNearMerchant())
            {
                shopPanel.Toggle();
            }
        }

        private void UpdatePrompt()
        {
            if (promptCanvas == null || promptText == null)
            {
                return;
            }

            var label = string.Empty;
            if (isClimbing)
            {
                label = "Climbing...";
            }
            else if (IsNearForestPortal())
            {
                label = "Press F: Forest";
            }
            else if (IsNearMinePortal())
            {
                label = IsMineUnlocked() ? "Press F: Mine Cave" : "Mine locked";
            }
            else if (IsNearRope())
            {
                label = upperFloor ? "Press F: Climb down" : "Press F: Climb up";
            }
            else if (IsNearNpc() && !dialogueCanvas.activeSelf)
            {
                label = "Press F: Talk";
            }
            else if (IsNearAnvil())
            {
                label = "Press F: Anvil";
            }
            else if (IsNearMerchant())
            {
                label = "Press F: Merchant";
            }

            promptText.text = label;
            promptCanvas.SetActive(!string.IsNullOrEmpty(label));
        }

        private void ToggleDialogue()
        {
            dialogueCanvas.SetActive(!dialogueCanvas.activeSelf);
            if (dialogueCanvas.activeSelf)
            {
                RefreshDialogue();
            }
        }

        private void RefreshDialogue()
        {
            var questService = runtime.QuestService;
            var quest = questService.GetNextActionableQuest();
            if (quest == null)
            {
                dialogueBody.text = "Scripticus\n\nNo new errands right now.";
                actionButton.gameObject.SetActive(false);
                return;
            }

            dialogueBody.text = BuildQuestText(quest);
            actionButton.gameObject.SetActive(true);
            actionButton.onClick.RemoveAllListeners();

            if (!questService.IsQuestActive(quest.Id))
            {
                SetAction("Accept", true, () => questService.AcceptQuest(quest.Id));
            }
            else
            {
                SetAction("Complete", questService.CanComplete(quest.Id), () => questService.CompleteQuest(quest.Id));
            }
        }

        private void SetAction(string label, bool interactable, UnityEngine.Events.UnityAction action)
        {
            actionButton.GetComponentInChildren<Text>().text = label;
            actionButton.interactable = interactable;
            actionButton.onClick.AddListener(() =>
            {
                action();
                runtime.Save();
                RefreshDialogue();
            });
        }

        private string BuildQuestText(QuestDefinition quest)
        {
            var text = $"Scripticus\n\n{quest.Title}\n{quest.Description}";
            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var progress = runtime.QuestService.GetObjectiveProgress(quest.Id, i);
                var label = string.IsNullOrEmpty(objective.displayText)
                    ? $"{objective.objectiveType}: {objective.targetId}"
                    : objective.displayText;
                text += $"\n\n{label}: {progress}/{objective.requiredAmount}";
            }

            return text;
        }

        private void BuildGround()
        {
            var sky = CreateSpriteObject("Village Sky", new Vector3(0f, 0.2f, 0.4f), new Vector3(16f, 7.2f, 1f), new Color32(108, 164, 196, 255), -20);
            sky.transform.SetParent(transform, false);

            var ground = CreateSpriteObject("Village Ground", new Vector3(0f, LowerFloorY - 0.57f, 0f), new Vector3(15.6f, 0.36f, 1f), new Color32(73, 104, 65, 255), -10);
            ground.transform.SetParent(transform, false);

            var path = CreateSpriteObject("Village Path", new Vector3(0f, LowerFloorY - 0.33f, 0f), new Vector3(14.2f, 0.24f, 1f), new Color32(147, 123, 82, 255), -9);
            path.transform.SetParent(transform, false);

            var upperPlatform = CreateSpriteObject("Village Upper Platform", new Vector3(0f, UpperFloorY - 0.57f, 0f), new Vector3(14.1f, 0.30f, 1f), new Color32(103, 116, 88, 255), -8);
            upperPlatform.transform.SetParent(transform, false);

            var upperPath = CreateSpriteObject("Village Upper Path", new Vector3(0f, UpperFloorY - 0.35f, 0f), new Vector3(12.8f, 0.18f, 1f), new Color32(153, 132, 91, 255), -7);
            upperPath.transform.SetParent(transform, false);

            var rope = CreateSpriteObject("Village Rope", new Vector3(RopeX, (LowerFloorY + UpperFloorY) * 0.5f, 0f), new Vector3(0.12f, 2.8f, 1f), new Color32(176, 139, 82, 255), 1);
            rope.transform.SetParent(transform, false);
        }

        private void BuildPlayer()
        {
            var playerObject = new GameObject("Village Player");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = new Vector3(-2.5f, LowerFloorY, 0f);
            playerTransform = playerObject.transform;

            var renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = runtime.State.Character != null ? runtime.State.Character.IdleSprite : null;
            if (renderer.sprite == null)
            {
                renderer.sprite = CreateSolidSprite(new Color32(82, 168, 255, 255));
            }

            renderer.sortingOrder = 5;

            attackFlash = CreateSpriteObject("Village Attack Flash", new Vector3(0.62f, 0.10f, 0f), new Vector3(0.34f, 0.10f, 1f), new Color32(255, 236, 128, 255), 6);
            attackFlash.transform.SetParent(playerObject.transform, false);
            attackFlash.SetActive(false);
        }

        private void Jump()
        {
            if (jumpRemainingSeconds > 0f)
            {
                return;
            }

            jumpRemainingSeconds = JumpDuration;
        }

        private void Attack()
        {
            attackRemainingSeconds = AttackSeconds;
            if (attackFlash != null)
            {
                attackFlash.SetActive(true);
            }
        }

        private void UpdateActionVisuals()
        {
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
            var baseY = upperFloor ? UpperFloorY : LowerFloorY;
            var jumpProgress = jumpRemainingSeconds > 0f ? Mathf.Clamp01(1f - jumpRemainingSeconds / JumpDuration) : 0f;
            position.y = baseY + Mathf.Sin(jumpProgress * Mathf.PI) * JumpHeight;
            playerTransform.position = position;
        }

        private void BuildNpc()
        {
            var npc = CreateSpriteObject("Quest NPC Scripticus", new Vector3(NpcX, LowerFloorY + 0.03f, 0f), new Vector3(0.62f, 0.92f, 1f), new Color32(238, 205, 117, 255), 4);
            npc.transform.SetParent(transform, false);

            var marker = CreateSpriteObject("NPC Marker", new Vector3(NpcX, LowerFloorY + 0.67f, 0f), new Vector3(0.22f, 0.22f, 1f), new Color32(255, 246, 135, 255), 6);
            marker.transform.SetParent(transform, false);
        }

        private void BuildAnvilNpc()
        {
            var anvil = CreateSpriteObject("Anvil NPC", new Vector3(AnvilX, LowerFloorY - 0.06f, 0f), new Vector3(0.72f, 0.48f, 1f), new Color32(118, 128, 142, 255), 4);
            anvil.transform.SetParent(transform, false);
            CreateWorldLabel("Anvil Label", "Anvil", new Vector3(AnvilX, LowerFloorY + 0.58f, 0f), 30, Color.white);
        }

        private void BuildMerchantNpc()
        {
            var merchant = CreateSpriteObject("Merchant NPC", new Vector3(MerchantX, LowerFloorY + 0.03f, 0f), new Vector3(0.62f, 0.92f, 1f), new Color32(102, 184, 156, 255), 4);
            merchant.transform.SetParent(transform, false);
            CreateWorldLabel("Merchant Label", "Merchant", new Vector3(MerchantX, LowerFloorY + 0.78f, 0f), 30, Color.white);
        }

        private void BuildPortals()
        {
            CreatePortal("Forest Portal", new Vector3(ForestPortalX, LowerFloorY + 0.12f, 0f), "Forest", new Color32(67, 169, 84, 255));
            CreatePortal("Mine Cave Portal", new Vector3(MinePortalX, UpperFloorY + 0.12f, 0f), "Mine Cave", new Color32(118, 139, 175, 255));
        }

        private void CreatePortal(string name, Vector3 position, string label, Color32 color)
        {
            var ring = CreateSpriteObject(name, position, new Vector3(0.72f, 1.15f, 1f), color, 3);
            ring.transform.SetParent(transform, false);

            var core = CreateSpriteObject($"{name} Core", position, new Vector3(0.46f, 0.88f, 1f), new Color32(58, 62, 86, 255), 4);
            core.transform.SetParent(transform, false);

            CreateWorldLabel($"{name} Label", label, position + new Vector3(0f, 0.86f, 0f), 34, Color.white);
        }

        private void BuildPrompt()
        {
            var canvas = UiFactory.CreateCanvas("Village Interaction Prompt");
            promptCanvas = canvas.gameObject;
            var panel = UiFactory.CreatePanel(canvas.transform, "Prompt Panel", new Vector2(0.40f, 0.68f), new Vector2(0.60f, 0.76f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.09f, 0.88f));
            promptText = UiFactory.CreateText(panel, "Prompt Text", "Press F", 22, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(promptText.rectTransform, Vector2.zero, Vector2.zero);
            promptCanvas.SetActive(false);
        }

        private void BuildUtilityPanels()
        {
            var canvas = UiFactory.CreateCanvas("Village Utility Panels");
            craftingPanel = new IdleOnLike.UI.CraftingPanel(runtime, canvas.transform);
            shopPanel = new IdleOnLike.UI.ShopPanel(runtime, canvas.transform);
        }

        private void StartClimb()
        {
            if (isClimbing)
            {
                return;
            }

            climbTargetUpperFloor = !upperFloor;
            climbElapsedSeconds = 0f;
            climbStartPosition = new Vector3(RopeX, upperFloor ? UpperFloorY : LowerFloorY, 0f);
            climbEndPosition = new Vector3(RopeX, climbTargetUpperFloor ? UpperFloorY : LowerFloorY, 0f);
            playerTransform.position = climbStartPosition;
            jumpRemainingSeconds = 0f;
            attackRemainingSeconds = 0f;
            if (attackFlash != null)
            {
                attackFlash.SetActive(false);
            }

            isClimbing = true;
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
            playerTransform.position = climbEndPosition;
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

        private void BuildDialogue()
        {
            var canvas = UiFactory.CreateCanvas("Village NPC Dialogue");
            dialogueCanvas = canvas.gameObject;
            var panel = UiFactory.CreatePanel(canvas.transform, "Dialogue Panel", new Vector2(0.30f, 0.20f), new Vector2(0.70f, 0.66f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.09f, 0.96f));
            dialogueBody = UiFactory.CreateText(panel, "Dialogue Body", string.Empty, 18, TextAnchor.UpperLeft, new Color(0.90f, 0.93f, 0.96f));
            UiFactory.SetRect(dialogueBody.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            actionButton = UiFactory.CreateButton(panel, "Quest Action Button", "Accept", new Color(0.22f, 0.42f, 0.72f, 1f));
            UiFactory.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.07f), new Vector2(0.50f, 0.18f), Vector2.zero, Vector2.zero);

            var closeButton = UiFactory.CreateButton(panel, "Close Button", "Close", new Color(0.32f, 0.32f, 0.36f, 1f));
            UiFactory.SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.56f, 0.07f), new Vector2(0.88f, 0.18f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(() => dialogueCanvas.SetActive(false));
            dialogueCanvas.SetActive(false);
        }

        private static GameObject CreateSpriteObject(string name, Vector3 position, Vector3 scale, Color32 color, int sortingOrder)
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

        private static void CreateWorldLabel(string name, string text, Vector3 position, int fontSize, Color color)
        {
            var labelObject = new GameObject(name);
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
    }
}
