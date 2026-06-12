using IdleOnLike.Data;
using IdleOnLike.Save;

namespace IdleOnLike.Core
{
    public sealed class GameState
    {
        public GameState(GameCatalog catalog, PlayerSaveData saveData)
        {
            Catalog = catalog;
            SaveData = saveData;
        }

        public GameCatalog Catalog { get; }
        public PlayerSaveData SaveData { get; }

        public CharacterDefinition Character => Catalog != null ? Catalog.FindCharacter(SaveData.characterId) : null;
        public ZoneDefinition CurrentZone => Catalog != null ? Catalog.FindZone(SaveData.currentZoneId) : null;
    }
}
