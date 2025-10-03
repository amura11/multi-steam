using System.Collections.ObjectModel;

namespace PlayniteMultiAccountSteamLibrary.Extension;

public interface ISteamLibrarySettingsModel
{
    ObservableCollection<SteamAccountSettingsModel> SteamAccountSettings { get; set; }

    SwitcherToolInstallationType SwitcherToolInstallationType { get; set; }

    string? LauncherLocation { get; set; }

    int StartupDelay { get; set; }

    int SwitchTimeout { get; set; }

    int LaunchTimeout { get; set; }

    int PollingInterval { get; set; }
}