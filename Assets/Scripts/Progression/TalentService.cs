using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Save;
using UnityEngine;

namespace IdleOnLike.Progression
{
    public sealed class TalentService
    {
        private readonly GameState state;

        public TalentService(GameState state)
        {
            this.state = state;
            this.state.SaveData.EnsureCollections();
        }

        public event Action Changed;

        public int TalentPoints => state.SaveData.talentPoints;

        public int GetRank(string talentId)
        {
            var entry = state.SaveData.talentRanks.Find(rank => rank.id == talentId);
            return entry != null ? entry.rank : 0;
        }

        public bool CanRankUp(TalentDefinition talent)
        {
            if (talent == null || state.SaveData.talentPoints <= 0 || GetRank(talent.Id) >= talent.MaxRank)
            {
                return false;
            }

            foreach (var prerequisite in talent.Prerequisites)
            {
                if (prerequisite != null && GetRank(prerequisite.Id) <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool RankUp(string talentId)
        {
            var talent = state.Catalog.FindTalent(talentId);
            if (!CanRankUp(talent))
            {
                return false;
            }

            var entry = state.SaveData.talentRanks.Find(rank => rank.id == talentId);
            if (entry == null)
            {
                entry = new SaveRankEntry { id = talentId, rank = 0 };
                state.SaveData.talentRanks.Add(entry);
            }

            entry.rank++;
            state.SaveData.talentPoints--;
            Changed?.Invoke();
            return true;
        }

        public float GetStatBonus(TalentStatType statType)
        {
            var total = 0f;
            foreach (var talent in state.Catalog.Talents)
            {
                if (talent != null && talent.StatType == statType)
                {
                    total += GetRank(talent.Id) * talent.ValuePerRank;
                }
            }

            return total;
        }

        public int GetDamageBonus()
        {
            return Mathf.RoundToInt(GetStatBonus(TalentStatType.Strength) + GetStatBonus(TalentStatType.AttackPower));
        }

        public int GetMaxHpBonus()
        {
            return Mathf.RoundToInt(GetStatBonus(TalentStatType.MaxHp));
        }

        public float GetMoveSpeedMultiplier()
        {
            return 1f + Mathf.Clamp(GetStatBonus(TalentStatType.Agility) * 0.01f, 0f, 0.30f);
        }

        public float GetDodgeChance()
        {
            return Mathf.Clamp01(GetStatBonus(TalentStatType.Agility) * 0.002f);
        }
    }
}
