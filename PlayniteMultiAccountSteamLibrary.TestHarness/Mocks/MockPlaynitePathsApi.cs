using System;
using Playnite.SDK;

namespace PlayniteMultiAccountSteamLibrary.TestHarness.Mocks
{
    public class MockPlaynitePathsApi : IPlaynitePathsAPI
    {
        public bool IsPortable { get; set; }

        public string ApplicationPath { get; set; }

        public string ConfigurationPath { get; set; }

        public string ExtensionsDataPath => AppDomain.CurrentDomain.BaseDirectory;
    }
}