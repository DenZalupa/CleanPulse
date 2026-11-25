namespace CleanPulse.Core.Models;

public sealed record FolderSizeEntry(
    string FullPath,
    string Name,
    long SizeBytes,
    int FileCount,
    double PercentOfTotal);

public sealed record LargeFileEntry(
    string FullPath,
    string Name,
    string DirectoryPath,
    string Extension,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc,
    DiskFileCategory FileCategory);

public sealed record DiskAnalysisResult(
    string RootPath,
    long TotalBytes,
    int TotalFiles,
    int SkippedDirectories,
    TimeSpan Duration,
    IReadOnlyList<FolderSizeEntry> TopFolders,
    IReadOnlyList<LargeFileEntry> TopFiles);

public sealed record DuplicateFileEntry(
    string FullPath,
    string Name,
    string DirectoryPath,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc);

public sealed record DuplicateGroup(
    string HashHex,
    long FileSizeBytes,
    IReadOnlyList<DuplicateFileEntry> Files)
{
    public long ReclaimableBytes => Files.Count > 1 ? (Files.Count - 1) * FileSizeBytes : 0;
}

public sealed record DuplicateScanResult(
    string RootPath,
    int ScannedFiles,
    int CandidateGroups,
    long TotalReclaimableBytes,
    TimeSpan Duration,
    IReadOnlyList<DuplicateGroup> Groups);

public sealed record StartupItem(
    string Id,
    string Name,
    string Command,
    string SourceLocation,
    StartupScope Scope,
    bool IsEnabled,
    bool RequiresAdmin);

public sealed record DismCleanupResult(
    bool Success,
    int ExitCode,
    string Output,
    TimeSpan Duration);
