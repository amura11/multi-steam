namespace Playnite.SteamFusion.Plugin;

public interface IProcessWatcherFactory
{
    IProcessWatcher Create(string targetDirectory, int pollingInterval, int stabilizationInterval, int startTimeout);
}