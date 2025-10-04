using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace Playnite.SteamFusion.Steam
{
    public class SteamApiService : ISteamApiService
    {
        private const string SteamApiBaseUrl = "https://api.steampowered.com";
        private readonly string apiKey;
        private readonly string steamId;
        private readonly ILogger logger;

        public SteamApiService(string steamId, string apiKey)
            : this(steamId, apiKey, LogManager.GetLogger()) { }

        internal SteamApiService(string steamId, string apiKey, ILogger logger)
        {
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.steamId = steamId ?? throw new ArgumentNullException(nameof(steamId));
            this.logger = logger;
        }

        public List<OwnedSteamGame> GetOwnedGames(bool includeAppInfo = true, bool includePlayedFreeGames = true)
        {
            if (string.IsNullOrWhiteSpace(this.steamId))
            {
                throw new ArgumentException("Steam ID must not be null or empty.", nameof(this.steamId));
            }

            var queryParameters = new Dictionary<string, string>
            {
                { "key", this.apiKey },
                { "steamid", this.steamId },
                { "include_appinfo", includeAppInfo.ToString().ToLowerInvariant() },
                { "include_played_free_games", includePlayedFreeGames.ToString().ToLowerInvariant() }
            };

            var requestUri = BuildSteamApiUrl("/IPlayerService/GetOwnedGames/v1/", queryParameters);
            var request = WebRequest.CreateHttp(requestUri);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException($"Request for games returned a non-ok response: {response.StatusCode}: {response.StatusDescription}");
                }

                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        throw new InvalidOperationException("Response stream was null.");
                    }

                    using (var responseReader = new StreamReader(responseStream))
                    {
                        var responseData = responseReader.ReadToEnd();
                        var result = Serialization.FromJson<GetOwnedGamesResponse>(responseData);
                        return result?.Response.Games ?? new List<OwnedSteamGame>();
                    }
                }
            }
        }

        public ApiTestResult TestConnection()
        {
            ApiTestResult result;

            try
            {
                var queryParameters = new Dictionary<string, string>
                {
                    { "key", this.apiKey },
                    { "steamid", this.steamId },
                    { "include_appinfo", "false" },
                    { "include_played_free_games", "false" }
                };

                var requestUri = BuildSteamApiUrl("/IPlayerService/GetOwnedGames/v1/", queryParameters);
                var request = WebRequest.CreateHttp(requestUri);

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null)
                            {
                                using (var responseReader = new StreamReader(responseStream))
                                {
                                    var responseData = responseReader.ReadToEnd();

                                    if (!string.IsNullOrWhiteSpace(responseData))
                                    {
                                        result = new ApiTestResult()
                                        {
                                            Success = true
                                        };
                                    }
                                    else
                                    {
                                        result = new ApiTestResult()
                                        {
                                            Success = false,
                                            ErrorMessage = "Steam API returned empty response."
                                        };
                                    }
                                }
                            }
                            else
                            {
                                result = new ApiTestResult()
                                {
                                    Success = false,
                                    ErrorMessage = "Steam API response stream was null."
                                };
                            }
                        }
                    }
                    else
                    {
                        result = new ApiTestResult()
                        {
                            Success = false,
                            ErrorMessage = $"Steam API returned: {response.StatusCode} {response.StatusDescription}"
                        };
                    }
                }
            }
            catch (Exception exception)
            {
                result = new ApiTestResult()
                {
                    Success = false,
                    ErrorMessage = $"Exception: {exception.Message}"
                };
            }

            return result;
        }

        private Uri BuildSteamApiUrl(string relativePath, Dictionary<string, string> queryParameters)
        {
            var uriBuilder = new UriBuilder($"{SteamApiBaseUrl}{relativePath}");
            var queryString = new StringBuilder();

            foreach (var parameter in queryParameters)
            {
                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }

                queryString.Append(Uri.EscapeDataString(parameter.Key));
                queryString.Append('=');
                queryString.Append(Uri.EscapeDataString(parameter.Value));
            }

            uriBuilder.Query = queryString.ToString();
            return uriBuilder.Uri;
        }
    }
}