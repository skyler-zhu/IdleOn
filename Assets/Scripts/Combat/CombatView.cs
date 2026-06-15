using System.Collections;
using System.Collections.Generic;
using IdleOnLike.Core;
using IdleOnLike.Data;
using UnityEngine;

namespace IdleOnLike.Combat
{
    public sealed class CombatView : MonoBehaviour
    {
        private readonly Dictionary<CombatEnemyInstance, EnemyView> enemyViews = new Dictionary<CombatEnemyInstance, EnemyView>();

        private GameRuntime runtime;
        private CombatService combatService;
        private Transform playerTransform;
        private VisualAnimatorDriver playerVisual;
        private GameObject attackFlash;
        private GameObject treeObject;
        private GameObject villagePortalObject;
        private GameObject stonePortalObject;
        private bool wasJumping;

        public bool IsPlayerNearTree(Vector3 playerPosition)
        {
            return treeObject != null && Vector3.Distance(playerPosition, treeObject.transform.position) <= 1.35f;
        }

        public bool IsPlayerNearVillagePortal(Vector3 playerPosition)
        {
            return villagePortalObject != null && Vector3.Distance(playerPosition, villagePortalObject.transform.position) <= 1.25f;
        }

        public bool IsPlayerNearStonePortal(Vector3 playerPosition)
        {
            return stonePortalObject != null && Vector3.Distance(playerPosition, stonePortalObject.transform.position) <= 1.25f;
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
            BuildStonePortal();
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
            if (IsStoneZone())
            {
                return;
            }

            treeObject = new GameObject("Tree Resource Node");
            treeObject.transform.SetParent(transform, false);
            var visual = GetZoneVisual();
            var visualSlot = ZoneVisualSlotType.TreeResource;
            var visualScale = GetVisualScale(visual, visualSlot);
            var sizeScale = GetVisualSizeScale(visual, visualSlot);
            treeObject.transform.position = new Vector3(-5.65f, 1.25f + GetVisualYOffset(visual, visualSlot), 0f);

            var nodeSprite = GetVisualSprite(visual, visualSlot);
            if (nodeSprite == null)
            {
                nodeSprite = GetResourceNodeSprite("tree");
            }

            if (nodeSprite != null)
            {
                treeObject.transform.localScale = Vector3.Scale(new Vector3(0.95f, 0.95f, 1f) * visualScale, new Vector3(sizeScale.x, sizeScale.y, 1f));
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
                var previousPosition = playerTransform.position;
                var jumpOffset = combatService.IsJumping
                    ? Mathf.Sin(combatService.JumpProgress * Mathf.PI) * 1.12f
                    : 0f;
                playerTransform.position = combatService.PlayerPosition + Vector3.up * jumpOffset;
                playerVisual.SetFacing(combatService.FacingSign);
                var horizontalSpeed = Mathf.Abs(playerTransform.position.x - previousPosition.x) > 0.001f ? 1f : 0f;
                playerVisual.SetMoveAmount(horizontalSpeed);
                if (combatService.IsJumping && !wasJumping)
                {
                    playerVisual.PlayJump();
                }

                playerVisual.Tick(Time.deltaTime);
                wasJumping = combatService.IsJumping;
            }

            foreach (var pair in enemyViews)
            {
                if (pair.Value.Root != null)
                {
                    var deltaX = pair.Key.currentPosition.x - pair.Value.PreviousDataX;
                    if (Mathf.Abs(deltaX) > 0.001f)
                    {
                        pair.Value.Visual?.SetFacing(deltaX < 0f ? -1f : 1f);
                    }

                    pair.Value.PreviousDataX = pair.Key.currentPosition.x;
                    pair.Value.Root.transform.position = GetEnemyVisualPosition(pair.Key);
                    pair.Value.Visual?.Tick(Time.deltaTime);
                }
            }
        }

        private void BuildGround()
        {
            var visual = GetZoneVisual();
            var stoneZone = IsStoneZone();
            CreateTilemapOrSprite(visual, stoneZone ? "Stone Sanctum Backdrop" : "Forest Backdrop", ZoneVisualSlotType.Background, new Vector3(0f, 0f, 0.5f), new Vector2(16f, 6.6f), stoneZone ? new Color32(59, 58, 67, 255) : new Color32(66, 116, 85, 255), -20);
            CreateTilemapOrSprite(visual, stoneZone ? "Stone Sanctum Ground" : "Combat Ground", ZoneVisualSlotType.LowerGround, new Vector3(0f, -1.95f, 0.1f), new Vector2(14f, 0.20f), stoneZone ? new Color32(88, 82, 83, 255) : new Color32(54, 86, 65, 255), -10);
            if (stoneZone)
            {
                return;
            }

            CreateTilemapOrSprite(visual, "Upper Forest Platform", ZoneVisualSlotType.UpperPlatform, new Vector3(0f, 0.62f, 0.1f), new Vector2(15.2f, 0.18f), new Color32(64, 104, 73, 255), -9);

            var rope = new GameObject("Forest Rope");
            rope.transform.SetParent(transform, false);
            rope.transform.position = new Vector3(0.15f, -0.1f, 0f);
            rope.transform.localScale = new Vector3(0.12f, 2.65f, 1f);
            var ropeRenderer = rope.AddComponent<SpriteRenderer>();
            var ropeSprite = GetVisualSprite(visual, ZoneVisualSlotType.Rope);
            ropeRenderer.sprite = ropeSprite != null
                ? ropeSprite
                : CreateSolidSprite(new Color32(176, 139, 82, 255));
            ropeRenderer.sortingOrder = 1;
            CreateWorldLabel("Forest Rope Label", "Rope\nPress F", rope.transform.position + new Vector3(0f, 1.46f, 0f), 26, Color.white);
        }

