using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using PlayniteMultiAccountSteamLibrary.Extension.Plugin;
using PlayniteMultiAccountSteamLibrary.Extension.Steam;

// ReSharper disable ConvertClosureToMethodGroup

namespace PlayniteMultiAccountSteamLibrary.Extension
{
    public class SteamLibraryPlugin : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly SteamLibrarySettingsViewModel settingsViewModel;

        public SteamLibraryPlugin(IPlayniteAPI api) : base(api)
        {
            this.settingsViewModel = new SteamLibrarySettingsViewModel(this, api);

            this.Properties = new LibraryPluginProperties()
            {
                HasSettings = true
            };
        }

        public override Guid Id => Guid.Parse("eb07dae3-4021-4734-abb6-e8a121894d48");

        public override string Name => "Multi Account Steam Library";

        public override LibraryClient Client { get; } = new SteamLibraryClient();

        private SteamLibrarySettingsModel Settings => this.settingsViewModel.Settings;
        
        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            var allGameMetadata = this.Settings.SteamAccountSettings.SelectMany(x => ProcessSteamAccountGames(x))
                .ToList();

            var toReturn = allGameMetadata.Select(x => ProcessSteamGameMetadata(x))
                .ToList();

            return toReturn;
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != this.Id)
            {
                yield break;
            }

            yield return new SteamPlayController(args.Game, this.Settings, LogManager.GetLogger(nameof(SteamPlayController)));
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return this.settingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SteamLibrarySettingsView();
        }

        private List<SteamGameMetadata> ProcessSteamAccountGames(SteamAccountSettingsModel accountSettings)
        {
            logger.Debug($"Processing games for {accountSettings.Name}({accountSettings.Id})");

            var steamService = new SteamService(accountSettings.Name, accountSettings.Id, accountSettings.Key);
            var games = steamService.GetGames();

            logger.Debug($"Processed {games.Count} games from {accountSettings.Name}({accountSettings.Id})'s library");

            return games;
        }

        private GameMetadata ProcessSteamGameMetadata(SteamGameMetadata steamGameMetadata)
        {
            var icon = string.IsNullOrWhiteSpace(steamGameMetadata.IconUrl) == false ? new MetadataFile(steamGameMetadata.IconUrl) : null;

            return new GameMetadata()
            {
                Name = steamGameMetadata.Name,
                GameId = $"{steamGameMetadata.OwnerId}:{steamGameMetadata.GameId}",
                IsInstalled = steamGameMetadata.IsInstalled,
                InstallDirectory = steamGameMetadata.InstallLocation,
                Icon = icon,
                Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                Source = new MetadataNameProperty("Steam"),
                Tags = new HashSet<MetadataProperty>()
                {
                    new MetadataNameProperty(steamGameMetadata.OwnerName),
                    new MetadataNameProperty(steamGameMetadata.OwnerId)
                }
            };
        }
    }
}