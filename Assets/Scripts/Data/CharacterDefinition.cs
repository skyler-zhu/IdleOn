using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Character", fileName = "Character_")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private CharacterRole role = CharacterRole.Beginner;
        [TextArea]
        [SerializeField] private string description;

        [Header("Art")]
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private GameObject characterPrefab;

        [Header("Starting State")]
        [SerializeField] private StatBlock baseStats;
        [SerializeField] private ZoneDefinition startingZone;
        [SerializeField] private ItemDefinition startingWeapon;

        public string Id => id;
        public string DisplayName => displayName;
        public CharacterRole Role => role;
        public string Description => description;
        public Sprite Portrait => portrait;
        public Sprite IdleSprite => idleSprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public GameObject CharacterPrefab => characterPrefab;
        public StatBlock BaseStats => baseStats;
        public ZoneDefinition StartingZone => startingZone;
        public ItemDefinition StartingWeapon => startingWeapon;
    }
}
