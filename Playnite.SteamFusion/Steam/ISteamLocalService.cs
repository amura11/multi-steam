using System.Collections.Generic;

namespace Playnite.SteamFusion.Steam;

public interface ISteamLocalService
{
    List<InstalledSteamGame> GetInstalledGames();

    string? GetActiveSteamId();

    bool LaunchGame(string gameId);

    bool InstallGame(string gameId);

    bool IsGameInstalled(string gameId);

    InstalledSteamGame? GetInstallInformation(string applicationId);
}