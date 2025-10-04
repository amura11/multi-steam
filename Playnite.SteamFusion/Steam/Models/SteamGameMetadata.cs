namespace Playnite.SteamFusion.Steam
{
    public class SteamGameMetadata
    {
        public string Name { get; set; } = null!;

        public string GameId { get; set; } = null!;

        public string OwnerName { get; set; } = null!;

        public string OwnerId { get; set; } = null!;

        public bool IsInstalled { get; set; }

        public string? InstallLocation { get; set; }

        public string? LogoUrl { get; set; }

        public string? IconUrl { get; set; }
    }
}