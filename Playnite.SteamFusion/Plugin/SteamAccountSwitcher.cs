using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SteamFusion.Steam;

namespace Playnite.SteamFusion.Plugin;

public class SteamAccountSwitcher : ISteamAccountSwitcher
{
    private readonly ILogger logger;
    private readonly SteamLibrarySettingsModel settings;
    private readonly ISteamLocalService steamService;

    public SteamAccountSwitcher(SteamLibrarySettingsModel settings)
        : this(settings, LogManager.GetLogger(), new SteamLocalService()) { }

    internal SteamAccountSwitcher(SteamLibrarySettingsModel settings, ILogger logger, ISteamLocalService steamService)
    {
        this.settings = settings;
        this.logger = logger;
        this.steamService = steamService;
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

        var switchSuccess = await PerformAccountSwitch(desiredSteamId, cancellationToken);
        if (!switchSuccess)
        {
            return false;
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

    private async Task WaitForProcessExitAsync(Process process, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            while (!process.HasExited)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Ignore if already exited or cannot kill
                    }

                    throw new OperationCanceledException(cancellationToken);
                }

                Thread.Sleep(200);
            }
        }, cancellationToken);
    }

    private async Task<bool> PerformAccountSwitch(string desiredSteamId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(this.settings.LauncherLocation))
        {
            this.logger.Error("LauncherLocation is not set. Cannot launch account switch tool.");
            return false;
        }

        var result = false;

        var startInfo = new ProcessStartInfo
        {
            FileName = this.settings.LauncherLocation!,
            Arguments = $"+s:{desiredSteamId} -silent",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            var process = Process.Start(startInfo);
            if (process != null)
            {
                try
                {
                    this.logger.Debug($"Switching Steam account to {desiredSteamId}");

                    await WaitForProcessExitAsync(process, cancellationToken);
                    result = true;

                    this.logger.Info($"Switched Steam account to {desiredSteamId}");
                }
                catch (OperationCanceledException)
                {
                    this.logger.Warn("Waiting for account switch tool process was cancelled.");
                }
                catch (Exception exception)
                {
                    this.logger.Error($"Error while waiting for account switch tool process to exit: {exception.Message}");
                }
            }
            else
            {
                this.logger.Error("Failed to start account switch tool process.");
            }
        }
        catch (Exception exception)
        {
            this.logger.Error($"Failed to launch account switch tool: {exception.Message}");
        }

        return result;
    }
}