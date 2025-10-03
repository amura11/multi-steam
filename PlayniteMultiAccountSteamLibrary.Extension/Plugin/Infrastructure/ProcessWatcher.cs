using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;

namespace PlayniteMultiAccountSteamLibrary.Extension.Plugin;

public class ProcessWatcher : IProcessWatcher
{
    private readonly string targetDirectory;
    private readonly int pollingInterval;
    private readonly int startTimeout;
    private readonly TimeSpan stabilizationWindow;
    private readonly ILogger logger;

    public ProcessWatcher(string targetDirectory, int pollingInterval, int stabilizationInterval, int startTimeout)
        : this(LogManager.GetLogger(), targetDirectory, pollingInterval, stabilizationInterval, startTimeout) { }

    internal ProcessWatcher(ILogger logger, string targetDirectory, int pollingInterval, int stabilizationInterval, int startTimeout)
    {
        this.logger = logger;
        this.targetDirectory = NormalizedPath(targetDirectory);
        this.pollingInterval = pollingInterval;
        this.startTimeout = startTimeout;
        this.stabilizationWindow = TimeSpan.FromMilliseconds(stabilizationInterval);
    }

    public async Task<int?> WaitForStartAsync(CancellationToken cancellationToken)
    {
        Process? startedProcess = null;
        var elapsed = 0;
        
        this.logger.Info($"Starting to watch for process in directory: {this.targetDirectory}");

        while (startedProcess == null && elapsed < this.startTimeout * 1000)
        {
            cancellationToken.ThrowIfCancellationRequested();

            startedProcess = FindRunningProcess();

            if (startedProcess != null)
            {
                this.logger.Info($"Found target process. Process ID: {startedProcess.Id}");
            }
            else
            {
                this.logger.Debug($"No matching process found. Time elapsed: {elapsed}ms / {this.startTimeout * 1000}ms");

                await Task.Delay(this.pollingInterval, cancellationToken);
                elapsed += this.pollingInterval;
            }
        }

        if (startedProcess == null)
        {
            this.logger.Warn($"Process watch timed out after {this.startTimeout} seconds");
        }

        return startedProcess?.Id;
    }

    public async Task WaitForEndAsync(CancellationToken cancellationToken)
    {
        DateTime? zeroObservedSinceUtc = null;
        var isRunning = true;
        
        this.logger.Info($"Starting to watch for process termination in directory: {this.targetDirectory}");

        while (isRunning == true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runningProcess = FindRunningProcess();

            if (runningProcess == null)
            {
                zeroObservedSinceUtc ??= DateTime.UtcNow;
                var timeWithNoProcess = DateTime.UtcNow - zeroObservedSinceUtc.Value;

                this.logger.Trace($"No process found for {timeWithNoProcess.TotalMilliseconds}ms. Stabilization window: {this.stabilizationWindow.TotalMilliseconds}ms");

                if (timeWithNoProcess >= this.stabilizationWindow)
                {
                    this.logger.Info("Process has remained terminated through stabilization window");
                    isRunning = false;
                }
            }
            else
            {
                if (zeroObservedSinceUtc.HasValue)
                {
                    this.logger.Debug($"Process found again after being missing. Resetting stabilization window");
                    zeroObservedSinceUtc = null;
                }

                await Task.Delay(this.pollingInterval, cancellationToken);
            }
        }
    }

    private Process? FindRunningProcess()
    {
        Process? foundProcess = null;

        var processes = Process.GetProcesses();
        this.logger.Debug($"Scanning {processes.Length} running processes");

        foreach (var process in processes)
        {
            var executablePath = GetMainModuleFileName(process);

            if (string.IsNullOrEmpty(executablePath))
            {
                this.logger.Trace($"Process {process.Id}: Could not get executable path");
                continue;
            }

            var executableDirectory = Path.GetDirectoryName(executablePath);

            if (string.IsNullOrEmpty(executableDirectory))
            {
                this.logger.Trace($"Process {process.Id}: Could not get directory from path '{executablePath}'");
                continue;
            }

            var normalizedExecutableDirectory = NormalizedPath(executableDirectory);

            if (normalizedExecutableDirectory.StartsWith(this.targetDirectory, StringComparison.InvariantCultureIgnoreCase))
            {
                this.logger.Debug($"Found matching process. ID: {process.Id}, Path: {executablePath}");
                foundProcess = process;
                break;
            }
            else
            {
                this.logger.Trace($"Process {process.Id}: Directory did not match target, Path: {executablePath} Normalized directory '{normalizedExecutableDirectory}");
            }
        }

        if (foundProcess == null)
        {
            this.logger.Debug($"No process found running from {this.targetDirectory}");
        }

        return foundProcess;
    }

    private string NormalizedPath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
    }
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr processHandle, int flags, StringBuilder executablePath, ref uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [Flags]
    private enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    private const int BufferSize = 1024;

    private static string? GetMainModuleFileName(Process process)
    {
        string? fileName;

        var handle = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, process.Id);

        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var fileNameBuilder = new StringBuilder(BufferSize);
            var bufferLength = (uint)BufferSize + 1;

            var result = QueryFullProcessImageName(handle, 0, fileNameBuilder, ref bufferLength);

            fileName = result ? fileNameBuilder.ToString() : null;
        }
        finally
        {
            CloseHandle(handle);
        }

        return fileName;
    }
}