namespace CleanPulse.Core.Models;

/// <summary>
/// Holds scan or cleanup metrics and discovered items for one category.
/// </summary>
public sealed record CategoryScanResult(
    string CategoryId,
    CategoryStatus Status,
    long TotalBytes,
    int FileCount,
    long CleanedBytes,
    int CleanedCount,
    int SkippedCount,
    IReadOnlyList<ScannedItem> Items,
    string? StatusDetail = null)
{
    public static CategoryScanResult Empty(string categoryId, CategoryStatus status = CategoryStatus.Idle, string? detail = null) =>
        new(categoryId, status, 0, 0, 0, 0, 0, Array.Empty<ScannedItem>(), detail);
}

/// <summary>
/// Progress payload emitted during analysis and cleaning runs.
/// </summary>
public sealed record ScanProgress(
    string CategoryId,
    string CurrentPath,
    int CompletedCategories,
    int TotalCategories,
    long AccumulatedBytes,
    int AccumulatedFiles);
