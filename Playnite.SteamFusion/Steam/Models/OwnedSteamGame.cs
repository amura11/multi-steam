using Playnite.SDK.Data;

namespace Playnite.SteamFusion.Steam
{
    public class OwnedSteamGame
    {
        [SerializationPropertyName("appid")]
        public int Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; } = null!;

        [SerializationPropertyName("playtime_forever")]
        public int PlaytimeMinutes { get; set; }

        [SerializationPropertyName("img_icon_url")]
        public string? IconFileName { get; set; }

        [SerializationPropertyName("img_logo_url")]
        public string? LogoFileName { get; set; }

        [SerializationPropertyName("has_community_visible_stats")]
        public bool HasCommunityVisibleStats { get; set; }

        public string? GetIconUrl()
        {
            return string.IsNullOrWhiteSpace(this.IconFileName) ? null : $"https://media.steampowered.com/steamcommunity/public/images/apps/{this.Id}/{this.IconFileName}.jpg";
        }

        public string? GetLogoUrl()
        {
            return string.IsNullOrWhiteSpace(this.IconFileName) ? null : $"https://media.steampowered.com/steamcommunity/public/images/apps/{this.Id}/{this.LogoFileName}.jpg";
        }

        public string? GetStatsUrl(string steamId)
        {
            return this.HasCommunityVisibleStats == false ? null : $"http://steamcommunity.com/profiles/{steamId}/stats/{this.Id}";
        }

        public override string ToString()
        {
            return $"{this.Name} ({this.Id})";
        }
    }
}