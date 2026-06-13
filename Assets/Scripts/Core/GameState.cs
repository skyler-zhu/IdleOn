using IdleOnLike.Data;
using IdleOnLike.Save;

namespace IdleOnLike.Core
{
    public sealed class GameState
    {
        public GameState(GameCatalog catalog, AccountSaveData accountData)
        {
            Catalog = catalog;
            AccountData = accountData;
        }

        public GameCatalog Catalog { get; }
        public AccountSaveData AccountData { get; }
        public PlayerSaveData SaveData => AccountData.GetActiveCharacter();

        public CharacterDefinition Character => Catalog != null ? Catalog.FindCharacter(SaveData.characterId) : null;
        public ZoneDefinition CurrentZone => Catalog != null ? Catalog.FindZone(SaveData.currentZoneId) : null;
        public int Coins
        {
            get => AccountData.coins;
            set => AccountData.coins = value;
        }
    }
}
