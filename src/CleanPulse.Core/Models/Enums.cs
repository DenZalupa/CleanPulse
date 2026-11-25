namespace CleanPulse.Core.Models;

/// <summary>
/// Logical grouping for system cleanup categories.
/// </summary>
public enum CleanupGroup
{
    SystemAndTemp,
    WindowsCache,
    LogsAndDumps
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
