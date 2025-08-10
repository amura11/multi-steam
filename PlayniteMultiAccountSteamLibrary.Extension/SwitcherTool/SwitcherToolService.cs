using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK.Data;
using PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool.Models;

namespace PlayniteMultiAccountSteamLibrary.Extension.SwitcherTool;

public class SwitcherToolService
{
    private const string PortableInstallationDirectory = "TcNoAccountSwitcher";
    private const string GitHubApiReleasesUrl = "https://api.github.com/repos/TCNOco/TcNo-Acc-Switcher/releases/latest";
    private const string FirstRunExecutable = "_FIRST_RUN.exe";
    private const string ToolExecutable = "TcNo-Acc-Switcher.exe";
    private const string UserAgent = "PlayniteMultiAccountPlugin";
    private readonly string installationRoot;

    public SwitcherToolService(string installationRoot)
    {
        this.installationRoot = installationRoot;
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
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        var json = await httpClient.GetStringAsync(GitHubApiReleasesUrl);

        var release = Serialization.FromJson<ToolRelease>(json);

        return release?.TagName;
    }

    public string? GetLocalVersion()
    {
        var basePath = Path.Combine(this.installationRoot, PortableInstallationDirectory);
        var targetDirectory = Path.GetFullPath(basePath);
        var versionFile = Path.Combine(targetDirectory, "version");

        var version = !File.Exists(versionFile) ? null : File.ReadAllText(versionFile).Trim();

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
        var tempZipPath = Path.GetTempFileName();
        var basePath = Path.Combine(this.installationRoot, PortableInstallationDirectory);
        var targetDirectory = Path.GetFullPath(basePath);
        var backupDirectory = targetDirectory + ".bak";
        var backupCreated = false;
        var isFirstInstall = !Directory.Exists(targetDirectory);
        try
        {
            var release = await GetLatestReleaseAsync();
            var zipUrl = GetPortableZipUrl(release);
            await DownloadReleaseZip(zipUrl, tempZipPath);
            backupCreated = BackupExistingInstallation(targetDirectory, backupDirectory);
            ExtractZip(tempZipPath, targetDirectory);
            WriteVersionFile(targetDirectory, release.TagName ?? "unknown");
            if (isFirstInstall)
            {
                RunFirstRunExe(targetDirectory);
            }
            if (backupCreated)
            {
                DeleteBackup(backupDirectory);
            }
        }
        catch (Exception)
        {
            if (backupCreated)
            {
                RestoreBackup(targetDirectory, backupDirectory);
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
        }
    }
    
    private void RunFirstRunExe(string targetDirectory)
    {
        var exePath = Path.Combine(targetDirectory, FirstRunExecutable);
        
        if (File.Exists(exePath))
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = targetDirectory;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();
        }
    }

    private async Task<ToolRelease> GetLatestReleaseAsync()
    {
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
            throw new Exception("No portable zip asset found in the latest release.");
        }

        return zipAsset.DownloadUrl;
    }

    private async Task DownloadReleaseZip(string zipUrl, string destinationPath)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        
        using var response = await httpClient.GetAsync(zipUrl);
        response.EnsureSuccessStatusCode();
        
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream);
    }

    private bool BackupExistingInstallation(string targetDirectory, string backupDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, true);
            }

            Directory.Move(targetDirectory, backupDirectory);
            return true;
        }

        return false;
    }

    private void ExtractZip(string zipPath, string extractToDirectory)
    {
        ZipFile.ExtractToDirectory(zipPath, extractToDirectory);
    }

    private void WriteVersionFile(string targetDirectory, string version)
    {
        var versionFilePath = Path.Combine(targetDirectory, "version");
        File.WriteAllText(versionFilePath, version);
    }

    private void RestoreBackup(string targetDirectory, string backupDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, true);
        }

        Directory.Move(backupDirectory, targetDirectory);
    }

    private void DeleteBackup(string backupDirectory)
    {
        if (Directory.Exists(backupDirectory))
        {
            Directory.Delete(backupDirectory, true);
        }
    }
}