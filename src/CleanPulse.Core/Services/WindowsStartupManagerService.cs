using CleanPulse.Core.Models;
using Microsoft.Win32;

namespace CleanPulse.Core.Services;

/// <summary>
/// Encodes/decodes Windows Explorer StartupApproved binary values safely without deleting registry entries.
/// </summary>
public static class StartupApprovedCodec
{
    public static bool IsEnabled(byte[]? data)
    {
        if (data is null || data.Length == 0)
        {
            return true;
        }

        // In Windows StartupApproved: even leading byte (0x02, 0x06) = Enabled; odd (0x03, 0x01) = Disabled.
        return (data[0] & 0x01) == 0;
    }

    public static byte[] EncodeState(bool enabled, DateTimeOffset? disabledAtUtc = null)
    {
        byte[] result = new byte[12];
        result[0] = enabled ? (byte)0x02 : (byte)0x03;

        if (!enabled)
        {
            long fileTime = (disabledAtUtc ?? DateTimeOffset.UtcNow).ToFileTime();
            BitConverter.TryWriteBytes(result.AsSpan(4, 8), fileTime);
        }

        return result;
    }
}

/// <summary>
/// Reads and toggles Windows startup applications via Registry Run keys, Startup folders, and StartupApproved flags.
/// </summary>
public sealed class WindowsStartupManagerService : IStartupManagerService
{
    private const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedRunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ApprovedFolderSubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    public IReadOnlyList<StartupItem> GetStartupItems()
    {
        var items = new List<StartupItem>();

        ReadRegistryRunItems(Registry.CurrentUser, StartupScope.CurrentUserRun, requiresAdmin: false, "HKCU", items);
        ReadRegistryRunItems(Registry.LocalMachine, StartupScope.LocalMachineRun, requiresAdmin: true, "HKLM", items);

        ReadStartupFolderItems(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Registry.CurrentUser,
            StartupScope.CurrentUserStartupFolder,
            requiresAdmin: false,
            "User Startup",
            items);

        ReadStartupFolderItems(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            Registry.LocalMachine,
            StartupScope.CommonStartupFolder,
            requiresAdmin: true,
            "All Users Startup",
            items);

        return items
            .OrderByDescending(i => i.IsEnabled)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool SetEnabled(StartupItem item, bool enabled, out string? error)
    {
        error = null;
        try
        {
            var rootHive = item.Scope is StartupScope.CurrentUserRun or StartupScope.CurrentUserStartupFolder
                ? Registry.CurrentUser
                : Registry.LocalMachine;

            string approvedSubKey = item.Scope is StartupScope.CurrentUserRun or StartupScope.LocalMachineRun
                ? ApprovedRunSubKey
                : ApprovedFolderSubKey;

            using var key = rootHive.CreateSubKey(approvedSubKey, writable: true);
            if (key is null)
            {
                error = "Unable to open StartupApproved registry key.";
                return false;
            }

            key.SetValue(item.Id, StartupApprovedCodec.EncodeState(enabled), RegistryValueKind.Binary);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void ReadRegistryRunItems(
        RegistryKey hive,
        StartupScope scope,
        bool requiresAdmin,
        string hiveLabel,
        List<StartupItem> items)
    {
        try
        {
            using var runKey = hive.OpenSubKey(RunSubKey, writable: false);
            if (runKey is null)
            {
                return;
            }

            using var approvedKey = hive.OpenSubKey(ApprovedRunSubKey, writable: false);

            foreach (string valueName in runKey.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                string command = runKey.GetValue(valueName)?.ToString() ?? string.Empty;
                byte[]? approvedBytes = approvedKey?.GetValue(valueName) as byte[];
                bool enabled = StartupApprovedCodec.IsEnabled(approvedBytes);

                items.Add(new StartupItem(
                    Id: valueName,
                    Name: valueName,
                    Command: command,
                    SourceLocation: $@"{hiveLabel}\{RunSubKey}",
                    Scope: scope,
                    IsEnabled: enabled,
                    RequiresAdmin: requiresAdmin));
            }
        }
        catch
        {
            // Ignore unreadable hive/key.
        }
    }

    private static void ReadStartupFolderItems(
        string folderPath,
        RegistryKey hive,
        StartupScope scope,
        bool requiresAdmin,
        string label,
        List<StartupItem> items)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        try
        {
            using var approvedKey = hive.OpenSubKey(ApprovedFolderSubKey, writable: false);

            foreach (string file in Directory.EnumerateFiles(folderPath))
            {
                string fileName = Path.GetFileName(file);
                if (string.Equals(fileName, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                byte[]? approvedBytes = approvedKey?.GetValue(fileName) as byte[];
                bool enabled = StartupApprovedCodec.IsEnabled(approvedBytes);

                items.Add(new StartupItem(
                    Id: fileName,
                    Name: Path.GetFileNameWithoutExtension(fileName),
                    Command: file,
                    SourceLocation: $"{label} ({folderPath})",
                    Scope: scope,
                    IsEnabled: enabled,
                    RequiresAdmin: requiresAdmin));
            }
        }
        catch
        {
            // Ignore inaccessible startup folders.
        }
    }
}
