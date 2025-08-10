using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteMultiAccountSteamLibrary.Extension.Steam;
using PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool;
// ReSharper disable AsyncVoidMethod

namespace PlayniteMultiAccountSteamLibrary.Extension
{
    public class SteamLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly SteamLibraryPlugin plugin;
        private SteamLibrarySettingsModel? editingClone;
        private SteamLibrarySettingsModel settings = null!;
        private SteamAccountSettingsModel? editingAccount;
        private bool isNewAccount = false;
        private bool isInstallingSwitcherTool = false;
        private string? localSwitcherToolVersion = "Unknown";
        private string? remoteSwitcherToolVersion = "Unknown";
        private bool isSwitcherToolInstallButtonEnabled;
        private string switcherToolInstallButtonTextKey = "SwitcherToolInstallButton";
        private string? installErrorMessage;
        private string? accountValidationErrorMessage;
        private bool isTestingAccountCredentials;

        private readonly ILogger logger = LogManager.GetLogger();

        public SteamLibrarySettingsViewModel(SteamLibraryPlugin plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SteamLibrarySettingsModel>();

            // LoadPluginSettings returns null if no saved data is available.
            this.Settings = savedSettings ?? new SteamLibrarySettingsModel();

            this.AddSteamAccountCommand = new RelayCommand(AddSteamAccount);
            this.EditSteamAccountCommand = new RelayCommand<SteamAccountSettingsModel>(EditSteamAccount);
            this.CancelEditSteamAccountCommand = new RelayCommand(CancelAddEditSteamAccount, CanCancelAddEditSteamAccount);
            this.CompleteEditSteamAccountCommand = new RelayCommand(CompleteEditSteamAccount, CanCompleteEditSteamAccount);
            this.BrowseForSwitcherToolCommand = new RelayCommand(BrowseForSwitcherTool);
            this.InstallSwitcherToolCommand = new RelayCommand(InstallSwitcherTool, CanInstallSwitcherTool);
            this.Settings.PropertyChanged += OnSettingsChanged;

            _ = UpdateSwitcherToolInfoAsync();
        }

        public RelayCommand AddSteamAccountCommand { get; }

        public RelayCommand<SteamAccountSettingsModel> EditSteamAccountCommand { get; }

        public RelayCommand CancelEditSteamAccountCommand { get; }

        public RelayCommand CompleteEditSteamAccountCommand { get; }

        public RelayCommand BrowseForSwitcherToolCommand { get; }

        public RelayCommand InstallSwitcherToolCommand { get; }

        public bool IsAccountEditorVisible => this.editingAccount != null;

        public SteamLibrarySettingsModel Settings
        {
            get => this.settings;
            private set => SetValue(ref this.settings, value);
        }

        public bool IsCreatingNewAccount
        {
            get => this.isNewAccount;
            set => SetValue(ref this.isNewAccount, value);
        }

        public SteamAccountSettingsModel? EditingAccount
        {
            get => this.editingAccount;

            private set
            {
                SetValue(ref this.editingAccount, value);
                OnPropertyChanged(nameof(this.IsAccountEditorVisible));
            }
        }

        public string? LocalSwitcherToolVersion
        {
            get => this.localSwitcherToolVersion;
            private set => SetValue(ref this.localSwitcherToolVersion, value);
        }

        public string? RemoteSwitcherToolVersion
        {
            get => this.remoteSwitcherToolVersion;
            private set => SetValue(ref this.remoteSwitcherToolVersion, value);
        }

        public bool IsSwitcherToolInstallButtonEnabled
        {
            get => this.isSwitcherToolInstallButtonEnabled;
            set => SetValue(ref this.isSwitcherToolInstallButtonEnabled, value);
        }

        public string SwitcherToolInstallButtonTextKey
        {
            get => this.switcherToolInstallButtonTextKey;
            private set => SetValue(ref this.switcherToolInstallButtonTextKey, value);
        }

        public string? InstallErrorMessage
        {
            get => this.installErrorMessage;
            set => SetValue(ref this.installErrorMessage, value);
        }

        public string? AccountValidationErrorMessage
        {
            get => this.accountValidationErrorMessage;
            set => SetValue(ref this.accountValidationErrorMessage, value);
        }

        public bool IsTestingAccountCredentials
        {
            get => this.isTestingAccountCredentials;
            set => SetValue(ref this.isTestingAccountCredentials, value);
        }

        public void BeginEdit()
        {
            this.editingClone = Serialization.GetClone(this.Settings);
        }

        public void CancelEdit()
        {
            this.Settings = this.editingClone!;
        }

