using System.Threading;
using System.Threading.Tasks;

namespace Playnite.SteamFusion.AccountSwitcher;

public interface ISteamAccountSwitcher
{
    Task<bool> SwitchToAccount(string steamId, CancellationToken cancellationToken);
}