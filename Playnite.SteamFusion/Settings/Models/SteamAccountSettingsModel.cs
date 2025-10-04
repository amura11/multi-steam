using System.Collections.Generic;

namespace Playnite.SteamFusion
{
    public class SteamAccountSettingsModel : ObservableObject
    {
        private string name = null!;
        private string id = null!;
        private string key = null!;

        public SteamAccountSettingsModel Clone()
        {
            return new SteamAccountSettingsModel()
            {
                id = this.id,
                name = this.Name,
                key = this.key
            };
        }

        public void Merge(SteamAccountSettingsModel other)
        {
            this.Name = other.Name;
            this.Key = other.Key;
        }
        
        public string Name
        {
            get => this.name;
            set => SetValue(ref this.name, value);
        }

        public string Id
        {
            get => this.id;
            set => SetValue(ref this.id, value);
        }

        public string Key
        {
            get => this.key;
            set => SetValue(ref this.key, value);
        }
    }
}