        private void BuildVillagePortal()
        {
            var visual = GetZoneVisual();
            var portalSprite = GetVisualSprite(visual, ZoneVisualSlotType.Portal);
            var stoneZone = IsStoneZone();
            villagePortalObject = new GameObject(stoneZone ? "Forest Portal" : "Village Portal");
            villagePortalObject.transform.SetParent(transform, false);
            villagePortalObject.transform.position = new Vector3(6.05f, -1.22f, 0f);

            var ring = new GameObject(stoneZone ? "Forest Portal Ring" : "Village Portal Ring");
            ring.transform.SetParent(villagePortalObject.transform, false);
            ring.transform.localScale = new Vector3(0.72f, 1.15f, 1f);
            var ringRenderer = ring.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = portalSprite != null ? portalSprite : CreateSolidSprite(stoneZone ? new Color32(67, 169, 84, 255) : new Color32(86, 139, 212, 255));
            ringRenderer.sortingOrder = 3;

            if (portalSprite == null)
            {
                var core = new GameObject(stoneZone ? "Forest Portal Core" : "Village Portal Core");
                core.transform.SetParent(villagePortalObject.transform, false);
                core.transform.localScale = new Vector3(0.46f, 0.88f, 1f);
                var coreRenderer = core.AddComponent<SpriteRenderer>();
                coreRenderer.sprite = CreateSolidSprite(new Color32(45, 54, 78, 255));
                coreRenderer.sortingOrder = 4;
            }

            CreateWorldLabel(stoneZone ? "Forest Portal Label" : "Village Portal Label", stoneZone ? "Forest\nPress F" : "Village\nPress F", villagePortalObject.transform.position + new Vector3(0f, 0.98f, 0f), 30, Color.white);
        }

        private void BuildStonePortal()
        {
            if (IsStoneZone())
            {
                return;
            }

            var visual = GetZoneVisual();
            var portalSprite = GetVisualSprite(visual, ZoneVisualSlotType.Portal);
            stonePortalObject = new GameObject("Stone Sanctum Portal");
            stonePortalObject.transform.SetParent(transform, false);
            stonePortalObject.transform.position = new Vector3(-6.05f, -1.22f, 0f);

            var ring = new GameObject("Stone Sanctum Portal Ring");
            ring.transform.SetParent(stonePortalObject.transform, false);
            ring.transform.localScale = new Vector3(0.72f, 1.15f, 1f);
            var ringRenderer = ring.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = portalSprite != null ? portalSprite : CreateSolidSprite(new Color32(130, 116, 148, 255));
            ringRenderer.sortingOrder = 3;

            if (portalSprite == null)
            {
                var core = new GameObject("Stone Sanctum Portal Core");
                core.transform.SetParent(stonePortalObject.transform, false);
                core.transform.localScale = new Vector3(0.46f, 0.88f, 1f);
                var coreRenderer = core.AddComponent<SpriteRenderer>();
                coreRenderer.sprite = CreateSolidSprite(new Color32(42, 39, 51, 255));
                coreRenderer.sortingOrder = 4;
            }

            var stoneZone = runtime.Catalog.FindZone("stone_sanctum");
            var label = runtime.IsZoneUnlocked(stoneZone) ? "Stone Sanctum\nPress F" : "Stone Sanctum\nLocked";
            CreateWorldLabel("Stone Sanctum Portal Label", label, stonePortalObject.transform.position + new Vector3(0f, 0.98f, 0f), 26, Color.white);
        }

        private void BuildPlayer()
        {
            var playerObject = new GameObject("Player Visual");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = combatService.PlayerPosition;
            playerTransform = playerObject.transform;
            var character = runtime.State.Character;
            playerVisual = new VisualAnimatorDriver(
                playerObject,
                character != null ? character.IdleSprite : null,
                character != null ? character.AnimationClips : null,
                character != null ? character.VisualScale : 1f,
                5,
                new Color32(82, 168, 255, 255));
            playerVisual.SetFacing(1f);

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
                enemyView = new EnemyView { Root = root };
                enemyViews.Add(enemy, enemyView);
                enemy.viewReference = enemyView;
            }

