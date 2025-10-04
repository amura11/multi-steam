using Playnite.SDK.Data;

namespace Playnite.SteamFusion.Steam
{
    public class GetOwnedGamesResponse
    {
        [SerializationPropertyName("response")]
        public GetOwnedGamesResult Response { get; set; } = null!;
    }
}