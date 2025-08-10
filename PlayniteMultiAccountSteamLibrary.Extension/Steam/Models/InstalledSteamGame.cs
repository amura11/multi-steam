namespace PlayniteMultiAccountSteamLibrary.Extension.Steam
{
    public class InstalledSteamGame
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string InstallDirectory { get; set; } = null!;

        public string LibraryPath { get; set; } = null!;

        public override string ToString()
        {
            return $"{this.Name} ({this.Id})";
        }
    }
}