        public void EndEdit()
        {
            this.plugin.SavePluginSettings(this.Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (this.isInstallingSwitcherTool == true)
            {
                errors.Add("Switcher tool is still installing, please wait.");
            }

            if (this.IsTestingAccountCredentials == true)
            {
                errors.Add("A steam account is still being added or edited, please wait");
            }

            if (string.IsNullOrWhiteSpace(this.Settings.LauncherLocation) || File.Exists(this.Settings.LauncherLocation) == false)
            {
                errors.Add("Invalid launcher location.");
            }

            return errors.Count == 0;
        }

        private void AddSteamAccount()
        {
            this.logger.Info("User requested to add a new Steam account");
            this.IsCreatingNewAccount = true;
            this.EditingAccount = new SteamAccountSettingsModel();
        }

        private void EditSteamAccount(SteamAccountSettingsModel account)
        {
            this.logger.Debug($"User requested to edit Steam account: {account.Id}");
            this.IsCreatingNewAccount = false;
            this.EditingAccount = account.Clone();
        }

        private bool CanCancelAddEditSteamAccount()
        {
            return this.IsTestingAccountCredentials == false;
        }
        
        private void CancelAddEditSteamAccount()
        {
            this.logger.Debug("User cancelled editing Steam account");
            this.EditingAccount = null;
        }

        private bool CanCompleteEditSteamAccount()
        {
            return this.IsTestingAccountCredentials == false;
        }
        
        private async void CompleteEditSteamAccount()
        {
            this.logger.Debug("User requested to save Steam account");
            
            if (this.editingAccount == null)
            {
                this.logger.Error("CompleteEditSteamAccount called with null editingAccount");
                throw new ArgumentException();
            }

            this.AccountValidationErrorMessage = null;
            this.IsTestingAccountCredentials = true;

            try
            {
                var steamService = new SteamApiService(this.editingAccount.Id, this.editingAccount.Key);
                var testResult = await Task.Run(() => steamService.TestConnection());

                if (!testResult.Success)
                {
                    this.logger.Warn($"Failed to validate Steam account: {testResult.ErrorMessage}");
                    this.AccountValidationErrorMessage = $"Failed to validate Steam account: {testResult.ErrorMessage}";
                }
                else
                {
                    if (this.isNewAccount)
                    {
                        this.logger.Info($"Adding new Steam account: {this.editingAccount.Id}");
                        this.settings.SteamAccountSettings.Add(this.editingAccount);
                    }
                    else
                    {
                        this.logger.Info($"Updating existing Steam account: {this.editingAccount.Id}");
                        var existing = this.settings.SteamAccountSettings.First(x => x.Id == this.editingAccount.Id);
                        existing.Merge(this.editingAccount);
                    }

                    this.EditingAccount = null;
                }
            }
            finally
            {
                this.IsTestingAccountCredentials = false;
            }
        }

        private void BrowseForSwitcherTool()
        {
            this.logger.Debug("User requested to browse for Switcher Tool executable");
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Select TcNo-Acc-Switcher Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                this.logger.Info($"User selected Switcher Tool executable: {openFileDialog.FileName}");
                this.settings.LauncherLocation = openFileDialog.FileName;
            }
        }

        private async Task UpdateSwitcherToolInfoAsync()
        {
            this.logger.Debug("Updating Switcher Tool info");
            if (this.Settings.SwitcherToolInstallationType != SwitcherToolInstallationType.Automatic)
            {
                this.LocalSwitcherToolVersion = null;
                this.RemoteSwitcherToolVersion = null;
                this.IsSwitcherToolInstallButtonEnabled = false;
                this.SwitcherToolInstallButtonTextKey = "SwitcherToolInstallButton";
                this.logger.Debug("Switcher Tool installation type is not Automatic; info cleared");
                return;
            }

            var service = new SwitcherToolService(this.plugin.GetPluginUserDataPath());
            this.LocalSwitcherToolVersion = service.GetLocalVersion();
            this.RemoteSwitcherToolVersion = await service.GetRemoteVersion();

            if (string.IsNullOrEmpty(this.localSwitcherToolVersion) == false)
            {
                this.settings.LauncherLocation = service.GetExecutablePath();
            }

            var isNewVersionAvailable = service.IsNewerVersion(this.RemoteSwitcherToolVersion, this.LocalSwitcherToolVersion);
            this.logger.Debug($"Switcher Tool local version: {this.LocalSwitcherToolVersion}, remote version: {this.RemoteSwitcherToolVersion}, new version available: {isNewVersionAvailable}");
            
            UpdateInstallButtonText(isNewVersionAvailable);
            this.IsSwitcherToolInstallButtonEnabled = true;
        }

        private void UpdateInstallButtonText(bool isNewVersionAvailable)
        {
            if (string.IsNullOrWhiteSpace(this.LocalSwitcherToolVersion))
            {
                this.SwitcherToolInstallButtonTextKey = "SwitcherToolInstallButtonInstall";
            }
            else if (isNewVersionAvailable)
            {
                this.SwitcherToolInstallButtonTextKey = "SwitcherToolInstallButtonUpdate";
            }
            else
            {
                this.SwitcherToolInstallButtonTextKey = "SwitcherToolInstallButtonReinstall";
            }
        }

        private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Settings.SwitcherToolInstallationType))
            {
                _ = UpdateSwitcherToolInfoAsync();
            }
        }

        private bool CanInstallSwitcherTool()
        {
            return this.IsSwitcherToolInstallButtonEnabled;
        }

        private async void InstallSwitcherTool()
        {
            this.logger.Debug("User requested to install/update Switcher Tool");
            
            this.isInstallingSwitcherTool = true;
            this.IsSwitcherToolInstallButtonEnabled = false;
            this.SwitcherToolInstallButtonTextKey = "SwitcherToolInstallButtonInstalling";
            this.InstallErrorMessage = null;

            var service = new SwitcherToolService(this.plugin.GetPluginUserDataPath());

            try
            {
                await service.Install();
                this.logger.Info("Switcher Tool installed successfully");
                
                this.settings.LauncherLocation = service.GetExecutablePath();
                await UpdateSwitcherToolInfoAsync();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to install Switcher Tool");
                this.InstallErrorMessage = ex.Message;
                var isNewVersionAvailable = service.IsNewerVersion(this.RemoteSwitcherToolVersion, this.LocalSwitcherToolVersion);

                UpdateInstallButtonText(isNewVersionAvailable);
            }
            finally
            {
                this.IsSwitcherToolInstallButtonEnabled = true;
                this.isInstallingSwitcherTool = false;
            }
        }
    }
}