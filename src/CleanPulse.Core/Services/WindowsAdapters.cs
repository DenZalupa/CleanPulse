using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

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
