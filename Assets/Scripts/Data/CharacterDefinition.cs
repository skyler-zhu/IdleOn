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
        [SerializeField] private VisualAnimationClips animationClips = new VisualAnimationClips();
        [SerializeField] private GameObject characterPrefab;
        [Min(0.1f)]
        [SerializeField] private float visualScale = 1.35f;

        [Header("Audio")]
        [SerializeField] private AudioClip attackSfx;

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
        public VisualAnimationClips AnimationClips => animationClips;
        public GameObject CharacterPrefab => characterPrefab;
        public float VisualScale => visualScale;
        public AudioClip AttackSfx => attackSfx;
        public StatBlock BaseStats => baseStats;
        public ZoneDefinition StartingZone => startingZone;
        public ItemDefinition StartingWeapon => startingWeapon;
    }
}
