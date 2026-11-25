using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using CleanPulse.Core.Models;
using Microsoft.VisualBasic.FileIO;

namespace CleanPulse.Core.Services;

public sealed class WindowsShellRecycleBin : IShellRecycleBin
{
    private const uint SherbNoConfirmation = 0x00000001;
    private const uint SherbNoProgressUi = 0x00000002;
    private const uint SherbNoSound = 0x00000004;
    private const int HResultEmptyOrUnexpected = unchecked((int)0x8000FFFF);

    [StructLayout(LayoutKind.Sequential)]
    private struct ShQueryRbInfo64
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ShQueryRbInfo32
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHQueryRecycleBinW")]
    private static extern int SHQueryRecycleBin64(string? pszRootPath, ref ShQueryRbInfo64 pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHQueryRecycleBinW")]
    private static extern int SHQueryRecycleBin32(string? pszRootPath, ref ShQueryRbInfo32 pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBinW(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    public RecycleBinInfo Query()
    {
        try
        {
            if (Environment.Is64BitProcess)
            {
                var info = new ShQueryRbInfo64 { cbSize = Marshal.SizeOf<ShQueryRbInfo64>() };
                int hr = SHQueryRecycleBin64(null, ref info);
                return hr == 0 ? new RecycleBinInfo(Math.Max(0, info.i64Size), Math.Max(0, info.i64NumItems)) : new RecycleBinInfo(0, 0);
            }
            else
            {
                var info = new ShQueryRbInfo32 { cbSize = Marshal.SizeOf<ShQueryRbInfo32>() };
                int hr = SHQueryRecycleBin32(null, ref info);
                return hr == 0 ? new RecycleBinInfo(Math.Max(0, info.i64Size), Math.Max(0, info.i64NumItems)) : new RecycleBinInfo(0, 0);
            }
        }
        catch
        {
            return new RecycleBinInfo(0, 0);
        }
    }

    public bool Empty(out string? error)
    {
        error = null;
        var current = Query();
        if (current.SizeBytes == 0 && current.ItemCount == 0)
        {
            return true;
        }

        int hr = SHEmptyRecycleBinW(IntPtr.Zero, null, SherbNoConfirmation | SherbNoProgressUi | SherbNoSound);
        if (hr == 0 || hr == HResultEmptyOrUnexpected)
        {
            return true;
        }

        error = $"Win32 HRESULT: 0x{hr:X8}";
        return false;
    }
}

public sealed class WindowsDnsCacheFlusher : IDnsCacheFlusher
{
    [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
    private static extern bool DnsFlushResolverCache();

    public bool Flush(out string? error)
    {
        error = null;
        try
        {
            if (DnsFlushResolverCache())
            {
                return true;
            }

            error = "DnsFlushResolverCache returned false.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

public sealed class WindowsPrivilegeService : IPrivilegeService
{
    public bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public bool RestartAsAdministrator()
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class WindowsFileDeletionService : IFileDeletionService
{
    public bool TryDeleteFile(FileInfo file, bool moveToRecycleBin)
    {
        try
        {
            if ((file.Attributes & FileAttributes.ReadOnly) != 0)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }

            if (moveToRecycleBin)
            {
                FileSystem.DeleteFile(
                    file.FullName,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin,
                    UICancelOption.ThrowException);
                return true;
            }

            file.Delete();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

public sealed class WindowsProcessDetector : IProcessDetector
{
    public bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        try
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class WindowsDismService : IDismService
{
    public async Task<DismCleanupResult> RunComponentCleanupAsync(
        IProgress<string>? outputLineProgress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var sb = new StringBuilder();

        string dismExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "Dism.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = File.Exists(dismExe) ? dismExe : "dism.exe",
            Arguments = "/Online /Cleanup-Image /StartComponentCleanup",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lock (sb)
                {
                    sb.AppendLine(e.Data);
                }

                outputLineProgress?.Report(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lock (sb)
                {
                    sb.AppendLine(e.Data);
                }

                outputLineProgress?.Report(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                return new DismCleanupResult(false, -1, "Failed to start Dism.exe", sw.Elapsed);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var reg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore process kill race.
                }
            });

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            sw.Stop();
            return new DismCleanupResult(process.ExitCode == 0, process.ExitCode, sb.ToString().Trim(), sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new DismCleanupResult(false, -1, "Cancelled", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DismCleanupResult(false, -1, ex.Message, sw.Elapsed);
        }
    }
}

public sealed class WindowsScheduledTaskService : IScheduledTaskService
{
    private const string TaskName = "CleanPulseAutoClean";

    public ScheduleFrequency GetCurrentFrequency()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Query /TN \"{TaskName}\" /FO LIST /V",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p is null)
            {
                return ScheduleFrequency.Disabled;
            }

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);
            if (p.ExitCode != 0)
            {
                return ScheduleFrequency.Disabled;
            }

            if (output.Contains("DAILY", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Ежедневно", StringComparison.OrdinalIgnoreCase))
            {
                return ScheduleFrequency.Daily;
            }

            if (output.Contains("MONTHLY", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Ежемесячно", StringComparison.OrdinalIgnoreCase))
            {
                return ScheduleFrequency.Monthly;
            }

            return ScheduleFrequency.Weekly;
        }
        catch
        {
            return ScheduleFrequency.Disabled;
        }
    }

    public bool ApplySchedule(ScheduleFrequency frequency, string executablePath, out string? error)
    {
        error = null;
        if (frequency == ScheduleFrequency.Disabled)
        {
            int code = RunSchtasks($"/Delete /TN \"{TaskName}\" /F", out string err);
            // Exit code 1 when task already doesn't exist is fine.
            if (code == 0 || err.Contains("cannot find", StringComparison.OrdinalIgnoreCase) || err.Contains("не удается найти", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            error = err;
            return false;
        }

        string scheduleArg = frequency switch
        {
            ScheduleFrequency.Daily => "/SC DAILY /ST 10:00",
            ScheduleFrequency.Weekly => "/SC WEEKLY /D SUN /ST 10:00",
            ScheduleFrequency.Monthly => "/SC MONTHLY /D 1 /ST 10:00",
            _ => "/SC WEEKLY /D SUN /ST 10:00"
        };

        string tr = $"\\\"{executablePath}\\\" --clean-safe --silent";
        int createCode = RunSchtasks($"/Create /TN \"{TaskName}\" /TR \"{tr}\" {scheduleArg} /F", out string createErr);
        if (createCode == 0)
        {
            return true;
        }

        error = createErr;
        return false;
    }

    private static int RunSchtasks(string args, out string output)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p is null)
            {
                output = "Failed to start schtasks.exe";
                return -1;
            }

            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(5000);
            output = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            output = ex.Message;
            return -1;
        }
    }
}
