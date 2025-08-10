using Playnite.SDK.Models;

namespace PlayniteMultiAccountSteamLibrary.Extension.Extensions;

public static class GameExtensions
{
    public static string? GetSteamId(this Game game)
    {
        var parts = game.GameId.Split(':');

        return parts.Length != 2 ? null : parts[0];
    }
    
    public static string? GetApplicationId(this Game game)
    {
        var parts = game.GameId.Split(':');

        return parts.Length != 2 ? null : parts[1];
    }
}