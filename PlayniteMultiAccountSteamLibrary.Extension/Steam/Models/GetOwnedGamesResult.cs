using System.Collections.Generic;
using Playnite.SDK.Data;

namespace PlayniteMultiAccountSteamLibrary.Extension.Steam
{
    public class GetOwnedGamesResult
    {
        [SerializationPropertyName("game_count")]
        public int GameCount { get; set; }
        
        [SerializationPropertyName("games")]
        public List<OwnedSteamGame> Games { get; set; } = null!;
    }
}