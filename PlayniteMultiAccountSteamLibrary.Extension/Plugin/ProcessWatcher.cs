using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteMultiAccountSteamLibrary.Extension.Plugin;

public class ProcessWatcher
{
    private readonly string targetDirectory;
    private readonly int pollingInterval;
    private readonly int startTimeout;
    private readonly TimeSpan stabilizationWindow;

    public ProcessWatcher(string targetDirectory, int pollingInterval, int stabilizationInterval, int startTimeout)
    {
        this.targetDirectory = Path.GetFullPath(targetDirectory).TrimEnd(Path.DirectorySeparatorChar);
        this.pollingInterval = pollingInterval;
        this.startTimeout = startTimeout;
        this.stabilizationWindow = TimeSpan.FromMilliseconds(stabilizationInterval);
    }

    public async Task<int?> WaitForStartAsync(CancellationToken cancellationToken)
    {
        Process? startedProcess = null;
        var elapsed = 0;

        while (startedProcess == null && elapsed < this.startTimeout * 1000)
        {
            cancellationToken.ThrowIfCancellationRequested();

            startedProcess = FindRunningProcess();
            
            if (startedProcess != null)
            {
                //TODO: Info log
            }
            else
            {
                //TODO: Debug log

                await Task.Delay(this.pollingInterval, cancellationToken);
                elapsed += this.pollingInterval;
            }
        }

        return startedProcess?.Id;
    }

    public async Task WaitForEndAsync(CancellationToken cancellationToken)
    {
        DateTime? zeroObservedSinceUtc = null;
        var isRunning = true;

        while (isRunning == true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runningProcess = FindRunningProcess();

            if (runningProcess == null)
            {
                zeroObservedSinceUtc ??= DateTime.UtcNow;

                if (DateTime.UtcNow - zeroObservedSinceUtc.Value >= this.stabilizationWindow)
                {
                    isRunning = false;
                }
            }
            else
            {
                await Task.Delay(this.pollingInterval, cancellationToken);
            }
        }
    }

    private Process? FindRunningProcess()
    {
        Process? foundProcess = null;

        var processes = Process.GetProcesses();

        foreach (var process in processes)
        {
            var executablePath = GetMainModuleFileName(process);

            if (string.IsNullOrEmpty(executablePath))
            {
                continue;
            }

            var executableDirectory = Path.GetDirectoryName(executablePath);

            if (string.IsNullOrEmpty(executableDirectory))
            {
                continue;
            }

            var normalizedExecutableDirectory = Path.GetFullPath(executableDirectory).TrimEnd(Path.DirectorySeparatorChar);

            if (string.Equals(normalizedExecutableDirectory, this.targetDirectory, StringComparison.OrdinalIgnoreCase))
            {
                //TODO: Debug log

                foundProcess = process;
                break;
            }
        }

        return foundProcess;
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