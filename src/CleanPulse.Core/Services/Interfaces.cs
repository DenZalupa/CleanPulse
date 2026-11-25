using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

public sealed record RecycleBinInfo(long SizeBytes, long ItemCount);

public interface IShellRecycleBin
{
    RecycleBinInfo Query();
    bool Empty(out string? error);
}

public interface IDnsCacheFlusher
{
    bool Flush(out string? error);
}

public interface IPrivilegeService
{
    bool IsAdministrator();
    bool RestartAsAdministrator();
}

public interface IFileDeletionService
{
    bool TryDeleteFile(FileInfo file, bool moveToRecycleBin);
}

public interface IProcessDetector
{
    bool IsProcessRunning(string processName);
}

public interface IDismService
{
    Task<DismCleanupResult> RunComponentCleanupAsync(
        IProgress<string>? outputLineProgress = null,
        CancellationToken cancellationToken = default);
}

public interface IScheduledTaskService
{
    ScheduleFrequency GetCurrentFrequency();
    bool ApplySchedule(ScheduleFrequency frequency, string executablePath, out string? error);
}

public interface IStartupManagerService
{
    IReadOnlyList<StartupItem> GetStartupItems();
    bool SetEnabled(StartupItem item, bool enabled, out string? error);
}
