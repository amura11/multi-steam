using System.Threading.Tasks;

namespace Playnite.SteamFusion.SwitcherTool;

public interface ISwitcherToolService
{
    string GetExecutablePath();

    Task<string?> GetRemoteVersion();

    string? GetLocalVersion();

    bool IsNewerVersion(string? version1, string? version2);

    Task Install();
}