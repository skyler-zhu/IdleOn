using IdleOnLike.Data;
using UnityEngine;

namespace IdleOnLike.Combat
{
    public sealed class CombatEnemyInstance
    {
        public EnemyDefinition enemyDefinition;
        public int currentHp;
        public Vector3 spawnPosition;
        public Vector3 currentPosition;
        public float nextAttackTime;
        public object viewReference;

        public bool IsAlive => enemyDefinition != null && currentHp > 0;
    }
}
