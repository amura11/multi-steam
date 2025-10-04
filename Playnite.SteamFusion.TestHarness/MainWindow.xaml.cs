using System.Windows;
using Playnite.SDK.Plugins;
using Playnite.SteamFusion;
using Playnite.SteamFusion.Plugin;
using Playnite.SteamFusion.TestHarness.Mocks;
using LibraryPlugin = Playnite.SteamFusion.Plugin.LibraryPlugin;

namespace Playnite.SteamFusion.TestHarness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MockPlayniteApi mockPlayniteApi;
        private readonly LibraryPlugin libraryPluginService;

        public MainWindow()
        {
            this.mockPlayniteApi = new MockPlayniteApi();
            this.libraryPluginService = new LibraryPlugin(this.mockPlayniteApi);

            InitializeComponent();

            var settingsViewModel = (SteamLibrarySettingsViewModel)this.libraryPluginService.GetSettings(true);
            var settingsView = (SteamLibrarySettingsView)this.libraryPluginService.GetSettingsView(true);

            settingsView.DataContext = settingsViewModel;

            this.ContentHost.Content = settingsView;
        }

        private void OnFetchGameClicked(object sender, RoutedEventArgs e)
        {
            var arguments = new LibraryGetGamesArgs();

            var results = this.libraryPluginService.GetGames(arguments);
        }
    }
}