using System.Collections.Generic;

namespace Playnite.SteamFusion.Steam;

public interface ISteamService
{
    List<SteamGameMetadata> GetGames();
}