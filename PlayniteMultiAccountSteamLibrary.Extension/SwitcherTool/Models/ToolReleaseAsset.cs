using Playnite.SDK.Data;

namespace PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool.Models;

public class ToolReleaseAsset
{
    [SerializationPropertyName("name")]
    public string Name { get; set; } = null!;

    [SerializationPropertyName("browser_download_url")]
    public string DownloadUrl { get; set; } = null!;
}