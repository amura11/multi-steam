using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool.Models;

namespace PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool;

public class SwitcherToolService : ISwitcherToolService
{
    private const string PortableInstallationDirectory = "TcNoAccountSwitcher";
    private const string GitHubApiReleasesUrl = "https://api.github.com/repos/TCNOco/TcNo-Acc-Switcher/releases/latest";
    private const string FirstRunExecutable = "_FIRST_RUN.exe";
    private const string ToolExecutable = "TcNo-Acc-Switcher.exe";
    private const string UserAgent = "PlayniteMultiAccountPlugin";
    private readonly string installationRoot;
    private readonly ILogger logger;

    public SwitcherToolService(string installationRoot)
        : this(installationRoot, LogManager.GetLogger()) { }

    internal SwitcherToolService(string installationRoot, ILogger logger)
    {
        this.installationRoot = installationRoot;
        this.logger = logger;
    }

    public string GetExecutablePath()
    {
        var basePath = Path.Combine(this.installationRoot, PortableInstallationDirectory);
        var targetDirectory = Path.GetFullPath(basePath);
        var executablePath = Path.Combine(targetDirectory, ToolExecutable);

        return executablePath;
    }

    public async Task<string?> GetRemoteVersion()
    {
        this.logger.Info("Fetching remote version from GitHub API");
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        var json = await httpClient.GetStringAsync(GitHubApiReleasesUrl);

        var release = Serialization.FromJson<ToolRelease>(json);

        this.logger.Debug($"Remote version: {release?.TagName}");
        return release?.TagName;
    }

    public string? GetLocalVersion()
    {
        this.logger.Info($"Getting local version from {this.installationRoot}");
        var basePath = Path.Combine(this.installationRoot, PortableInstallationDirectory);
        var targetDirectory = Path.GetFullPath(basePath);
        var versionFile = Path.Combine(targetDirectory, "version");

        var version = !File.Exists(versionFile) ? null : File.ReadAllText(versionFile).Trim();
        this.logger.Debug($"Local version: {version}");

        return version;
    }

    public bool IsNewerVersion(string? version1, string? version2)
    {
        if (string.IsNullOrWhiteSpace(version1) && string.IsNullOrWhiteSpace(version2))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(version1))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(version2))
        {
            return true;
        }

        // Compare as strings, since format is sortable (yyyy-MM-dd_##)
        var comparison = string.Compare(version1, version2, StringComparison.Ordinal);

        return comparison > 0;
    }

    public async Task Install()
    {
        this.logger.Info("Starting switcher tool installation");
        
        var tempZipPath = Path.GetTempFileName();
        var basePath = Path.Combine(this.installationRoot, PortableInstallationDirectory);
        var targetDirectory = Path.GetFullPath(basePath);
        var backupDirectory = targetDirectory + ".bak";
        var backupCreated = false;
        var isFirstInstall = !Directory.Exists(targetDirectory);

        try
        {
            this.logger.Info("Fetching latest release information");
            var release = await GetLatestReleaseAsync();
            var zipUrl = GetPortableZipUrl(release);

            this.logger.Info($"Downloading release from {zipUrl}");
            await DownloadReleaseZip(zipUrl, tempZipPath);

            this.logger.Info("Creating backup of existing installation");
            backupCreated = BackupExistingInstallation(targetDirectory, backupDirectory);

            this.logger.Info($"Extracting release to {targetDirectory}");
            ExtractZip(tempZipPath, targetDirectory);

            this.logger.Info($"Writing version file with version: {release.TagName ?? "unknown"}");
            WriteVersionFile(targetDirectory, release.TagName ?? "unknown");

            if (isFirstInstall)
            {
                this.logger.Info("Running first-time setup executable");
                RunFirstRunExe(targetDirectory);
            }

            if (backupCreated)
            {
                this.logger.Info("Removing backup after successful installation");
                DeleteBackup(backupDirectory);
            }

            this.logger.Info("Installation completed successfully");
        }
        catch (Exception ex)
        {
            this.logger.Error(ex, "Error during installation");
            if (backupCreated)
            {
                this.logger.Info("Restoring backup due to installation failure");
                RestoreBackup(targetDirectory, backupDirectory);
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempZipPath))
            {
                this.logger.Debug($"Cleaning up temporary file: {tempZipPath}");
                File.Delete(tempZipPath);
            }
        }
    }

    private void RunFirstRunExe(string targetDirectory)
    {
        var exePath = Path.Combine(targetDirectory, FirstRunExecutable);

        if (File.Exists(exePath))
        {
            this.logger.Info($"Running first-time setup: {exePath}");
            
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = targetDirectory;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();
            
            this.logger.Info($"First-time setup completed with exit code: {process.ExitCode}");
        }
        else
        {
            this.logger.Warn($"First-time setup executable not found at: {exePath}");
        }
    }

    private async Task<ToolRelease> GetLatestReleaseAsync()
    {
        this.logger.Debug("Fetching latest release information from GitHub");
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        var json = await httpClient.GetStringAsync(GitHubApiReleasesUrl);
        var release = Serialization.FromJson<ToolRelease>(json);

        return release;
    }

    private string GetPortableZipUrl(ToolRelease release)
    {
        var zipAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

        if (zipAsset == null || string.IsNullOrEmpty(zipAsset.DownloadUrl))
        {
            this.logger.Error("No portable zip asset found in the latest release");
            throw new Exception("No portable zip asset found in the latest release.");
        }

        return zipAsset.DownloadUrl;
    }

    private async Task DownloadReleaseZip(string zipUrl, string destinationPath)
    {
        this.logger.Debug($"Downloading zip from {zipUrl} to {destinationPath}");
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        using var response = await httpClient.GetAsync(zipUrl);
        response.EnsureSuccessStatusCode();

        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream);
        this.logger.Debug("Download completed successfully");
    }

    private bool BackupExistingInstallation(string targetDirectory, string backupDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            this.logger.Info($"Creating backup at: {backupDirectory}");
            if (Directory.Exists(backupDirectory))
            {
                this.logger.Debug("Removing existing backup directory");
                Directory.Delete(backupDirectory, true);
            }

            Directory.Move(targetDirectory, backupDirectory);
            return true;
        }

        return false;
    }

    private void ExtractZip(string zipPath, string extractToDirectory)
    {
        this.logger.Debug($"Extracting {zipPath} to {extractToDirectory}");
        ZipFile.ExtractToDirectory(zipPath, extractToDirectory);
    }

    private void WriteVersionFile(string targetDirectory, string version)
    {
        var versionFilePath = Path.Combine(targetDirectory, "version");
        this.logger.Debug($"Writing version file at {versionFilePath} with version: {version}");
        File.WriteAllText(versionFilePath, version);
    }

    private void RestoreBackup(string targetDirectory, string backupDirectory)
    {
        this.logger.Info($"Restoring backup from {backupDirectory} to {targetDirectory}");
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, true);
        }

        Directory.Move(backupDirectory, targetDirectory);
        this.logger.Info("Backup restored successfully");
    }

    private void DeleteBackup(string backupDirectory)
    {
        if (Directory.Exists(backupDirectory))
        {
            this.logger.Debug($"Removing backup directory: {backupDirectory}");
            Directory.Delete(backupDirectory, true);
        }
    }
}