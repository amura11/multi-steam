using System.Windows;
using Playnite.SDK.Plugins;
using PlayniteMultiAccountSteamLibrary.Extension;
using PlayniteMultiAccountSteamLibrary.TestHarness.Mocks;

namespace PlayniteMultiAccountSteamLibrary.TestHarness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MockPlayniteApi mockPlayniteApi;
        private readonly SteamLibraryPlugin libraryPlugin;

        public MainWindow()
        {
            this.mockPlayniteApi = new MockPlayniteApi();
            this.libraryPlugin = new SteamLibraryPlugin(this.mockPlayniteApi);

            InitializeComponent();

            var settingsViewModel = (SteamLibrarySettingsViewModel)this.libraryPlugin.GetSettings(true);
            var settingsView = (SteamLibrarySettingsView)this.libraryPlugin.GetSettingsView(true);

            settingsView.DataContext = settingsViewModel;

            this.ContentHost.Content = settingsView;
        }

        private void OnFetchGameClicked(object sender, RoutedEventArgs e)
        {
            var arguments = new LibraryGetGamesArgs();

            var results = this.libraryPlugin.GetGames(arguments);
        }
    }
}