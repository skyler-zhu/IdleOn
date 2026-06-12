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

            BuildGround();
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
            treeObject.transform.position = new Vector3(3.25f, -0.95f, 0f);

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

        private void Update()
        {
            if (playerTransform != null)
            {
                playerTransform.position = combatService.PlayerPosition;
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
            ground.transform.position = new Vector3(0f, -1.65f, 0.1f);
            ground.transform.localScale = new Vector3(8f, 0.18f, 1f);
            var renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(new Color32(54, 86, 65, 255));
            renderer.sortingOrder = -10;
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
