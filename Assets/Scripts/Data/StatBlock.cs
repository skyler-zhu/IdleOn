using System;
using UnityEngine;

namespace IdleOnLike.Data
{
    [Serializable]
    public struct StatBlock
    {
        [Min(0)] public int maxHp;
        [Min(0)] public int attack;
        [Min(0)] public int defense;
        [Min(0)] public int strength;
        [Min(0)] public int agility;
        [Min(0)] public int wisdom;
        [Min(0)] public int luck;

        public static StatBlock operator +(StatBlock left, StatBlock right)
        {
            return new StatBlock
            {
                maxHp = left.maxHp + right.maxHp,
                attack = left.attack + right.attack,
                defense = left.defense + right.defense,
                strength = left.strength + right.strength,
                agility = left.agility + right.agility,
                wisdom = left.wisdom + right.wisdom,
                luck = left.luck + right.luck
            };
        }
    }
}
