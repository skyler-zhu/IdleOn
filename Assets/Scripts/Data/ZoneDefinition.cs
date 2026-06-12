using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Zone", fileName = "Zone_")]
    public sealed class ZoneDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Scene")]
        [SerializeField] private string sceneName;
        [SerializeField] private Sprite mapIcon;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private GameObject zonePrefab;

        [Header("Unlocks")]
        [SerializeField] private QuestDefinition requiredQuest;
        [SerializeField] private int requiredCharacterLevel;

        [Header("Content")]
        [SerializeField] private List<ZoneEnemySpawn> enemies = new List<ZoneEnemySpawn>();
        [SerializeField] private List<ZoneResourceNode> resourceNodes = new List<ZoneResourceNode>();
        [SerializeField] private ZoneDefinition nextZonePreview;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public string SceneName => sceneName;
        public Sprite MapIcon => mapIcon;
        public Sprite BackgroundSprite => backgroundSprite;
        public GameObject ZonePrefab => zonePrefab;
        public QuestDefinition RequiredQuest => requiredQuest;
        public int RequiredCharacterLevel => requiredCharacterLevel;
        public IReadOnlyList<ZoneEnemySpawn> Enemies => enemies;
        public IReadOnlyList<ZoneResourceNode> ResourceNodes => resourceNodes;
        public ZoneDefinition NextZonePreview => nextZonePreview;
    }
}
