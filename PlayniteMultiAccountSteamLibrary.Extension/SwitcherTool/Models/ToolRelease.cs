using System.Collections.Generic;
using Playnite.SDK.Data;

namespace PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool.Models;

public class ToolRelease
{
    [SerializationPropertyName("tag_name")]
    public string? TagName { get; set; }

    [SerializationPropertyName("assets")]
    public List<ToolReleaseAsset> Assets { get; set; } = new List<ToolReleaseAsset>();
}