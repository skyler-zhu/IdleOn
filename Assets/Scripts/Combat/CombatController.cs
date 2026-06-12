using System.Collections;
using IdleOnLike.Core;
using IdleOnLike.UI;
using UnityEngine;

namespace IdleOnLike.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        private const float AttackSeconds = 1f;
        private const float RespawnSeconds = 1f;

        private CombatService combatService;
        private bool waitingForRespawn;

        public static CombatController Create(GameRuntime runtime)
        {
            var controllerObject = new GameObject("Combat Controller");
            var controller = controllerObject.AddComponent<CombatController>();
            controller.Initialize(runtime);
            return controller;
        }

        private void Initialize(GameRuntime runtime)
        {
            combatService = new CombatService(runtime.State, runtime.InventoryService, runtime.EquipmentService, runtime.QuestService);
            CombatHudScreen.Build(runtime, combatService, runtime.ReturnToVillage);
            combatService.EnemyDefeated += runtime.Save;
            combatService.SpawnNextEnemy();
            StartCoroutine(CombatLoop());
        }

        private IEnumerator CombatLoop()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(AttackSeconds);

                if (waitingForRespawn)
                {
                    continue;
                }

                var defeated = combatService.AttackCurrentEnemy();
                if (defeated)
                {
                    StartCoroutine(RespawnAfterDelay());
                }
            }
        }

        private IEnumerator RespawnAfterDelay()
        {
            waitingForRespawn = true;
            yield return new WaitForSeconds(RespawnSeconds);
            combatService.SpawnNextEnemy();
            waitingForRespawn = false;
        }
    }
}
