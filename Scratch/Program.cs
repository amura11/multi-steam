using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Scratch
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var test = "D:\\SteamLibrary\\steamapps\\common\\Balatro";

            var found = false;

            var processes = Process.GetProcesses();

            Console.WriteLine(processes.Length);
            
            foreach (var process in processes)
            {
                string executablePath;
                if (!TryGetMainModuleFileName(process, out executablePath))
                {
                    Console.WriteLine($"Failed to get path for ${process.ProcessName}");
                    continue;
                }

                var executableDirectory = Path.GetDirectoryName(executablePath);

                if (string.IsNullOrEmpty(executableDirectory))
                {
                    continue;
                }

                Console.WriteLine(executablePath);

                if (string.Equals(Path.GetFullPath(executableDirectory).TrimEnd(Path.DirectorySeparatorChar), test, StringComparison.OrdinalIgnoreCase))
                {
                    //TODO: Debug log

                    found = true;
                    break;
                }
            }
        }

        // Use the limited query right; this avoids VM_READ and works on far more processes.
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr processHandle, int flags, StringBuilder executablePath, ref uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [Flags]
        public enum ProcessAccessFlags : uint
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

        public static bool TryGetMainModuleFileName(Process process, out string fileName, int buffer = 1024)
        {
            fileName = null;
            var handle = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, process.Id);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var fileNameBuilder = new StringBuilder(buffer);
                uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
                var result = QueryFullProcessImageName(handle, 0, fileNameBuilder, ref bufferLength);
                fileName = result ? fileNameBuilder.ToString() : null;
                return result;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        // private static bool TryGetProcessExecutablePath(int processId, out string executablePath)
        // {
        //     executablePath = string.Empty;
        //
        //     IntPtr processHandle = IntPtr.Zero;
        //     try
        //     {
        //         processHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        //         if (processHandle == IntPtr.Zero)
        //             return false;
        //
        //         // Start with a reasonable buffer; grow if needed.
        //         int bufferSize = 260;
        //         StringBuilder buffer = new StringBuilder(bufferSize);
        //
        //         while (true)
        //         {
        //             int size = buffer.Capacity;
        //             if (QueryFullProcessImageName(processHandle, 0, buffer, ref size))
        //             {
        //                 executablePath = buffer.ToString(0, size);
        //                 return !string.IsNullOrEmpty(executablePath);
        //             }
        //
        //             int error = Marshal.GetLastWin32Error();
        //             const int ERROR_INSUFFICIENT_BUFFER = 122;
        //
        //             if (error == ERROR_INSUFFICIENT_BUFFER)
        //             {
        //                 bufferSize *= 2;
        //                 buffer = new StringBuilder(bufferSize);
        //                 continue;
        //             }
        //
        //             // Access denied or process gone.
        //             return false;
        //         }
        //     }
        //     catch
        //     {
        //         return false;
        //     }
        //     finally
        //     {
        //         if (processHandle != IntPtr.Zero)
        //             CloseHandle(processHandle);
        //     }
        // }
    }
}