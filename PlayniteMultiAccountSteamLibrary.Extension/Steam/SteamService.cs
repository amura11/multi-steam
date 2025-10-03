using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;

namespace PlayniteMultiAccountSteamLibrary.Extension.Steam
{
    public class SteamService : ISteamService
    {
        private static readonly HashSet<int> ExcludedGameIds = new HashSet<int>() { 228980 };
        private readonly string accountName;
        private readonly string steamId;
        private readonly ISteamApiService apiService;
        private readonly ISteamLocalService localService;
        private readonly ILogger logger;

        /// <summary>
        /// Creates a new instance of the <see cref="SteamService"/>.
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="accountId"></param>
        /// <param name="apiKey"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SteamService(string accountName, string accountId, string apiKey)
            : this(accountName, accountId, LogManager.GetLogger(), new SteamApiService(accountId, apiKey), new SteamLocalService()) { }

        internal SteamService(string accountName, string accountId, ILogger logger, ISteamApiService apiService, ISteamLocalService localService)
        {
            this.accountName = accountName ?? throw new ArgumentNullException(nameof(accountName));
            this.steamId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            this.logger = logger;
            this.apiService = apiService;
            this.localService = localService;
        }

        public List<SteamGameMetadata> GetGames()
        {
            var toReturn = new List<SteamGameMetadata>();

            var ownedGames = this.apiService.GetOwnedGames();
            var installedGames = this.localService.GetInstalledGames();

            foreach (var ownedGame in ownedGames)
            {
                if (ExcludedGameIds.Contains(ownedGame.Id))
                {
                    continue;
                }

                var installedGame = installedGames.FirstOrDefault(x => x.Id == ownedGame.Id);

                var gameMetadata = new SteamGameMetadata()
                {
                    Name = ownedGame.Name,
                    GameId = ownedGame.Id.ToString(),
                    OwnerName = this.accountName,
                    OwnerId = this.steamId,
                    LogoUrl = ownedGame.GetIconUrl(),
                    IsInstalled = installedGame != null,
                    InstallLocation = installedGame?.InstallDirectory
                };

                toReturn.Add(gameMetadata);
            }

            return toReturn;
        }
    }
}