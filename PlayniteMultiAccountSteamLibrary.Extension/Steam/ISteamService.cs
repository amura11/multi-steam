using System.Collections.Generic;

namespace PlayniteMultiAccountSteamLibrary.Extension.Steam;

public interface ISteamService
{
    List<SteamGameMetadata> GetGames();
}