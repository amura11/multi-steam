using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SteamFusion.Steam;
using Playnite.SteamFusion.Extensions;

namespace Playnite.SteamFusion.Plugin;

public class SteamPlayController : PlayController
{
    private readonly ILogger logger;
    private readonly SteamLibrarySettingsModel settings;
    private readonly ISteamLocalService steamService;
    private readonly ISteamAccountSwitcher accountSwitcher;
    private readonly IProcessWatcherFactory processWatcherFactory;
    private readonly CancellationTokenSource cancellationTokenSource;

    public SteamPlayController(Game game, SteamLibrarySettingsModel settings) :
        this(game, settings, LogManager.GetLogger(), new SteamLocalService(), new SteamAccountSwitcher(settings), new ProcessWatcherFactory()) { }

    internal SteamPlayController(Game game, SteamLibrarySettingsModel settings, ILogger logger, ISteamLocalService steamService, ISteamAccountSwitcher accountSwitcher, IProcessWatcherFactory processWatcherFactory) : base(game)
    {
        this.logger = logger;
        this.settings = settings;
        this.steamService = steamService;
        this.accountSwitcher = accountSwitcher;
        this.processWatcherFactory = processWatcherFactory;
        this.cancellationTokenSource = new CancellationTokenSource();
    }

    public override void Dispose()
    {
        this.logger.Debug($"Disposing SteamPlayController for game: {this.Game?.Name}");
        base.Dispose();

        this.cancellationTokenSource.Cancel();
        this.cancellationTokenSource.Dispose();
    }

    public override void Play(PlayActionArgs args)
    {
        if (this.Game == null)
        {
            throw new InvalidOperationException("Game is null");
        }

        this.logger.Info($"Attempting to play game: {this.Game?.Name} (Id: {this.Game?.Id})");

        var steamId = this.Game!.GetSteamId();
        var applicationId = this.Game!.GetApplicationId();

        this.logger.Debug($"Resolved SteamId: {steamId}, ApplicationId: {applicationId} for game: {this.Game?.Name}");

        if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(applicationId))
        {
            this.logger.Error($"Can't start game, invalid ID in game: {this.Game?.Name}, SteamId: {steamId}, ApplicationId: {applicationId}");
            throw new Exception("Can't start game, invalid ID in game");
        }

        Task.Run(() => StartAndMonitorGame(steamId!, applicationId!, this.Game!.InstallDirectory, this.cancellationTokenSource.Token));
    }

    private async Task StartAndMonitorGame(string steamId, string applicationId, string installationDirectory, CancellationToken cancellationToken)
    {
        Stopwatch? stopwatch = null;

        try
        {
            this.logger.Info($"Starting and monitoring game: {this.Game?.Name} (SteamId: {steamId}, AppId: {applicationId})");

            var switchResult = await this.accountSwitcher.SwitchToAccount(steamId, cancellationToken);

            if (switchResult == false)
            {
                this.logger.Error($"Failed to switch to Steam account {steamId} for game: {this.Game?.Name}");
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }

            var launchResult = this.steamService.LaunchGame(applicationId);

            if (launchResult == false)
            {
                this.logger.Error($"Failed to launch game: {this.Game?.Name} (AppId: {applicationId})");
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }

            var processWatcher = this.processWatcherFactory.Create(installationDirectory, this.settings.PollingInterval, 2000, this.settings.LaunchTimeout);

            var startedProcessId = await processWatcher.WaitForStartAsync(cancellationToken);

            if (startedProcessId.HasValue == false)
            {
                this.logger.Error($"Game process for {this.Game?.Name} did not start within the expected time.");
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }

            stopwatch = Stopwatch.StartNew();

            this.logger.Info($"Game process started for {this.Game?.Name} (ProcessId: {startedProcessId.Value})");

            InvokeOnStarted(new GameStartedEventArgs());

            await processWatcher.WaitForEndAsync(cancellationToken);

            this.logger.Info($"Game process ended for {this.Game?.Name} (ProcessId: {startedProcessId.Value})");

            InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(stopwatch.Elapsed.TotalSeconds)));
        }
        catch (Exception exception)
        {
            this.logger.Error(exception, $"Exception occurred while starting or monitoring game: {this.Game?.Name}");
            InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(stopwatch?.Elapsed.TotalSeconds ?? 0)));
        }
    }
}