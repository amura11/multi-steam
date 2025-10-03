using System.Threading;
using System.Threading.Tasks;

namespace PlayniteMultiAccountSteamLibrary.Extension.Plugin;

public interface ISteamAccountSwitcher
{
    Task<bool> SwitchToAccount(string steamId, CancellationToken cancellationToken);
}