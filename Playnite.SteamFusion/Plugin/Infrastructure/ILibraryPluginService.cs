namespace Playnite.SteamFusion.Plugin;

public interface ILibraryPluginService
{
    public SteamLibrarySettingsModel? LoadPluginSettings();

    public void SavePluginSettings(SteamLibrarySettingsModel settings);

    public string GetPluginUserDataPath();
}