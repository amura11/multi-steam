using System.Collections.Generic;

namespace Playnite.SteamFusion.Steam;

public interface ISteamApiService
{
    List<OwnedSteamGame> GetOwnedGames(bool includeAppInfo = true, bool includePlayedFreeGames = true);

    ApiTestResult TestConnection();
}