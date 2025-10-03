using System.Threading;
using System.Threading.Tasks;

namespace PlayniteMultiAccountSteamLibrary.Extension.Plugin;

public interface IProcessWatcher
{
    Task<int?> WaitForStartAsync(CancellationToken cancellationToken);

    Task WaitForEndAsync(CancellationToken cancellationToken);
}