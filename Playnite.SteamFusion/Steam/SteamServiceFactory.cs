namespace Playnite.SteamFusion.Steam;

public class SteamServiceFactory : ISteamServiceFactory
{
    public ISteamService Create(string accountName, string accountId, string apiKey)
    {
        return new SteamService(accountName, accountId, apiKey);
    }
}