using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SteamFusion.Steam;
using Playnite.SteamFusion.SwitcherTool;

// ReSharper disable AsyncVoidMethod

namespace Playnite.SteamFusion
{
    public class SteamLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly ISwitcherToolService switcherToolService;
        private readonly ISteamLibraryPluginService pluginService;
        private readonly IPlayniteAPI playniteApi;
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
        private string? addEditSteamAccountValidationMessage;
        private bool isTestingAccountCredentials;

        private readonly ILogger logger;

        public SteamLibrarySettingsViewModel(ISteamLibraryPluginService pluginService, IPlayniteAPI playniteApi)
            : this(pluginService, playniteApi, LogManager.GetLogger(), new SwitcherToolService(pluginService.GetPluginUserDataPath())) { }

        public SteamLibrarySettingsViewModel(ISteamLibraryPluginService pluginService, IPlayniteAPI playniteApi, ILogger logger, ISwitcherToolService switcherToolService)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.pluginService = pluginService;
            this.playniteApi = playniteApi;
            this.logger = logger;
            this.switcherToolService = switcherToolService;

            // Load saved settings.
            var savedSettings = this.pluginService.LoadPluginSettings();

            // LoadPluginSettings returns null if no saved data is available.
            this.Settings = savedSettings ?? new SteamLibrarySettingsModel();

            this.AddSteamAccountCommand = new RelayCommand(AddSteamAccount);
            this.EditSteamAccountCommand = new RelayCommand<SteamAccountSettingsModel>(EditSteamAccount);
            this.DeleteSteamAccountCommand = new RelayCommand<SteamAccountSettingsModel>(DeleteSteamAccount);
            this.CancelEditSteamAccountCommand = new RelayCommand(CancelAddEditSteamAccount, CanCancelAddEditSteamAccount);
            this.CompleteEditSteamAccountCommand = new RelayCommand(CompleteAddEditSteamAccount, CanCompleteAddEditSteamAccount);
            this.BrowseForSwitcherToolCommand = new RelayCommand(BrowseForSwitcherTool);
            this.InstallSwitcherToolCommand = new RelayCommand(InstallSwitcherTool, CanInstallSwitcherTool);
            this.Settings.PropertyChanged += OnSettingsChanged;

