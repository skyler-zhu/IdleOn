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
        private const float PlayerMinX = -4.2f;
        private const float PlayerMaxX = 4.2f;
        private const float NpcX = 1.65f;
        private const float InteractDistance = 1.05f;
        private const float PlayerGroundY = -1.05f;
        private const float JumpDuration = 0.72f;
        private const float JumpHeight = 1.12f;
        private const float AttackSeconds = 0.16f;

        private GameRuntime runtime;
        private Transform playerTransform;
        private GameObject attackFlash;
        private GameObject promptCanvas;
        private GameObject dialogueCanvas;
        private Text dialogueBody;
        private Button actionButton;
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
            BuildGround();
            BuildPlayer();
            BuildNpc();
            BuildPrompt();
            BuildDialogue();
        }

        private void Update()
        {
            if (runtime == null || runtime.State == null || playerTransform == null)
            {
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

            var nearNpc = IsNearNpc();
            promptCanvas.SetActive(nearNpc && !dialogueCanvas.activeSelf);
            if (nearNpc && Input.GetKeyDown(KeyCode.F))
            {
                ToggleDialogue();
            }
            else if (!nearNpc && dialogueCanvas.activeSelf)
            {
                dialogueCanvas.SetActive(false);
            }
        }

        private bool IsNearNpc()
        {
            return Mathf.Abs(playerTransform.position.x - NpcX) <= InteractDistance;
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
            var sky = CreateSpriteObject("Village Sky", new Vector3(0f, 0.8f, 0.4f), new Vector3(9f, 4.2f, 1f), new Color32(108, 164, 196, 255), -20);
            sky.transform.SetParent(transform, false);

            var ground = CreateSpriteObject("Village Ground", new Vector3(0f, -1.62f, 0f), new Vector3(9f, 0.36f, 1f), new Color32(73, 104, 65, 255), -10);
            ground.transform.SetParent(transform, false);

            var path = CreateSpriteObject("Village Path", new Vector3(0f, -1.38f, 0f), new Vector3(7.4f, 0.24f, 1f), new Color32(147, 123, 82, 255), -9);
            path.transform.SetParent(transform, false);
        }

        private void BuildPlayer()
        {
            var playerObject = new GameObject("Village Player");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = new Vector3(-2.5f, PlayerGroundY, 0f);
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
            var jumpProgress = jumpRemainingSeconds > 0f ? Mathf.Clamp01(1f - jumpRemainingSeconds / JumpDuration) : 0f;
            position.y = PlayerGroundY + Mathf.Sin(jumpProgress * Mathf.PI) * JumpHeight;
            playerTransform.position = position;
        }

        private void BuildNpc()
        {
            var npc = CreateSpriteObject("Quest NPC Scripticus", new Vector3(NpcX, -1.02f, 0f), new Vector3(0.62f, 0.92f, 1f), new Color32(238, 205, 117, 255), 4);
            npc.transform.SetParent(transform, false);

            var marker = CreateSpriteObject("NPC Marker", new Vector3(NpcX, -0.38f, 0f), new Vector3(0.22f, 0.22f, 1f), new Color32(255, 246, 135, 255), 6);
            marker.transform.SetParent(transform, false);
        }

        private void BuildPrompt()
        {
            var canvas = UiFactory.CreateCanvas("Village Interaction Prompt");
            promptCanvas = canvas.gameObject;
            var panel = UiFactory.CreatePanel(canvas.transform, "Prompt Panel", new Vector2(0.40f, 0.68f), new Vector2(0.60f, 0.76f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.09f, 0.88f));
            var text = UiFactory.CreateText(panel, "Prompt Text", "Press F", 22, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            promptCanvas.SetActive(false);
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
    }
}
