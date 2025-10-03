using System.Collections.Generic;

namespace PlayniteMultiAccountSteamLibrary.Extension.Steam;

public interface ISteamApiService
{
    List<OwnedSteamGame> GetOwnedGames(bool includeAppInfo = true, bool includePlayedFreeGames = true);

    ApiTestResult TestConnection();
}