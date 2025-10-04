namespace Playnite.SteamFusion.Plugin;

public class ProcessWatcherFactory : IProcessWatcherFactory
{
    public IProcessWatcher Create(string targetDirectory, int pollingInterval, int stabilizationInterval, int startTimeout)
    {
        return new ProcessWatcher(targetDirectory, pollingInterval, stabilizationInterval, startTimeout);
    }
}