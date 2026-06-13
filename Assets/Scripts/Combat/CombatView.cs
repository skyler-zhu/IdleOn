using System.Collections;
using System.Collections.Generic;
using IdleOnLike.Core;
using UnityEngine;

namespace IdleOnLike.Combat
{
    public sealed class CombatView : MonoBehaviour
    {
        private readonly Dictionary<CombatEnemyInstance, EnemyView> enemyViews = new Dictionary<CombatEnemyInstance, EnemyView>();

        private GameRuntime runtime;
        private CombatService combatService;
        private Transform playerTransform;
        private SpriteRenderer playerRenderer;
        private Animator playerAnimator;
        private GameObject attackFlash;
        private GameObject treeObject;
        private GameObject villagePortalObject;

        public bool IsPlayerNearTree(Vector3 playerPosition)
        {
            return treeObject != null && Vector3.Distance(playerPosition, treeObject.transform.position) <= 1.35f;
        }

        public bool IsPlayerNearVillagePortal(Vector3 playerPosition)
        {
            return villagePortalObject != null && Vector3.Distance(playerPosition, villagePortalObject.transform.position) <= 1.25f;
        }

        public static CombatView Create(GameRuntime runtime, CombatService combatService)
        {
            var viewObject = new GameObject("Combat View");
            var view = viewObject.AddComponent<CombatView>();
            view.Initialize(runtime, combatService);
            return view;
        }

        private void Initialize(GameRuntime gameRuntime, CombatService service)
        {
            runtime = gameRuntime;
            combatService = service;

            ConfigureCamera();
            BuildGround();
            BuildVillagePortal();
            BuildPlayer();
            BuildTree();

            combatService.EnemySpawned += OnEnemySpawned;
            combatService.EnemyDamaged += OnEnemyDamaged;
            combatService.EnemyDefeated += OnEnemyDefeated;
            combatService.PlayerAttacked += OnPlayerAttacked;
            combatService.PlayerDamaged += OnPlayerDamaged;
            combatService.PlayerRestStarted += OnPlayerRestStarted;
            combatService.PlayerRestEnded += OnPlayerRestEnded;
        }

