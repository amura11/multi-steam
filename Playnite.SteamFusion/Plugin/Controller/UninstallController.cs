using System;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SteamFusion.AccountSwitcher;
using Playnite.SteamFusion.Steam;
using Playnite.SteamFusion.Extensions;
using PlayniteUninstallController = Playnite.SDK.Plugins.UninstallController;

namespace Playnite.SteamFusion.Plugin;

public class UninstallController : PlayniteUninstallController
{
    private readonly ILogger logger;
    private readonly SteamLibrarySettingsModel settings;
    private readonly ISteamLocalService steamService;
    private readonly ISteamAccountSwitcher accountSwitcher;
    private readonly CancellationTokenSource cancellationTokenSource;

    public UninstallController(Game game, SteamLibrarySettingsModel settings) :
        this(game, settings, LogManager.GetLogger(), new SteamLocalService(), new SteamAccountSwitcher(settings)) { }

    internal UninstallController(Game game, SteamLibrarySettingsModel settings, ILogger logger, ISteamLocalService steamService, ISteamAccountSwitcher accountSwitcher) : base(game)
    {
        this.logger = logger;
        this.settings = settings;
        this.steamService = steamService;
        this.accountSwitcher = accountSwitcher;
        this.cancellationTokenSource = new CancellationTokenSource();
    }

    public override void Dispose()
    {
        this.logger.Debug($"Disposing UninstallController for game: {this.Game?.Name}");
        base.Dispose();

        this.cancellationTokenSource.Cancel();
        this.cancellationTokenSource.Dispose();
    }

    public override void Uninstall(UninstallActionArgs args)
    {
        if (this.Game == null)
        {
            throw new InvalidOperationException("Game is null");
        }

        this.logger.Info($"Attempting to uninstall game: {this.Game?.Name} (Id: {this.Game?.Id})");

        var steamId = this.Game!.GetSteamId();
        var applicationId = this.Game!.GetApplicationId();

        this.logger.Debug($"Resolved SteamId: {steamId}, ApplicationId: {applicationId} for game: {this.Game?.Name}");

        if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(applicationId))
        {
            this.logger.Error($"Can't uninstall game, invalid ID in game: {this.Game?.Name}, SteamId: {steamId}, ApplicationId: {applicationId}");
            throw new Exception("Can't uninstall game, invalid ID in game");
        }

        Task.Run(() => StartAndMonitorUninstallation(steamId!, applicationId!, this.cancellationTokenSource.Token));
    }

    private async Task StartAndMonitorUninstallation(string steamId, string applicationId, CancellationToken cancellationToken)
    {
        try
        {
            this.logger.Info($"Starting and monitoring uninstallation: {this.Game?.Name} (SteamId: {steamId}, AppId: {applicationId})");

            var switchResult = await this.accountSwitcher.SwitchToAccount(steamId, cancellationToken);

            if (switchResult == false)
            {
                this.logger.Error($"Failed to switch to Steam account {steamId} for game: {this.Game?.Name}");
                return;
            }

            var uninstallResult = this.steamService.UninstallGame(applicationId);

            if (uninstallResult == false)
            {
                this.logger.Error($"Failed to start uninstallation for game: {this.Game?.Name} (AppId: {applicationId})");
                return;
            }

            await MonitorUninstallation(applicationId, cancellationToken);
            
            this.logger.Info($"Uninstallation completed for game: {this.Game?.Name}");
            InvokeOnUninstalled(new GameUninstalledEventArgs());
        }
        catch (Exception exception)
        {
            this.logger.Error(exception, $"Exception occurred while uninstalling game: {this.Game?.Name}");
            InvokeOnUninstalled(new GameUninstalledEventArgs());
        }
    }

    private async Task MonitorUninstallation(string applicationId, CancellationToken cancellationToken)
    {
        var pollingInterval = TimeSpan.FromMilliseconds(this.settings.PollingInterval * 10);
        var installInformation = this.steamService.GetInstallInformation(applicationId);

        while (cancellationToken.IsCancellationRequested == false && installInformation != null)
        {
            await Task.Delay(pollingInterval, cancellationToken);
            
            installInformation = this.steamService.GetInstallInformation(applicationId);
        }
    }
}