namespace CleanPulse.Core.Models;

/// <summary>
/// User-defined folder/pattern rule for regular cleanup.
/// </summary>
public sealed class CustomCleanupRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string SearchPattern { get; set; } = "*";
    public bool Recursive { get; set; } = true;
    public bool RespectMinimumAge { get; set; } = true;
    public bool MoveToRecycleBin { get; set; } = true;
}

/// <summary>
/// User preferences persisted locally in %LOCALAPPDATA%\CleanPulse\settings.json.
/// </summary>
public sealed class AppSettings
{
    public int DesignVersion { get; set; } = 2;
    public string Language { get; set; } = "ru";
    public string Theme { get; set; } = "Light";
    public int MinimumFileAgeHours { get; set; } = 24;
    public bool ConfirmBeforeClean { get; set; } = true;
    public bool AutoAnalyzeOnStartup { get; set; } = false;
    public ScheduleFrequency ScheduledCleanFrequency { get; set; } = ScheduleFrequency.Disabled;
    public Dictionary<string, bool> CategorySelection { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Exclusions { get; set; } = new();
    public List<CustomCleanupRule> CustomRules { get; set; } = new();
    public long TotalLifetimeFreedBytes { get; set; }
}

/// <summary>
/// Represents disk capacity statistics for display in the UI.
/// </summary>
public sealed record DriveSpaceInfo(
    string DriveName,
    string VolumeLabel,
    long TotalBytes,
    long FreeBytes)
{
    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    public double UsedPercentage => TotalBytes > 0 ? Math.Round((double)UsedBytes / TotalBytes * 100.0, 1) : 0;
}
