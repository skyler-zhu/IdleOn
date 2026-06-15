using UnityEngine;
using UnityEngine.Tilemaps;

namespace IdleOnLike.Data
{
    [CreateAssetMenu(menuName = "IdleOn Like/Data/Zone Tilemap", fileName = "ZoneTilemap_")]
    public sealed class ZoneTilemapDefinition : ScriptableObject
    {
        [SerializeField] private TileBase lowerGroundTile;
        [SerializeField] private TileBase upperPlatformTile;
        [SerializeField] private TileBase backgroundTile;
        [SerializeField] private bool useBackgroundTile;
        [SerializeField] private float tileSize = 1f;

        public TileBase LowerGroundTile => lowerGroundTile;
        public TileBase UpperPlatformTile => upperPlatformTile;
        public TileBase BackgroundTile => useBackgroundTile ? backgroundTile : null;
        public bool UseBackgroundTile => useBackgroundTile;
        public float TileSize => Mathf.Max(0.05f, tileSize);
    }
}
