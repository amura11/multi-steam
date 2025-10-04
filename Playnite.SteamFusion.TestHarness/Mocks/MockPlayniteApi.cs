using System;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Playnite.SteamFusion.TestHarness.Mocks
{
    public class MockPlayniteApi  : IPlayniteAPI
    {
        public string ExpandGameVariables(Game game, string inputString)
        {
            throw new NotImplementedException();
        }

        public string ExpandGameVariables(Game game, string inputString, string emulatorDir)
        {
            throw new NotImplementedException();
        }

        public GameAction ExpandGameVariables(Game game, GameAction action)
        {
            throw new NotImplementedException();
        }

        public void StartGame(Guid gameId)
        {
            throw new NotImplementedException();
        }

        public void InstallGame(Guid gameId)
        {
            throw new NotImplementedException();
        }

        public void UninstallGame(Guid gameId)
        {
            throw new NotImplementedException();
        }

        public void AddCustomElementSupport(SDK.Plugins.Plugin source, AddCustomElementSupportArgs args)
        {
            throw new NotImplementedException();
        }

        public void AddSettingsSupport(SDK.Plugins.Plugin source, AddSettingsSupportArgs args)
        {
            throw new NotImplementedException();
        }

        public void AddConvertersSupport(SDK.Plugins.Plugin source, AddConvertersSupportArgs args)
        {
            throw new NotImplementedException();
        }

        public IMainViewAPI MainView { get; set; }

        public IGameDatabaseAPI Database { get; set; }

        public IDialogsFactory Dialogs { get; set; }

        public IPlaynitePathsAPI Paths { get; } = new MockPlaynitePathsApi();

        public INotificationsAPI Notifications { get; set; }

        public IPlayniteInfoAPI ApplicationInfo { get; set; }

        public IWebViewFactory WebViews { get; set; }

        public IResourceProvider Resources { get; set; }

        public IUriHandlerAPI UriHandler { get; set; }

        public IPlayniteSettingsAPI ApplicationSettings { get; set; }

        public IAddons Addons { get; set; }

        public IEmulationAPI Emulation { get; set; }
    }
}