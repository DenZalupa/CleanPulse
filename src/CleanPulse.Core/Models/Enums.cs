namespace CleanPulse.Core.Models;

/// <summary>
/// Logical grouping for system cleanup categories.
/// </summary>
public enum CleanupGroup
{
    SystemAndTemp,
    WindowsCache,
    LogsAndDumps,
    Browsers,
    DevAndApps,
    CustomRules
}

/// <summary>
/// Specifies how a cleanup target is scanned and cleaned.
/// </summary>
public enum TargetKind
{
    DirectoryContents,
    RecycleBin,
    DnsCache
}

/// <summary>
/// Execution state of a cleanup category.
/// </summary>
public enum CategoryStatus
{
    Idle,
    Scanning,
    Analyzed,
    Cleaning,
    Cleaned,
    AccessDenied,
    NotFound,
    Cancelled,
    Error
}

/// <summary>
/// Frequency for optional Windows Task Scheduler auto-cleanup.
/// </summary>
public enum ScheduleFrequency
{
    Disabled,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Windows autostart location scope.
/// </summary>
public enum StartupScope
{
    CurrentUserRun,
    LocalMachineRun,
    CurrentUserStartupFolder,
    CommonStartupFolder
}

/// <summary>
/// File classification used by Disk Analyzer filters.
/// </summary>
public enum DiskFileCategory
{
    All,
    Archives,
    DiskImages,
    Installers,
    Media,
    TempAndDumps
}
