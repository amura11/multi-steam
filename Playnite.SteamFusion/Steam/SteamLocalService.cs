using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using Playnite.SDK;

namespace Playnite.SteamFusion.Steam
{
    public class SteamLocalService : ISteamLocalService
    {
        private const string SteamExecutable = "steam.exe";

        private static string? steamInstallPath = null;
        private static DateTime? loginFileLastWriteTime = null;
        private static string? activeSteamId = null;
        private static readonly object lockObject = new object();

        private static List<InstalledSteamGame>? cachedGames = null;
        private static DateTime? lastMaxWriteTime = null;

        private readonly ILogger logger;

        public SteamLocalService()
            : this(LogManager.GetLogger()) { }

        internal SteamLocalService(ILogger logger)
        {
            this.logger = logger;
        }

        private string SteamInstallPath
        {
            get
            {
                if (steamInstallPath == null)
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

                    if (key?.GetValue("SteamPath") is not string path)
                    {
                        throw new Exception("Unable to find steam install path");
                    }

                    steamInstallPath = path;
                }

                return steamInstallPath;
            }
        }

        public List<InstalledSteamGame> GetInstalledGames()
        {
            var configPath = Path.Combine(this.SteamInstallPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(configPath))
            {
                return new List<InstalledSteamGame>();
            }

            var libraryFolders = ParseLibraryFoldersManifest(configPath);
            var currentMaxWriteTime = File.GetLastWriteTimeUtc(configPath);

            // Find the newest write time across all manifest files
            foreach (var libraryPath in libraryFolders.Values)
            {
                var steamAppsPath = Path.Combine(libraryPath, "steamapps");
                foreach (var manifestFile in Directory.EnumerateFiles(steamAppsPath, "appmanifest_*.acf"))
                {
                    var writeTime = File.GetLastWriteTimeUtc(manifestFile);
                    if (writeTime > currentMaxWriteTime)
                    {
                        currentMaxWriteTime = writeTime;
                    }
                }
            }

            if (cachedGames == null || lastMaxWriteTime != currentMaxWriteTime)
            {
                var installedGames = new List<InstalledSteamGame>();

                foreach (var libraryPath in libraryFolders.Values)
                {
                    var steamAppsPath = Path.Combine(libraryPath, "steamapps");

                    foreach (var manifestFile in Directory.EnumerateFiles(steamAppsPath, "appmanifest_*.acf"))
                    {
                        var game = ParseGameManifest(manifestFile, libraryPath);
                        installedGames.Add(game);
                    }
                }

                cachedGames = installedGames;
                lastMaxWriteTime = currentMaxWriteTime;
            }

            return cachedGames;
        }

        public InstalledSteamGame? GetInstallInformation(string applicationId)
        {
            var parsedId = int.Parse(applicationId);
            
            var installedGames = GetInstalledGames();

            var game = installedGames.FirstOrDefault(x => x.Id == parsedId);

            return game;
        }

        public string? GetActiveSteamId()
        {
            var loginUsersPath = Path.Combine(this.SteamInstallPath, "config", "loginusers.vdf");

            if (File.Exists(loginUsersPath) == false)
            {
                return null;
            }

            lock (lockObject)
            {
                var loginFileWriteTime = File.GetLastWriteTimeUtc(loginUsersPath);

                if (loginFileLastWriteTime == null || loginFileLastWriteTime != loginFileWriteTime)
                {
                    using var reader = new StreamReader(loginUsersPath);
                    var vdf = VdfConvert.Deserialize(reader);
                    var users = vdf.Value;
                    string? mostRecentSteamId = null;

                    foreach (var user in users.Children<VProperty>())
                    {
                        var mostRecent = user.Value["MostRecent"]?.ToString();
                        if (mostRecent == "1")
                        {
                            mostRecentSteamId = user.Key;
                            break;
                        }
                    }

                    activeSteamId = mostRecentSteamId;
                    loginFileLastWriteTime = loginFileWriteTime;
                }

                return activeSteamId;
            }
        }

        public bool LaunchGame(string gameId)
        {
            var arguments = $"-silent \"steam://rungameid/{gameId}\"";

            return Run(arguments);
        }

        public bool InstallGame(string gameId)
        {
            var arguments = $"steam://install/{gameId}";

            return Run(arguments);
        }

        public bool IsGameInstalled(string gameId)
        {
            var found = false;

            if (int.TryParse(gameId, out var id))
            {
                var installedGames = GetInstalledGames();

                found = installedGames.Any(x => x.Id == id);
            }

            return found;
        }

        private bool Run(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(this.SteamInstallPath, SteamExecutable),
                Arguments = arguments,
                UseShellExecute = false
            };

            var process = Process.Start(processStartInfo);

            return process != null;
        }

        private Dictionary<string, string> ParseLibraryFoldersManifest(string manifestPath)
        {
            var libraries = new Dictionary<string, string>();

            using var reader = new StreamReader(manifestPath);
            var vdf = VdfConvert.Deserialize(reader);

            foreach (var entry in vdf.Value.Children<VProperty>())
            {
                if (int.TryParse(entry.Key, out _))
                {
                    var path = entry.Value["path"]?.ToString();
                    if (string.IsNullOrEmpty(path) == false)
                    {
                        libraries[entry.Key] = path!.Replace(@"\\", @"\");
                    }
                }
            }

            return libraries;
        }

        private InstalledSteamGame ParseGameManifest(string manifestPath, string libraryPath)
        {
            using var reader = new StreamReader(manifestPath);
            var vdf = VdfConvert.Deserialize(reader);

            var appState = vdf.Value ?? throw new NullReferenceException("No app state in manifest");
            var gameId = appState["appid"]?.ToString() ?? throw new NullReferenceException("No app ID manifest");
            var name = appState["name"]?.ToString() ?? throw new NullReferenceException("No Name in manifest");
            var installDir = appState["installdir"]?.ToString() ?? throw new NullReferenceException("No install directory in manifest");
            var fullPath = Path.Combine(libraryPath, "steamapps", "common", installDir);

            return new InstalledSteamGame()
            {
                Id = int.Parse(gameId),
                Name = name,
                InstallDirectory = fullPath,
                LibraryPath = libraryPath
            };
        }
    }
}