        private void BuildTree()
        {
            treeObject = new GameObject("Tree Resource Node");
            treeObject.transform.SetParent(transform, false);
            treeObject.transform.position = new Vector3(-5.65f, 1.25f, 0f);

            var nodeSprite = GetResourceNodeSprite("tree");
            if (nodeSprite != null)
            {
                treeObject.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                var renderer = treeObject.AddComponent<SpriteRenderer>();
                renderer.sprite = nodeSprite;
                renderer.sortingOrder = 1;
                return;
            }

            var trunk = new GameObject("Trunk");
            trunk.transform.SetParent(treeObject.transform, false);
            trunk.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            trunk.transform.localScale = new Vector3(0.18f, 0.72f, 1f);
            var trunkRenderer = trunk.AddComponent<SpriteRenderer>();
            trunkRenderer.sprite = CreateSolidSprite(new Color32(118, 82, 48, 255));
            trunkRenderer.sortingOrder = 1;

            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(treeObject.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            canopy.transform.localScale = new Vector3(0.72f, 0.52f, 1f);
            var canopyRenderer = canopy.AddComponent<SpriteRenderer>();
            canopyRenderer.sprite = CreateSolidSprite(new Color32(72, 142, 82, 255));
            canopyRenderer.sortingOrder = 1;
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

        private void Update()
        {
            if (playerTransform != null)
            {
                var jumpOffset = combatService.IsJumping
                    ? Mathf.Sin(combatService.JumpProgress * Mathf.PI) * 1.12f
                    : 0f;
                playerTransform.position = combatService.PlayerPosition + Vector3.up * jumpOffset;
                playerTransform.localScale = new Vector3(combatService.FacingSign, 1f, 1f);
            }

            foreach (var pair in enemyViews)
            {
                if (pair.Value.Root != null)
                {
                    pair.Value.Root.transform.position = pair.Key.currentPosition;
                }
            }
        }

        private void BuildGround()
        {
            var ground = new GameObject("Combat Ground");
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(0f, -1.95f, 0.1f);
            ground.transform.localScale = new Vector3(14f, 0.20f, 1f);
            var renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(new Color32(54, 86, 65, 255));
            renderer.sortingOrder = -10;

            var upper = new GameObject("Upper Forest Platform");
            upper.transform.SetParent(transform, false);
            upper.transform.position = new Vector3(0.8f, 0.62f, 0.1f);
            upper.transform.localScale = new Vector3(12.4f, 0.18f, 1f);
            var upperRenderer = upper.AddComponent<SpriteRenderer>();
            upperRenderer.sprite = CreateSolidSprite(new Color32(64, 104, 73, 255));
            upperRenderer.sortingOrder = -9;

            var rope = new GameObject("Forest Rope");
            rope.transform.SetParent(transform, false);
            rope.transform.position = new Vector3(0.15f, -0.1f, 0f);
            rope.transform.localScale = new Vector3(0.12f, 2.65f, 1f);
            var ropeRenderer = rope.AddComponent<SpriteRenderer>();
            ropeRenderer.sprite = CreateSolidSprite(new Color32(176, 139, 82, 255));
            ropeRenderer.sortingOrder = 1;
            CreateWorldLabel("Forest Rope Label", "Rope\nPress F", rope.transform.position + new Vector3(0f, 1.46f, 0f), 26, Color.white);
        }

        private void BuildVillagePortal()
        {
            villagePortalObject = new GameObject("Village Portal");
            villagePortalObject.transform.SetParent(transform, false);
            villagePortalObject.transform.position = new Vector3(6.05f, -1.22f, 0f);

            var ring = new GameObject("Village Portal Ring");
            ring.transform.SetParent(villagePortalObject.transform, false);
            ring.transform.localScale = new Vector3(0.72f, 1.15f, 1f);
            var ringRenderer = ring.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = CreateSolidSprite(new Color32(86, 139, 212, 255));
            ringRenderer.sortingOrder = 3;

            var core = new GameObject("Village Portal Core");
            core.transform.SetParent(villagePortalObject.transform, false);
            core.transform.localScale = new Vector3(0.46f, 0.88f, 1f);
            var coreRenderer = core.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = CreateSolidSprite(new Color32(45, 54, 78, 255));
            coreRenderer.sortingOrder = 4;

            CreateWorldLabel("Village Portal Label", "Village\nPress F", villagePortalObject.transform.position + new Vector3(0f, 0.98f, 0f), 30, Color.white);
        }

        private void BuildPlayer()
        {
            var playerObject = new GameObject("Player Visual");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = combatService.PlayerPosition;
            playerTransform = playerObject.transform;

            playerRenderer = playerObject.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = runtime.State.Character != null ? runtime.State.Character.IdleSprite : null;
            if (playerRenderer.sprite == null)
            {
                playerRenderer.sprite = CreateSolidSprite(new Color32(82, 168, 255, 255));
            }

            playerRenderer.sortingOrder = 5;

            playerAnimator = playerObject.AddComponent<Animator>();
            if (runtime.State.Character != null && runtime.State.Character.AnimatorController != null)
            {
                playerAnimator.runtimeAnimatorController = runtime.State.Character.AnimatorController;
            }

            attackFlash = new GameObject("Attack Flash");
            attackFlash.transform.SetParent(playerObject.transform, false);
            attackFlash.transform.localPosition = new Vector3(0.72f, 0.12f, 0f);
            attackFlash.transform.localScale = new Vector3(0.38f, 0.10f, 1f);
            var flashRenderer = attackFlash.AddComponent<SpriteRenderer>();
            flashRenderer.sprite = CreateSolidSprite(new Color32(255, 236, 128, 255));
            flashRenderer.sortingOrder = 6;
            attackFlash.SetActive(false);
        }

        private void OnEnemySpawned(CombatEnemyInstance enemy)
        {
            if (!enemyViews.TryGetValue(enemy, out var enemyView))
            {
                var root = new GameObject($"Enemy Visual - {enemy.enemyDefinition.DisplayName}");
                root.transform.SetParent(transform, false);
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 4;
                enemyView = new EnemyView { Root = root, Renderer = renderer };
                enemyViews.Add(enemy, enemyView);
                enemy.viewReference = enemyView;
            }

            enemyView.Root.name = $"Enemy Visual - {enemy.enemyDefinition.DisplayName}";
            enemyView.Root.transform.position = enemy.currentPosition;
            enemyView.Root.SetActive(true);
            enemyView.Renderer.sprite = enemy.enemyDefinition.IdleSprite != null
                ? enemy.enemyDefinition.IdleSprite
                : CreateSolidSprite(new Color32(123, 212, 97, 255));
            enemyView.Renderer.color = Color.white;
        }

        private void OnEnemyDamaged(CombatEnemyInstance enemy)
        {
            if (enemyViews.TryGetValue(enemy, out var enemyView))
            {
                StopCoroutineSafe(enemyView.HitRoutine);
                enemyView.HitRoutine = StartCoroutine(PlayEnemyHit(enemy, enemyView));
            }
        }

        private void OnEnemyDefeated(CombatEnemyInstance enemy)
        {
            if (enemyViews.TryGetValue(enemy, out var enemyView))
            {
                StopCoroutineSafe(enemyView.DeathRoutine);
                enemyView.DeathRoutine = StartCoroutine(PlayEnemyDeath(enemyView));
            }
        }

        private void OnPlayerAttacked()
        {
            if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
            {
                playerAnimator.SetTrigger("Attack");
                return;
            }

            StartCoroutine(PlayPlayerAttack());
        }

        private void OnPlayerDamaged()
        {
            StartCoroutine(FlashPlayer(new Color(1f, 0.55f, 0.55f, 1f), 0.12f));
        }

        private void OnPlayerRestStarted()
        {
            if (playerRenderer != null)
            {
                playerRenderer.color = new Color(0.55f, 0.65f, 0.78f, 1f);
            }
        }

        private void OnPlayerRestEnded()
        {
            if (playerRenderer != null)
            {
                playerRenderer.color = Color.white;
            }
        }

        private IEnumerator PlayPlayerAttack()
        {
            attackFlash.SetActive(true);
            var startScale = playerTransform.localScale;
            playerTransform.localScale = new Vector3(startScale.x * 1.08f, startScale.y * 0.92f, startScale.z);
            yield return new WaitForSeconds(0.10f);
            playerTransform.localScale = startScale;
            attackFlash.SetActive(false);
        }

        private IEnumerator PlayEnemyHit(CombatEnemyInstance enemy, EnemyView enemyView)
        {
            enemyView.Renderer.color = Color.white * 1.6f;
            combatService.PushEnemyBack(enemy, 0.42f);
            yield return new WaitForSeconds(0.10f);
            enemyView.Renderer.color = Color.white;
        }

        private IEnumerator PlayEnemyDeath(EnemyView enemyView)
        {
            var renderer = enemyView.Renderer;
            for (var t = 0f; t < 1f; t += Time.deltaTime * 4f)
            {
                if (renderer == null)
                {
                    yield break;
                }

                renderer.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            if (enemyView.Root != null)
            {
                enemyView.Root.SetActive(false);
            }
        }

        private IEnumerator FlashPlayer(Color color, float seconds)
        {
            if (playerRenderer == null)
            {
                yield break;
            }

            var previous = playerRenderer.color;
            playerRenderer.color = color;
            yield return new WaitForSeconds(seconds);
            playerRenderer.color = combatService.IsResting ? new Color(0.55f, 0.65f, 0.78f, 1f) : previous;
        }

        private void StopCoroutineSafe(Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }

        private static Sprite CreateSolidSprite(Color32 color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
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
            camera.transform.position = new Vector3(0f, -0.35f, -10f);
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
            if (combatService == null)
            {
                return;
            }

            combatService.EnemySpawned -= OnEnemySpawned;
            combatService.EnemyDamaged -= OnEnemyDamaged;
            combatService.EnemyDefeated -= OnEnemyDefeated;
            combatService.PlayerAttacked -= OnPlayerAttacked;
            combatService.PlayerDamaged -= OnPlayerDamaged;
            combatService.PlayerRestStarted -= OnPlayerRestStarted;
            combatService.PlayerRestEnded -= OnPlayerRestEnded;
        }

        private sealed class EnemyView
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Coroutine HitRoutine;
            public Coroutine DeathRoutine;
        }
    }
}
