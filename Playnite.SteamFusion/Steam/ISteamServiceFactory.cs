namespace Playnite.SteamFusion.Steam;

public interface ISteamServiceFactory
{
    ISteamService Create(string accountName, string accountId, string apiKey);
}