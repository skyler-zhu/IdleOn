using System.Collections;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.UI;
using UnityEngine;

namespace IdleOnLike.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        private const int EnemyCount = 3;
        private const int StoneEnemyCount = 1;
        private const float AttackSeconds = 1f;
        private const float RespawnSeconds = 1f;

        private GameRuntime runtime;
        private CombatService combatService;
        private CombatView combatView;
        private bool isAutoMode;
        private bool isReturningToVillage;
        private float nextManualActionTime;

        public static CombatController Create(GameRuntime runtime)
        {
            var controllerObject = new GameObject("Combat Controller");
            var controller = controllerObject.AddComponent<CombatController>();
            controller.Initialize(runtime);
            return controller;
        }

        private void Initialize(GameRuntime gameRuntime)
        {
            runtime = gameRuntime;
            combatService = new CombatService(runtime.State, runtime.InventoryService, runtime.EquipmentService, runtime.QuestService, runtime.TalentService);
            if (runtime.State.CurrentZone == runtime.Catalog.ForestZone && runtime.ConsumeSpawnForestAtStonePortal())
            {
                combatService.SpawnAtForestStonePortal();
            }
            else if (runtime.State.CurrentZone == runtime.Catalog.ForestZone && runtime.State.SaveData.currentActivity == ZoneActivity.Chopping.ToString())
            {
                isAutoMode = true;
                combatService.SpawnAtTree();
            }

            combatView = CombatView.Create(runtime, combatService);
            CombatHudScreen.Build(runtime, combatService, () => combatView.IsPlayerNearTree(combatService.PlayerPosition), () => isAutoMode, ToggleAutoMode, CanManualAction, PerformManualAction);
            combatService.EnemyDefeated += OnEnemyDefeated;
            combatService.PlayerDied += OnPlayerDied;
            runtime.GatheringService.ResourceGathered += OnResourceGathered;
            combatService.SpawnInitialEnemies(combatService.IsStoneZone ? StoneEnemyCount : EnemyCount);
            StartCoroutine(CombatLoop());
        }

        private void Update()
        {
            if (combatService == null || isReturningToVillage)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                combatService.Jump();
            }

            if (!combatService.IsStoneZone && Input.GetKeyDown(KeyCode.F) && combatService.IsNearRope)
            {
                combatService.UseRope();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F) && combatView.IsPlayerNearVillagePortal(combatService.PlayerPosition))
            {
                if (combatService.IsStoneZone)
                {
                    runtime.ReturnFromStoneSanctumToForest();
                }
                else
                {
                    runtime.ReturnToVillage();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.F) && combatView.IsPlayerNearStonePortal(combatService.PlayerPosition))
            {
                runtime.TravelToStoneSanctum();
                return;
            }

            if (Input.GetKeyDown(KeyCode.J) && !isAutoMode)
            {
                PerformManualAction();
            }

            var fighting = runtime.State.SaveData.currentActivity == ZoneActivity.Fighting.ToString();
            var chopping = !combatService.IsStoneZone && runtime.State.SaveData.currentActivity == ZoneActivity.Chopping.ToString();
            if (!isAutoMode)
            {
                combatService.MovePlayerManual(Input.GetAxisRaw("Horizontal"), Time.deltaTime);
            }

            combatService.Tick(Time.deltaTime, Time.time, fighting, chopping, isAutoMode);
            runtime.GatheringService.Tick(isAutoMode && chopping ? Time.deltaTime : 0f, isAutoMode && chopping && combatView.IsPlayerNearTree(combatService.PlayerPosition));
        }

        private void ToggleAutoMode()
        {
            isAutoMode = !isAutoMode;
        }

        private void PerformManualAction()
        {
            if (!CanManualAction())
            {
                return;
            }

            if (!combatService.IsStoneZone && combatView.IsPlayerNearTree(combatService.PlayerPosition))
            {
                if (!runtime.GatheringService.IsChopping)
                {
                    runtime.GatheringService.StartGathering("tree", true);
                }

                runtime.GatheringService.GatherOnce(combatView.IsPlayerNearTree(combatService.PlayerPosition));
                nextManualActionTime = Time.time + runtime.SkillTreeService.GetGatherSeconds(SkillType.Chopping, 2f);
                return;
            }

            if (combatService.AttackCurrentTarget())
            {
                nextManualActionTime = Time.time + AttackSeconds;
            }
            else
            {
                nextManualActionTime = Time.time + AttackSeconds;
            }
        }

        private bool CanManualAction()
        {
            return !isAutoMode && Time.time >= nextManualActionTime;
        }

        private IEnumerator CombatLoop()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(AttackSeconds);
                if (!isReturningToVillage && isAutoMode && runtime.State.SaveData.currentActivity == ZoneActivity.Fighting.ToString())
                {
                    if (combatService.CanAttackCurrentTargetInRange())
                    {
                        combatService.AttackCurrentTarget();
                    }
                }
            }
        }

        private void OnEnemyDefeated(CombatEnemyInstance enemy)
        {
            if (isReturningToVillage)
            {
                return;
            }

            runtime.Save();
            StartCoroutine(RespawnAfterDelay(enemy));
        }

        private void OnPlayerDied()
        {
            if (isReturningToVillage)
            {
                return;
            }

            isReturningToVillage = true;
            StartCoroutine(ReturnToVillageAfterDeath());
        }

        private void OnResourceGathered()
        {
            combatView?.PlayPlayerGather();
        }

        private IEnumerator ReturnToVillageAfterDeath()
        {
            yield return new WaitForSeconds(1f);
            runtime.State.SaveData.currentHp = Mathf.Max(1, runtime.State.SaveData.currentHp);
            runtime.Save();
            runtime.ReturnToVillage();
        }

        private IEnumerator RespawnAfterDelay(CombatEnemyInstance enemy)
        {
            yield return new WaitForSeconds(RespawnSeconds);
            combatService.ReplaceEnemy(enemy);
        }

        private void OnDestroy()
        {
            if (combatService != null)
            {
                combatService.EnemyDefeated -= OnEnemyDefeated;
                combatService.PlayerDied -= OnPlayerDied;
            }

            if (runtime != null && runtime.GatheringService != null)
            {
                runtime.GatheringService.ResourceGathered -= OnResourceGathered;
            }
        }
    }
}
