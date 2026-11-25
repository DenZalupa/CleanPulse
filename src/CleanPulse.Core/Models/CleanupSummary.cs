namespace CleanPulse.Core.Models;

/// <summary>
/// Aggregate summary for an entire analysis or cleanup batch.
/// </summary>
public sealed record CleanupSummary(
    bool WasDryRun,
    DateTimeOffset CompletedAtUtc,
    TimeSpan Duration,
    long TotalBytes,
    int TotalFiles,
    long CleanedBytes,
    int CleanedFiles,
    int SkippedCount,
    int AccessDeniedCategories,
    bool WasCancelled,
    IReadOnlyList<CategoryScanResult> CategoryResults);

/// <summary>
/// Persisted log record of a completed cleanup or analysis run.
/// </summary>
public sealed class CleanupHistoryEntry
{
    public DateTimeOffset TimestampUtc { get; set; }
    public string ActionType { get; set; } = "Clean";
    public long BytesAffected { get; set; }
    public int FilesAffected { get; set; }
    public int SkippedCount { get; set; }
    public double DurationSeconds { get; set; }
    public int CategoriesCount { get; set; }
    public string SummaryText { get; set; } = string.Empty;
}
