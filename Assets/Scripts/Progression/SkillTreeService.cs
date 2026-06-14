using System;
using IdleOnLike.Core;
using IdleOnLike.Data;
using IdleOnLike.Save;
using UnityEngine;

namespace IdleOnLike.Progression
{
    public sealed class SkillTreeService
    {
        private readonly GameState state;

        public SkillTreeService(GameState state)
        {
            this.state = state;
            this.state.SaveData.EnsureCollections();
        }

        public event Action Changed;

        public int GetRank(string nodeId)
        {
            var entry = state.SaveData.skillNodeRanks.Find(rank => rank.id == nodeId);
            return entry != null ? entry.rank : 0;
        }

        public SaveSkillProgress GetProgress(SkillType skillType)
        {
            var progress = state.SaveData.skillProgress.Find(entry => entry.skillType == skillType);
            if (progress != null)
            {
                if (progress.level <= 0)
                {
                    progress.level = 1;
                }

                return progress;
            }

            progress = new SaveSkillProgress
            {
                skillType = skillType,
                level = 1,
                experience = 0,
                points = 0
            };
            state.SaveData.skillProgress.Add(progress);
            return progress;
        }

        public int AddSkillExperience(SkillType skillType, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var progress = GetProgress(skillType);
            progress.experience += amount;
            var levels = 0;
            while (progress.experience >= ProgressionService.GetSkillExperienceRequired(progress.level))
            {
                progress.experience -= ProgressionService.GetSkillExperienceRequired(progress.level);
                progress.level++;
                progress.points++;
                levels++;
            }

            Changed?.Invoke();
            return levels;
        }

        public bool CanRankUp(SkillNodeDefinition node)
        {
            if (node == null || GetProgress(node.SkillType).points <= 0 || GetRank(node.Id) >= node.MaxRank)
            {
                return false;
            }

            foreach (var prerequisite in node.Prerequisites)
            {
                if (prerequisite != null && GetRank(prerequisite.Id) <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool RankUp(string nodeId)
        {
            var node = state.Catalog.FindSkillNode(nodeId);
            if (!CanRankUp(node))
            {
                return false;
            }

            var entry = state.SaveData.skillNodeRanks.Find(rank => rank.id == nodeId);
            if (entry == null)
            {
                entry = new SaveRankEntry { id = nodeId, rank = 0 };
                state.SaveData.skillNodeRanks.Add(entry);
            }

            entry.rank++;
            GetProgress(node.SkillType).points--;
            Changed?.Invoke();
            return true;
        }

        public float GetEffectValue(SkillType skillType, SkillNodeEffectType effectType)
        {
            var total = 0f;
            foreach (var node in state.Catalog.SkillNodes)
            {
                if (node != null && node.SkillType == skillType && node.EffectType == effectType)
                {
                    total += GetRank(node.Id) * node.ValuePerRank;
                }
            }

            return total;
        }

        public float GetGatherSeconds(SkillType skillType, float baseSeconds)
        {
            var speed = Mathf.Clamp(GetEffectValue(skillType, SkillNodeEffectType.GatherSpeed), 0f, 0.60f);
            return Mathf.Max(0.5f, baseSeconds * (1f - speed));
        }

        public float GetExtraDropChance(SkillType skillType)
        {
            var effectType = skillType == SkillType.Mining
                ? SkillNodeEffectType.ExtraOreChance
                : SkillNodeEffectType.ExtraWoodChance;
            return Mathf.Clamp01(GetEffectValue(skillType, effectType));
        }
    }
}
