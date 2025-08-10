using Playnite.SDK.Data;

namespace PlayniteMultiAccountSteamLibrary.Extension.Steam
{
    public class GetOwnedGamesResponse
    {
        [SerializationPropertyName("response")]
        public GetOwnedGamesResult Response { get; set; } = null!;
    }
}