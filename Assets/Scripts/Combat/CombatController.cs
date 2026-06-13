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
        private const float AttackSeconds = 1f;
        private const float RespawnSeconds = 1f;

        private GameRuntime runtime;
        private CombatService combatService;
        private CombatView combatView;
        private bool isAutoMode = true;
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
            combatService = new CombatService(runtime.State, runtime.InventoryService, runtime.EquipmentService, runtime.QuestService);
            combatView = CombatView.Create(runtime, combatService);
            CombatHudScreen.Build(runtime, combatService, runtime.ReturnToVillage, () => combatView.IsPlayerNearTree(combatService.PlayerPosition), () => isAutoMode, ToggleAutoMode, CanManualAction, PerformManualAction);
            combatService.EnemyDefeated += OnEnemyDefeated;
            combatService.SpawnInitialEnemies(EnemyCount);
            StartCoroutine(CombatLoop());
        }

        private void Update()
        {
            if (combatService == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                combatService.Jump();
            }

            if (Input.GetKeyDown(KeyCode.J) && !isAutoMode)
            {
                PerformManualAction();
            }

            var fighting = runtime.State.SaveData.currentActivity == ZoneActivity.Fighting.ToString();
            if (!isAutoMode)
            {
                combatService.MovePlayerManual(Input.GetAxisRaw("Horizontal"), Time.deltaTime);
            }

            combatService.Tick(Time.deltaTime, Time.time, fighting, isAutoMode);
            runtime.GatheringService.Tick(isAutoMode ? Time.deltaTime : 0f, isAutoMode && combatView.IsPlayerNearTree(combatService.PlayerPosition));
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

            if (runtime.GatheringService.IsGathering)
            {
                runtime.GatheringService.GatherOnce(combatView.IsPlayerNearTree(combatService.PlayerPosition));
                nextManualActionTime = Time.time + 2f;
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
                if (isAutoMode && runtime.State.SaveData.currentActivity == ZoneActivity.Fighting.ToString())
                {
                    combatService.AttackCurrentTarget();
                }
            }
        }

        private void OnEnemyDefeated(CombatEnemyInstance enemy)
        {
            runtime.Save();
            StartCoroutine(RespawnAfterDelay(enemy));
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
            }
        }
    }
}
