namespace Playnite.SteamFusion;

public interface ISteamLibraryPluginService
{
    public SteamLibrarySettingsModel? LoadPluginSettings();

    public void SavePluginSettings(SteamLibrarySettingsModel settings);

    public string GetPluginUserDataPath();
}