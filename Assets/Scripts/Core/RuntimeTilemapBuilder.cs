using UnityEngine;
using UnityEngine.Tilemaps;

namespace IdleOnLike.Core
{
    public static class RuntimeTilemapBuilder
    {
        public static bool TryCreateFilledTilemap(Transform parent, string name, TileBase tile, Vector3 center, Vector2 size, int sortingOrder, float tileSize, bool alignTop)
        {
            if (tile == null)
            {
                return false;
            }

            var tileWorldSize = GetTileWorldSize(tile, tileSize);
            var columns = Mathf.Max(1, Mathf.CeilToInt(size.x / tileWorldSize.x));
            var rows = Mathf.Max(1, Mathf.CeilToInt(size.y / tileWorldSize.y));

            var gridObject = new GameObject(name);
            gridObject.transform.SetParent(parent, false);
            gridObject.transform.position = center;

            var grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(tileWorldSize.x, tileWorldSize.y, 1f);

            var tilemapObject = new GameObject($"{name} Tiles");
            tilemapObject.transform.SetParent(gridObject.transform, false);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            var renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            for (var x = 0; x < columns; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            tilemap.CompressBounds();
            var boundsCenter = tilemap.localBounds.center;
            tilemapObject.transform.localPosition = new Vector3(-boundsCenter.x, -boundsCenter.y, 0f);
            if (alignTop)
            {
                var targetTop = center.y + size.y * 0.5f;
                var currentTop = tilemapObject.transform.TransformPoint(tilemap.localBounds.max).y;
                gridObject.transform.position += Vector3.up * (targetTop - currentTop);
            }

            return true;
        }

        private static Vector2 GetTileWorldSize(TileBase tile, float tileScale)
        {
            var data = new TileData();
            tile.GetTileData(Vector3Int.zero, null, ref data);
            var sprite = data.sprite;
            var scale = Mathf.Max(0.05f, tileScale);
            if (sprite == null)
            {
                return Vector2.one * scale;
            }

            var bounds = sprite.bounds.size;
            return new Vector2(Mathf.Max(0.05f, bounds.x * scale), Mathf.Max(0.05f, bounds.y * scale));
        }
    }
}
