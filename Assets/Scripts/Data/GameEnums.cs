namespace IdleOnLike.Data
{
    public enum CharacterRole
    {
        Beginner,
        Warrior,
        Archer,
        Mage
    }

    public enum ItemType
    {
        Material,
        Consumable,
        Equipment,
        Quest,
        Currency
    }

    public enum EquipmentSlot
    {
        None,
        Weapon,
        Helmet,
        Chest,
        Boots,
        Pendant
    }

    public enum SkillType
    {
        Combat,
        Chopping,
        Mining,
        Smithing
    }

    public enum TalentStatType
    {
        Strength,
        Agility,
        Wisdom,
        Luck,
        AttackPower,
        MaxHp
    }

    public enum SkillNodeEffectType
    {
        ExtraWoodChance,
        ExtraOreChance,
        GatherSpeed
    }

    public enum QuestObjectiveType
    {
        KillEnemy,
        CollectItem,
        GatherResource,
        CraftItem,
        ReachLevel,
        SwitchCharacter,
        EquipItem
    }

    public enum ZoneActivity
    {
        Fighting,
        Chopping,
        Mining
    }
}