            enemyView.Root.name = $"Enemy Visual - {enemy.enemyDefinition.DisplayName}";
            enemyView.Root.transform.position = GetEnemyVisualPosition(enemy);
            enemyView.Root.SetActive(true);
            enemyView.PreviousDataX = enemy.currentPosition.x;
            enemyView.Visual?.Dispose();
            enemyView.Visual = new VisualAnimatorDriver(
                enemyView.Root,
                enemy.enemyDefinition.IdleSprite,
                enemy.enemyDefinition.AnimationClips,
                enemy.enemyDefinition.VisualScale,
                4,
                new Color32(123, 212, 97, 255));
        }

        private static Vector3 GetEnemyVisualPosition(CombatEnemyInstance enemy)
        {
            if (enemy == null || enemy.enemyDefinition == null)
            {
                return Vector3.zero;
            }

            return enemy.currentPosition + Vector3.up * enemy.enemyDefinition.VisualYOffset;
        }

        private void OnEnemyDamaged(CombatEnemyInstance enemy)
        {
            if (enemyViews.TryGetValue(enemy, out var enemyView))
            {
                enemyView.Visual.PlayHit();
                StopCoroutineSafe(enemyView.HitRoutine);
                enemyView.HitRoutine = StartCoroutine(PlayEnemyHit(enemy, enemyView));
            }
        }

        private void OnEnemyDefeated(CombatEnemyInstance enemy)
        {
            if (enemyViews.TryGetValue(enemy, out var enemyView))
            {
                enemyView.Visual.PlayDeath();
                StopCoroutineSafe(enemyView.DeathRoutine);
                enemyView.DeathRoutine = StartCoroutine(PlayEnemyDeath(enemyView));
            }
        }

        private void OnPlayerAttacked()
        {
            runtime.PlayAttackSfx();
            playerVisual.PlayAttack();
            if (playerVisual.HasAttackAnimation)
            {
                return;
            }

            StartCoroutine(PlayPlayerAttack());
        }

        public void PlayPlayerGather()
        {
            if (playerVisual == null)
            {
                return;
            }

            playerVisual.PlayGather();
        }

        private void OnPlayerDamaged()
        {
            StartCoroutine(FlashPlayer(new Color(1f, 0.55f, 0.55f, 1f), 0.12f));
        }

        private void OnPlayerRestStarted()
        {
            playerVisual.PlayDeath();
            playerVisual.SetColor(new Color(0.50f, 0.52f, 0.58f, 1f));
        }

        private void OnPlayerRestEnded()
        {
            playerVisual.SetColor(Color.white);
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
            enemyView.Visual.SetColor(Color.white * 1.6f);
            combatService.PushEnemyBack(enemy, 0.42f);
            yield return new WaitForSeconds(0.10f);
            enemyView.Visual.SetColor(Color.white);
        }

        private IEnumerator PlayEnemyDeath(EnemyView enemyView)
        {
            for (var t = 0f; t < 1f; t += Time.deltaTime * 4f)
            {
                if (enemyView.Visual == null)
                {
                    yield break;
                }

                enemyView.Visual.SetColor(new Color(1f, 1f, 1f, 1f - t));
                yield return null;
            }

            if (enemyView.Root != null)
            {
                enemyView.Root.SetActive(false);
            }
        }

        private IEnumerator FlashPlayer(Color color, float seconds)
        {
            if (playerVisual == null)
            {
                yield break;
            }

            var previous = playerVisual.Renderer.color;
            playerVisual.SetColor(color);
            yield return new WaitForSeconds(seconds);
            playerVisual.SetColor(combatService.IsResting ? new Color(0.50f, 0.52f, 0.58f, 1f) : previous);
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

        private ZoneVisualDefinition GetZoneVisual()
        {
            var zone = runtime != null && runtime.State != null ? runtime.State.CurrentZone : null;
            return zone != null ? zone.Visual : null;
        }

        private bool IsStoneZone()
        {
            return runtime != null && runtime.State != null && runtime.State.CurrentZone != null && runtime.State.CurrentZone.Id == "stone_sanctum";
        }

        private static Sprite GetVisualSprite(ZoneVisualDefinition visual, ZoneVisualSlotType slotType)
        {
            return visual != null ? visual.GetSprite(slotType) : null;
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

            var instance = new GameObject(name);
            instance.transform.SetParent(transform, false);
            instance.transform.position = visualPosition;
            var renderer = instance.AddComponent<SpriteRenderer>();
            var sprite = GetVisualSprite(visual, slotType);
            renderer.sprite = sprite != null ? sprite : CreateSolidSprite(fallbackColor);
            renderer.sortingOrder = sortingOrder;
            instance.transform.localScale = GetSpriteScale(slotType, sprite, scaledSize);
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
            playerVisual?.Dispose();
            foreach (var enemyView in enemyViews.Values)
            {
                enemyView.Visual?.Dispose();
            }

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
            public VisualAnimatorDriver Visual;
            public Coroutine HitRoutine;
            public Coroutine DeathRoutine;
            public float PreviousDataX;
        }
    }
}
