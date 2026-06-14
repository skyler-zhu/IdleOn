using IdleOnLike.Save;
using UnityEngine;

namespace IdleOnLike.Progression
{
    public static class ProgressionService
    {
        public static int GetExperienceRequired(int level)
        {
            return 40 + (Mathf.Max(1, level) - 1) * 40;
        }

        public static int AddExperience(PlayerSaveData saveData, int amount)
        {
            if (saveData == null || amount <= 0)
            {
                return 0;
            }

            saveData.experience += amount;
            var levelsGained = 0;
            while (saveData.experience >= GetExperienceRequired(saveData.level))
            {
                saveData.experience -= GetExperienceRequired(saveData.level);
                saveData.level++;
                saveData.talentPoints++;
                levelsGained++;
            }

            return levelsGained;
        }

        public static int GetSkillExperienceRequired(int level)
        {
            return 30 + (Mathf.Max(1, level) - 1) * 30;
        }
    }
}
