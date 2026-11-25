namespace CleanPulse.Core.Models;

/// <summary>
/// Represents a single discovered file, directory, or virtual cleanup item.
/// </summary>
public sealed record ScannedItem(
    string CategoryId,
    string FullPath,
    string DisplayName,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc,
    bool IsDirectory = false,
    bool IsSpecialAction = false);