            _ = UpdateSwitcherToolInfoAsync();
        }

        public RelayCommand AddSteamAccountCommand { get; }

        public RelayCommand<SteamAccountSettingsModel> EditSteamAccountCommand { get; }

        public RelayCommand<SteamAccountSettingsModel> DeleteSteamAccountCommand { get; }

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

        public string? AddEditSteamAccountValidationMessage
        {
            get => this.addEditSteamAccountValidationMessage;
            set => SetValue(ref this.addEditSteamAccountValidationMessage, value);
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
            this.pluginService.SavePluginSettings(this.Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (this.isInstallingSwitcherTool == true)
            {
                errors.Add(this.playniteApi.Resources.GetString("ErrorSwitcherToolInstalling"));
            }

            if (this.IsTestingAccountCredentials == true)
            {
                errors.Add(this.playniteApi.Resources.GetString("ErrorAccountEditing"));
            }

            if (string.IsNullOrWhiteSpace(this.Settings.LauncherLocation) || File.Exists(this.Settings.LauncherLocation) == false)
            {
                errors.Add(this.playniteApi.Resources.GetString("ErrorInvalidLauncherLocation"));
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

        private void DeleteSteamAccount(SteamAccountSettingsModel account)
        {
            this.logger.Debug($"User requested to delete Steam account: {account.Id}");

            var result = this.playniteApi.Dialogs.ShowMessage(
                this.playniteApi.Resources.GetString("DialogDeleteAccountMessage"),
                this.playniteApi.Resources.GetString("DialogDeleteAccountTitle"),
                MessageBoxButton.YesNo
            );

            if (result == MessageBoxResult.Yes)
            {
                var accountToRemove = this.Settings.SteamAccountSettings.FirstOrDefault(a => a.Id == account.Id);
                if (accountToRemove != null)
                {
                    this.Settings.SteamAccountSettings.Remove(accountToRemove);
                    this.logger.Info($"Steam account deleted: {account.Id}");
                }
            }
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

        private bool CanCompleteAddEditSteamAccount()
        {
            return this.IsTestingAccountCredentials == false
                   && this.editingAccount != null
                   && string.IsNullOrEmpty(this.editingAccount.Name) == false
                   && string.IsNullOrEmpty(this.editingAccount.Id) == false
                   && string.IsNullOrEmpty(this.editingAccount.Key) == false;
        }

        private async void CompleteAddEditSteamAccount()
        {
            this.logger.Debug("User requested to save Steam account");

            this.AddEditSteamAccountValidationMessage = null;

            if (this.editingAccount == null)
            {
                this.logger.Error("CompleteEditSteamAccount called with null editingAccount");
                throw new ArgumentException();
            }

            // Additional validation: required fields
            if (string.IsNullOrWhiteSpace(this.editingAccount.Id) || string.IsNullOrWhiteSpace(this.editingAccount.Name) || string.IsNullOrWhiteSpace(this.editingAccount.Key))
            {
                this.AddEditSteamAccountValidationMessage = this.playniteApi.Resources.GetString("ErrorAccountFieldsRequired");
                return;
            }

            // Additional validation: unique Id and Name
            var duplicateId = (this.isNewAccount == true && this.Settings.SteamAccountSettings.Any(x => x.Id == this.editingAccount.Id)
                               || (this.isNewAccount == false && this.Settings.SteamAccountSettings.Count(x => x.Id == this.editingAccount.Id) > 1));

            var duplicateName = (this.isNewAccount == true && this.Settings.SteamAccountSettings.Any(x => string.Equals(x.Name, this.editingAccount.Name, StringComparison.InvariantCultureIgnoreCase))
                                 || (this.isNewAccount == false && this.Settings.SteamAccountSettings.Count(x => string.Equals(x.Name, this.editingAccount.Name, StringComparison.InvariantCultureIgnoreCase)) > 1));

            if (duplicateId)
            {
                this.AddEditSteamAccountValidationMessage = this.playniteApi.Resources.GetString("ErrorAccountIdNotUnique");
                return;
            }

            if (duplicateName)
            {
                this.AddEditSteamAccountValidationMessage = this.playniteApi.Resources.GetString("ErrorAccountNameNotUnique");
                return;
            }

            this.IsTestingAccountCredentials = true;

            try
            {
                var steamService = new SteamApiService(this.editingAccount.Id, this.editingAccount.Key);
                var testResult = await Task.Run(() => steamService.TestConnection());

                if (!testResult.Success)
                {
                    this.logger.Warn($"Failed to validate Steam account: {testResult.ErrorMessage}");
                    this.AddEditSteamAccountValidationMessage = $"Failed to validate Steam account: {testResult.ErrorMessage}";
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

            this.LocalSwitcherToolVersion = this.switcherToolService.GetLocalVersion();
            this.RemoteSwitcherToolVersion = await this.switcherToolService.GetRemoteVersion();

            if (string.IsNullOrEmpty(this.localSwitcherToolVersion) == false)
            {
                this.settings.LauncherLocation = this.switcherToolService.GetExecutablePath();
            }

            var isNewVersionAvailable = this.switcherToolService.IsNewerVersion(this.RemoteSwitcherToolVersion, this.LocalSwitcherToolVersion);
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

            try
            {
                await this.switcherToolService.Install();
                this.logger.Info("Switcher Tool installed successfully");

                this.settings.LauncherLocation = this.switcherToolService.GetExecutablePath();
                await UpdateSwitcherToolInfoAsync();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to install Switcher Tool");
                this.InstallErrorMessage = ex.Message;
                var isNewVersionAvailable = this.switcherToolService.IsNewerVersion(this.RemoteSwitcherToolVersion, this.LocalSwitcherToolVersion);

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