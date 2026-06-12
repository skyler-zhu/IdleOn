using System;
using System.Collections.Generic;
using IdleOnLike.Data;

namespace IdleOnLike.Progression
{
    public sealed class OfflineGainsResult
    {
        public TimeSpan elapsed;
        public ZoneActivity activity;
        public int experience;
        public int coins;
        public int levelsGained;
        public List<OfflineItemReward> items = new List<OfflineItemReward>();
        public List<string> questProgress = new List<string>();
    }

    public sealed class OfflineItemReward
    {
        public string itemId;
        public string displayName;
        public int quantity;
    }
}
