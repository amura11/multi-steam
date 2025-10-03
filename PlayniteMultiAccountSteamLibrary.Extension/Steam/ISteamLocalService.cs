using System.Collections.Generic;

namespace PlayniteMultiAccountSteamLibrary.Extension.Steam;

public interface ISteamLocalService
{
    List<InstalledSteamGame> GetInstalledGames();

    string? GetActiveSteamId();

    bool LaunchGame(string gameId);

    bool InstallGame(string gameId);
}