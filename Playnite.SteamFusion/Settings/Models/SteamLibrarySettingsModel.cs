using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Playnite.SteamFusion
{
    public class SteamLibrarySettingsModel : ObservableObject
    {
        private ObservableCollection<SteamAccountSettingsModel> steamAccountSettings = new ObservableCollection<SteamAccountSettingsModel>();
        private SwitcherToolInstallationType switcherToolInstallationType = SwitcherToolInstallationType.Manual;
        private string? launcherLocation;
        private int startupDelay = 10;
        private int switchTimeout = 30;
        private int launchTimeout = 30;
        private int pollingInterval = 1000;

        public ObservableCollection<SteamAccountSettingsModel> SteamAccountSettings
        {
            get => this.steamAccountSettings;
            set => SetValue(ref this.steamAccountSettings, value);
        }

        public string? LauncherLocation
        {
            get => this.launcherLocation;
            set => SetValue(ref this.launcherLocation, value);
        }

        public SwitcherToolInstallationType SwitcherToolInstallationType
        {
            get => this.switcherToolInstallationType;
            set => SetValue(ref this.switcherToolInstallationType, value);
        }
        
        public int StartupDelay
        {
            get => this.startupDelay;
            set => SetValue(ref this.startupDelay, value);
        }

        public int SwitchTimeout
        {
            get => this.switchTimeout;
            set => SetValue(ref this.switchTimeout, value);
        }
        
        public int LaunchTimeout
        {
            get => this.launchTimeout;
            set => SetValue(ref this.launchTimeout, value);
        }
        
        public int PollingInterval
        {
            get => this.pollingInterval;
            set => SetValue(ref this.pollingInterval, value);
        }
    }
}