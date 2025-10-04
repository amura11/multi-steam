using System;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SteamFusion.AccountSwitcher;
using Playnite.SteamFusion.Steam;
using Playnite.SteamFusion.Extensions;
using PlayniteInstallController = Playnite.SDK.Plugins.InstallController;

namespace Playnite.SteamFusion.Plugin;

public class InstallController : PlayniteInstallController
{
    private readonly ILogger logger;
    private readonly SteamLibrarySettingsModel settings;
    private readonly ISteamLocalService steamService;
    private readonly ISteamAccountSwitcher accountSwitcher;
    private readonly CancellationTokenSource cancellationTokenSource;

    public InstallController(Game game, SteamLibrarySettingsModel settings) :
        this(game, settings, LogManager.GetLogger(), new SteamLocalService(), new SteamAccountSwitcher(settings)) { }

    internal InstallController(Game game, SteamLibrarySettingsModel settings, ILogger logger, ISteamLocalService steamService, ISteamAccountSwitcher accountSwitcher) : base(game)
    {
        this.logger = logger;
        this.settings = settings;
        this.steamService = steamService;
        this.accountSwitcher = accountSwitcher;
        this.cancellationTokenSource = new CancellationTokenSource();
    }

    public override void Dispose()
    {
        this.logger.Debug($"Disposing InstallController for game: {this.Game?.Name}");
        base.Dispose();

        this.cancellationTokenSource.Cancel();
        this.cancellationTokenSource.Dispose();
    }

    public override void Install(InstallActionArgs args)
    {
        if (this.Game == null)
        {
            throw new InvalidOperationException("Game is null");
        }

        this.logger.Info($"Attempting to install game: {this.Game?.Name} (Id: {this.Game?.Id})");

        var steamId = this.Game!.GetSteamId();
        var applicationId = this.Game!.GetApplicationId();

        this.logger.Debug($"Resolved SteamId: {steamId}, ApplicationId: {applicationId} for game: {this.Game?.Name}");

        if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(applicationId))
        {
            this.logger.Error($"Can't install game, invalid ID in game: {this.Game?.Name}, SteamId: {steamId}, ApplicationId: {applicationId}");
            throw new Exception("Can't install game, invalid ID in game");
        }

        Task.Run(() => StartAndMonitorInstallation(steamId!, applicationId!, this.cancellationTokenSource.Token));
    }

    private async Task StartAndMonitorInstallation(string steamId, string applicationId, CancellationToken cancellationToken)
    {
        try
        {
            this.logger.Info($"Starting and monitoring installation: {this.Game?.Name} (SteamId: {steamId}, AppId: {applicationId})");

            var switchResult = await this.accountSwitcher.SwitchToAccount(steamId, cancellationToken);

            if (switchResult == false)
            {
                this.logger.Error($"Failed to switch to Steam account {steamId} for game: {this.Game?.Name}");
                return;
            }

            var installResult = this.steamService.InstallGame(applicationId);

            if (installResult == false)
            {
                this.logger.Error($"Failed to start installation for game: {this.Game?.Name} (AppId: {applicationId})");
                return;
            }

            var installInformation = await MonitorInstallation(applicationId, cancellationToken);

            if (installInformation != null)
            {
                this.logger.Info($"Installation completed for game: {this.Game?.Name}");
                
                var eventArgs = new GameInstalledEventArgs()
                {
                    InstalledInfo = new GameInstallationData()
                    {
                        InstallDirectory = installInformation.InstallDirectory
                    }
                };
                
                InvokeOnInstalled(eventArgs);
            }
            else
            {
                InvokeOnInstalled(new GameInstalledEventArgs());
            }
        }
        catch (Exception exception)
        {
            this.logger.Error(exception, $"Exception occurred while installing game: {this.Game?.Name}");
            InvokeOnInstalled(new GameInstalledEventArgs());
        }
    }

    private async Task<InstalledSteamGame?> MonitorInstallation(string applicationId, CancellationToken cancellationToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(this.settings.PollingInterval);
        InstalledSteamGame? installInformation = null;

        while (cancellationToken.IsCancellationRequested == false && installInformation == null)
        {
            installInformation = this.steamService.GetInstallInformation(applicationId);

            await Task.Delay(pollingInterval, cancellationToken);
        }

        return installInformation;
    }
}