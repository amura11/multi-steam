using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using PlayniteMultiAccountSteamLibrary.Extension.Steam;

namespace PlayniteMultiAccountSteamLibrary.Extension.Plugin;

public class SteamAccountSwitcher
{
    private readonly ILogger logger;
    private readonly SteamLibrarySettingsModel settings;
    private readonly SteamLocalService steamService;

    public SteamAccountSwitcher(SteamLibrarySettingsModel settings, ILogger? logger = null, SteamLocalService? steamService = null)
    {
        this.settings = settings;
        this.logger = logger ?? LogManager.GetLogger();
        this.steamService = steamService ?? new SteamLocalService();
    }

    public async Task<bool> SwitchToAccount(string steamId, CancellationToken cancellationToken)
    {
        var success = false;

        if (await WaitForAccountSwitch(steamId, cancellationToken))
        {
            success = await WaitForSteamLaunch(cancellationToken);
        }

        return success;
    }

    private async Task<bool> WaitForAccountSwitch(string desiredSteamId, CancellationToken cancellationToken)
    {
        if (this.steamService.GetActiveSteamId() == desiredSteamId)
        {
            this.logger.Info($"Steam active steam user is {desiredSteamId}, no switching needed");
            return true;
        }

        this.logger.Debug("Waiting for Steam user switch...");

        var isSwitched = false;
        var elapsed = 0;

        while (isSwitched == false && elapsed < (this.settings.SwitchTimeout * 1000))
        {
            var activeSteamId = this.steamService.GetActiveSteamId();

            if (activeSteamId == desiredSteamId)
            {
                this.logger.Info($"Steam user switched to {desiredSteamId} after {elapsed / 1000.0:N1} seconds");
                isSwitched = true;
            }
            else
            {
                this.logger.Debug($"Active Steam user is {activeSteamId}, waiting for {desiredSteamId}...");

                await Task.Delay(this.settings.PollingInterval, cancellationToken);
                elapsed += this.settings.PollingInterval;
            }
        }

        if (isSwitched == false)
        {
            this.logger.Warn("Timeout waiting for Steam account switch.");
        }

        return isSwitched;
    }

    private async Task<bool> WaitForSteamLaunch(CancellationToken cancellationToken)
    {
        if (Process.GetProcessesByName("steam").Length > 0)
        {
            this.logger.Info("Steam already started");
            return true;
        }

        this.logger.Debug("Waiting for Steam to start...");

        var elapsed = 0;
        var isRunning = false;

        while (isRunning == false && elapsed < (this.settings.LaunchTimeout * 1000))
        {
            if (Process.GetProcessesByName("steam").Length > 0)
            {
                this.logger.Info($"Steam is running after {elapsed / 1000.0:N1} seconds");
                isRunning = true;
            }
            else
            {
                this.logger.Debug("Steam process not started, waiting...");

                await Task.Delay(this.settings.PollingInterval, cancellationToken);
                elapsed += this.settings.PollingInterval;
            }
        }

        if (isRunning == false)
        {
            this.logger.Warn("Timeout waiting for Steam to start.");
        }

        return isRunning;
    }
}