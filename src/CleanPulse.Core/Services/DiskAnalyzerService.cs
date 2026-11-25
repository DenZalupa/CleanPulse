using System.Diagnostics;
using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Single-pass iterative disk analyzer finding heaviest subfolders and largest files safely.
/// </summary>
public sealed class DiskAnalyzerService
{
    private static readonly HashSet<string> ArchiveExts = new(StringComparer.OrdinalIgnoreCase)
        { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".cab" };

    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".iso", ".img", ".vhd", ".vhdx", ".vmdk", ".wim", ".esd" };

    private static readonly HashSet<string> InstallerExts = new(StringComparer.OrdinalIgnoreCase)
        { ".exe", ".msi", ".msix", ".appx", ".msp" };

    private static readonly HashSet<string> MediaExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".flv", ".wav", ".flac", ".mp3" };

    private static readonly HashSet<string> TempDumpExts = new(StringComparer.OrdinalIgnoreCase)
        { ".tmp", ".temp", ".dmp", ".log", ".bak", ".old", ".etl" };

    private readonly IFileDeletionService _fileDeleter;

    public DiskAnalyzerService(IFileDeletionService? fileDeleter = null)
    {
        _fileDeleter = fileDeleter ?? new WindowsFileDeletionService();
    }

    public Task<DiskAnalysisResult> AnalyzeAsync(
        string rootPath,
        int maxTopFiles = 150,
        int maxTopFolders = 30,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ExecuteAnalyze(rootPath, maxTopFiles, maxTopFolders, progress, cancellationToken), cancellationToken);

    public bool TryDeleteFile(string fullPath, bool moveToRecycleBin)
    {
        if (SafetyGuard.IsProtectedOsPath(fullPath) || !File.Exists(fullPath))
        {
            return false;
        }

        var info = new FileInfo(fullPath);
        if (SafetyGuard.IsReparsePoint(info))
        {
            return false;
        }

        return _fileDeleter.TryDeleteFile(info, moveToRecycleBin);
    }

    public static DiskFileCategory ClassifyExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            return DiskFileCategory.All;
        }

        if (ArchiveExts.Contains(ext)) return DiskFileCategory.Archives;
        if (ImageExts.Contains(ext)) return DiskFileCategory.DiskImages;
        if (InstallerExts.Contains(ext)) return DiskFileCategory.Installers;
        if (MediaExts.Contains(ext)) return DiskFileCategory.Media;
        if (TempDumpExts.Contains(ext)) return DiskFileCategory.TempAndDumps;
        return DiskFileCategory.All;
    }

    private static DiskAnalysisResult ExecuteAnalyze(
        string rootPath,
        int maxTopFiles,
        int maxTopFolders,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        if (!SafetyGuard.IsDirectorySafeForAnalysis(rootPath))
        {
            return new DiskAnalysisResult(rootPath, 0, 0, 0, sw.Elapsed, Array.Empty<FolderSizeEntry>(), Array.Empty<LargeFileEntry>());
        }

        string normalizedRoot = SafetyGuard.TryNormalizePath(rootPath) ?? rootPath;
        var rootDir = new DirectoryInfo(normalizedRoot);

        // Bucket -1 represents files sitting directly in rootPath.
        var folderStats = new Dictionary<int, (string FullPath, string Name, long Bytes, int Files)>();
        var folderNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int nextBucketId = 0;

        var topFilesHeap = new PriorityQueue<LargeFileEntry, long>();
        long totalBytes = 0;
        int totalFiles = 0;
        int skippedDirs = 0;
        int progressTick = 0;

        var stack = new Stack<(DirectoryInfo Dir, int BucketId)>();
        stack.Push((rootDir, -1));

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (currentDir, bucketId) = stack.Pop();

            if (++progressTick % 25 == 1)
            {
                progress?.Report(currentDir.FullName);
            }

            try
            {
                foreach (var file in currentDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (SafetyGuard.IsReparsePoint(file))
                    {
                        continue;
                    }

                    long length;
                    DateTimeOffset modified;
                    try
                    {
                        length = file.Length;
                        modified = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);
                    }
                    catch
                    {
                        continue;
                    }

                    totalBytes += length;
                    totalFiles++;

                    if (bucketId >= 0 && folderStats.TryGetValue(bucketId, out var stat))
                    {
                        folderStats[bucketId] = (stat.FullPath, stat.Name, stat.Bytes + length, stat.Files + 1);
                    }

                    var entry = new LargeFileEntry(
                        FullPath: file.FullName,
                        Name: file.Name,
                        DirectoryPath: file.DirectoryName ?? string.Empty,
                        Extension: file.Extension,
                        SizeBytes: length,
                        LastModifiedUtc: modified,
                        FileCategory: ClassifyExtension(file.Extension));

                    if (topFilesHeap.Count < maxTopFiles)
                    {
                        topFilesHeap.Enqueue(entry, length);
                    }
                    else if (topFilesHeap.TryPeek(out _, out long smallest) && length > smallest)
                    {
                        topFilesHeap.DequeueEnqueue(entry, length);
                    }
                }
            }
            catch
            {
                skippedDirs++;
            }

            try
            {
                foreach (var subDir in currentDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (SafetyGuard.IsReparsePoint(subDir))
                    {
                        continue;
                    }

                    int childBucket = bucketId;
                    if (bucketId == -1)
                    {
                        if (!folderNames.TryGetValue(subDir.FullName, out childBucket))
                        {
                            childBucket = nextBucketId++;
                            folderNames[subDir.FullName] = childBucket;
                            folderStats[childBucket] = (subDir.FullName, subDir.Name, 0, 0);
                        }
                    }

                    stack.Push((subDir, childBucket));
                }
            }
            catch
            {
                skippedDirs++;
            }
        }

        var topFolders = folderStats.Values
            .Where(s => s.Bytes > 0)
            .OrderByDescending(s => s.Bytes)
            .Take(maxTopFolders)
            .Select(s => new FolderSizeEntry(
                s.FullPath,
                s.Name,
                s.Bytes,
                s.Files,
                totalBytes > 0 ? Math.Round((double)s.Bytes / totalBytes * 100.0, 1) : 0))
            .ToList();

        var topFiles = new List<LargeFileEntry>(topFilesHeap.Count);
        while (topFilesHeap.Count > 0)
        {
            topFiles.Add(topFilesHeap.Dequeue());
        }

        topFiles.Reverse();
        sw.Stop();

        return new DiskAnalysisResult(
            normalizedRoot,
            totalBytes,
            totalFiles,
            skippedDirs,
            sw.Elapsed,
            topFolders,
            topFiles);
    }
}
