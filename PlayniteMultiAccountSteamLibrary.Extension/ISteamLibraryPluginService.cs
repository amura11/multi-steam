namespace PlayniteMultiAccountSteamLibrary.Extension;

public interface ISteamLibraryPluginService
{
    public SteamLibrarySettingsModel? LoadPluginSettings();

    public void SavePluginSettings(SteamLibrarySettingsModel settings);

    public string GetPluginUserDataPath();
}