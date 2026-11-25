namespace CleanPulse.Core.Models;

/// <summary>
/// Represents a single folder, file pattern, or system action inside a cleanup category.
/// </summary>
public sealed record CleanupTarget(
    TargetKind Kind,
    string Path,
    string SearchPattern = "*",
    bool Recursive = true,
    bool DeleteEmptySubdirectories = false,
    bool RespectMinimumAge = false)
{
    public static CleanupTarget Directory(
        string path,
        string searchPattern = "*",
        bool recursive = true,
        bool deleteEmptySubdirectories = true,
        bool respectMinimumAge = false) =>
        new(TargetKind.DirectoryContents, path, searchPattern, recursive, deleteEmptySubdirectories, respectMinimumAge);

    public static CleanupTarget RecycleBin() =>
        new(TargetKind.RecycleBin, "shell:RecycleBinFolder", "*", false, false, false);

    public static CleanupTarget DnsCache() =>
        new(TargetKind.DnsCache, "system:DnsResolverCache", "*", false, false, false);
}
