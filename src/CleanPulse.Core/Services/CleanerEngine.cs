using System.Diagnostics;
using System.Security;
using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Configuration passed into each scan or cleanup run.
/// </summary>
public sealed record CleanerOptions(
    TimeSpan MinimumFileAge,
    IReadOnlyList<string> Exclusions,
    int MaxPreviewItemsPerCategory = 5000,
    DateTimeOffset? UtcNowOverride = null);

/// <summary>
/// Performs safe, cancellable, dry-run or destructive cleanup across configured categories.
/// </summary>
public sealed class CleanerEngine
{
    private readonly IShellRecycleBin _recycleBin;
    private readonly IDnsCacheFlusher _dnsFlusher;

    public CleanerEngine(IShellRecycleBin recycleBin, IDnsCacheFlusher dnsFlusher)
    {
        _recycleBin = recycleBin;
        _dnsFlusher = dnsFlusher;
    }

    public Task<CategoryScanResult> AnalyzeCategoryAsync(
        CleanupCategory category,
        CleanerOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ExecuteCategory(category, dryRun: true, options, 0, 1, progress, cancellationToken), cancellationToken);

    public async Task<CleanupSummary> AnalyzeAllAsync(
        IReadOnlyList<CleanupCategory> categories,
        CleanerOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteBatchAsync(categories, dryRun: true, options, progress, cancellationToken).ConfigureAwait(false);

    public async Task<CleanupSummary> CleanAllAsync(
        IReadOnlyList<CleanupCategory> categories,
        CleanerOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteBatchAsync(categories, dryRun: false, options, progress, cancellationToken).ConfigureAwait(false);

    private async Task<CleanupSummary> ExecuteBatchAsync(
        IReadOnlyList<CleanupCategory> categories,
        bool dryRun,
        CleanerOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<CategoryScanResult>(categories.Count);
        long totalBytes = 0;
        int totalFiles = 0;
        long cleanedBytes = 0;
        int cleanedFiles = 0;
        int skippedCount = 0;
        int accessDeniedCategories = 0;
        bool wasCancelled = false;

        for (int i = 0; i < categories.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                wasCancelled = true;
                break;
            }

            var category = categories[i];
            int index = i;

            try
            {
                var result = await Task.Run(
                    () => ExecuteCategory(category, dryRun, options, index, categories.Count, progress, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                results.Add(result);
                totalBytes += result.TotalBytes;
                totalFiles += result.FileCount;
                cleanedBytes += result.CleanedBytes;
                cleanedFiles += result.CleanedCount;
                skippedCount += result.SkippedCount;

                if (result.Status == CategoryStatus.AccessDenied)
                {
                    accessDeniedCategories++;
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                results.Add(CategoryScanResult.Empty(category.Id, CategoryStatus.Cancelled));
                break;
            }
        }

        stopwatch.Stop();
        return new CleanupSummary(
            WasDryRun: dryRun,
            CompletedAtUtc: options.UtcNowOverride ?? DateTimeOffset.UtcNow,
            Duration: stopwatch.Elapsed,
            TotalBytes: totalBytes,
            TotalFiles: totalFiles,
            CleanedBytes: cleanedBytes,
            CleanedFiles: cleanedFiles,
            SkippedCount: skippedCount,
            AccessDeniedCategories: accessDeniedCategories,
            WasCancelled: wasCancelled,
            CategoryResults: results);
    }

    private CategoryScanResult ExecuteCategory(
        CleanupCategory category,
        bool dryRun,
        CleanerOptions options,
        int completedCategories,
        int totalCategories,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var exclusions = new ExclusionMatcher(options.Exclusions);
        DateTimeOffset utcNow = options.UtcNowOverride ?? DateTimeOffset.UtcNow;

        long totalBytes = 0;
        int fileCount = 0;
        long cleanedBytes = 0;
        int cleanedCount = 0;
        int skippedCount = 0;
        bool sawAccessDenied = false;
        bool sawAnyValidTarget = false;
        var previewItems = new List<ScannedItem>();
        string? lastError = null;

        foreach (var target in category.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (target.Kind)
            {
                case TargetKind.RecycleBin:
                    sawAnyValidTarget = true;
                    HandleRecycleBinTarget(
                        category.Id,
                        dryRun,
                        previewItems,
                        ref totalBytes,
                        ref fileCount,
                        ref cleanedBytes,
                        ref cleanedCount,
                        ref skippedCount,
                        ref lastError);
                    break;

                case TargetKind.DnsCache:
                    sawAnyValidTarget = true;
                    HandleDnsCacheTarget(
                        category.Id,
                        dryRun,
                        previewItems,
                        ref fileCount,
                        ref cleanedCount,
                        ref skippedCount,
                        ref lastError);
                    break;

                case TargetKind.DirectoryContents:
                    ProcessDirectoryTarget(
                        category.Id,
                        target,
                        dryRun,
                        options,
                        exclusions,
                        utcNow,
                        completedCategories,
                        totalCategories,
                        progress,
                        previewItems,
                        ref totalBytes,
                        ref fileCount,
                        ref cleanedBytes,
                        ref cleanedCount,
                        ref skippedCount,
                        ref sawAccessDenied,
                        ref sawAnyValidTarget,
                        cancellationToken);
                    break;
            }
        }

        CategoryStatus status;
        if (cancellationToken.IsCancellationRequested)
        {
            status = CategoryStatus.Cancelled;
        }
        else if (!sawAnyValidTarget && sawAccessDenied)
        {
            status = CategoryStatus.AccessDenied;
        }
        else if (lastError is not null && fileCount == 0 && cleanedCount == 0)
        {
            status = CategoryStatus.Error;
        }
        else if (!sawAnyValidTarget)
        {
            status = CategoryStatus.NotFound;
        }
        else
        {
            status = dryRun ? CategoryStatus.Analyzed : CategoryStatus.Cleaned;
        }

        // Show largest discovered files first so the inspector immediately highlights heavy clutter.
        previewItems.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));

        return new CategoryScanResult(
            CategoryId: category.Id,
            Status: status,
            TotalBytes: totalBytes,
            FileCount: fileCount,
            CleanedBytes: cleanedBytes,
            CleanedCount: cleanedCount,
            SkippedCount: skippedCount,
            Items: previewItems,
            StatusDetail: lastError);
    }

    private void HandleRecycleBinTarget(
        string categoryId,
        bool dryRun,
        List<ScannedItem> previewItems,
        ref long totalBytes,
        ref int fileCount,
        ref long cleanedBytes,
        ref int cleanedCount,
        ref int skippedCount,
        ref string? lastError)
    {
        var info = _recycleBin.Query();
        int itemCount = (int)Math.Min(int.MaxValue, info.ItemCount);

        totalBytes += info.SizeBytes;
        fileCount += itemCount;

        if (info.SizeBytes > 0 || info.ItemCount > 0)
        {
            previewItems.Add(new ScannedItem(
                CategoryId: categoryId,
                FullPath: "shell:RecycleBinFolder",
                DisplayName: $"Recycle Bin ({itemCount})",
                SizeBytes: info.SizeBytes,
                LastModifiedUtc: DateTimeOffset.UtcNow,
                IsDirectory: false,
                IsSpecialAction: true));
        }

        if (!dryRun && (info.SizeBytes > 0 || info.ItemCount > 0))
        {
            if (_recycleBin.Empty(out string? error))
            {
                cleanedBytes += info.SizeBytes;
                cleanedCount += itemCount;
            }
            else
            {
                skippedCount += Math.Max(1, itemCount);
                lastError = error;
            }
        }
    }

    private void HandleDnsCacheTarget(
        string categoryId,
        bool dryRun,
        List<ScannedItem> previewItems,
        ref int fileCount,
        ref int cleanedCount,
        ref int skippedCount,
        ref string? lastError)
    {
        fileCount += 1;
        previewItems.Add(new ScannedItem(
            CategoryId: categoryId,
            FullPath: "system:DnsResolverCache",
            DisplayName: "DNS Resolver Cache",
            SizeBytes: 0,
            LastModifiedUtc: DateTimeOffset.UtcNow,
            IsDirectory: false,
            IsSpecialAction: true));

        if (!dryRun)
        {
            if (_dnsFlusher.Flush(out string? error))
            {
                cleanedCount += 1;
            }
            else
            {
                skippedCount += 1;
                lastError = error;
            }
        }
    }

    private static void ProcessDirectoryTarget(
        string categoryId,
        CleanupTarget target,
        bool dryRun,
        CleanerOptions options,
        ExclusionMatcher exclusions,
        DateTimeOffset utcNow,
        int completedCategories,
        int totalCategories,
        IProgress<ScanProgress>? progress,
        List<ScannedItem> previewItems,
        ref long totalBytes,
        ref int fileCount,
        ref long cleanedBytes,
        ref int cleanedCount,
        ref int skippedCount,
        ref bool sawAccessDenied,
        ref bool sawAnyValidTarget,
        CancellationToken cancellationToken)
    {
        if (!SafetyGuard.IsDirectorySafe(target.Path) || exclusions.IsExcluded(target.Path))
        {
            return;
        }

        if (!Directory.Exists(target.Path))
        {
            return;
        }

        var rootDir = new DirectoryInfo(target.Path);
        if (SafetyGuard.IsReparsePoint(rootDir))
        {
            return;
        }

        var dirsToVisit = new Stack<DirectoryInfo>();
        var visitedSubdirectories = new List<DirectoryInfo>();
        dirsToVisit.Push(rootDir);

        int progressTick = 0;

        while (dirsToVisit.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDir = dirsToVisit.Pop();

            if (++progressTick % 20 == 1)
            {
                progress?.Report(new ScanProgress(
                    categoryId,
                    currentDir.FullName,
                    completedCategories,
                    totalCategories,
                    totalBytes,
                    fileCount));
            }

            IEnumerable<FileInfo> files;
            try
            {
                files = currentDir.EnumerateFiles(target.SearchPattern, SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                sawAccessDenied = true;
                continue;
            }
            catch (SecurityException)
            {
                sawAccessDenied = true;
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            sawAnyValidTarget = true;

            try
            {
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (SafetyGuard.IsReparsePoint(file) || SafetyGuard.IsProtectedPath(file.FullName))
                    {
                        continue;
                    }

                    if (exclusions.IsExcluded(file.FullName))
                    {
                        continue;
                    }

                    if (target.RespectMinimumAge && !IsOlderThanMinimumAge(file, options.MinimumFileAge, utcNow))
                    {
                        continue;
                    }

                    long length;
                    DateTimeOffset modifiedUtc;
                    try
                    {
                        length = file.Length;
                        modifiedUtc = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        skippedCount++;
                        continue;
                    }

                    totalBytes += length;
                    fileCount++;

                    if (dryRun)
                    {
                        if (previewItems.Count < options.MaxPreviewItemsPerCategory)
                        {
                            previewItems.Add(new ScannedItem(
                                CategoryId: categoryId,
                                FullPath: file.FullName,
                                DisplayName: file.Name,
                                SizeBytes: length,
                                LastModifiedUtc: modifiedUtc));
                        }
                    }
                    else
                    {
                        if (TryDeleteFile(file))
                        {
                            cleanedBytes += length;
                            cleanedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                sawAccessDenied = true;
            }
            catch (IOException)
            {
                // Directory modified concurrently during enumeration; keep going safely.
            }

            if (!target.Recursive)
            {
                continue;
            }

            try
            {
                foreach (var subDir in currentDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (SafetyGuard.IsReparsePoint(subDir) ||
                        SafetyGuard.IsProtectedPath(subDir.FullName) ||
                        exclusions.IsExcluded(subDir.FullName))
                    {
                        continue;
                    }

                    dirsToVisit.Push(subDir);
                    if (!dryRun && target.DeleteEmptySubdirectories)
                    {
                        visitedSubdirectories.Add(subDir);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                sawAccessDenied = true;
            }
            catch (IOException)
            {
                // Ignore transient filesystem enumeration issues.
            }
        }

        if (!dryRun && target.DeleteEmptySubdirectories && visitedSubdirectories.Count > 0)
        {
            DeleteEmptySubdirectoriesBottomUp(visitedSubdirectories, target.RespectMinimumAge, options.MinimumFileAge, utcNow);
        }
    }

    private static bool IsOlderThanMinimumAge(FileInfo file, TimeSpan minimumAge, DateTimeOffset utcNow)
    {
        if (minimumAge <= TimeSpan.Zero)
        {
            return true;
        }

        try
        {
            DateTime newestUtc = file.LastWriteTimeUtc > file.CreationTimeUtc
                ? file.LastWriteTimeUtc
                : file.CreationTimeUtc;
            return (utcNow.UtcDateTime - newestUtc) >= minimumAge;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDeleteFile(FileInfo file)
    {
        try
        {
            if ((file.Attributes & FileAttributes.ReadOnly) != 0)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }

            file.Delete();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    private static void DeleteEmptySubdirectoriesBottomUp(
        List<DirectoryInfo> directories,
        bool respectMinimumAge,
        TimeSpan minimumAge,
        DateTimeOffset utcNow)
    {
        // Deepest paths first so nested empty folders collapse cleanly.
        directories.Sort((a, b) => b.FullName.Length.CompareTo(a.FullName.Length));

        foreach (var dir in directories)
        {
            try
            {
                if (!dir.Exists || SafetyGuard.IsReparsePoint(dir) || SafetyGuard.IsProtectedPath(dir.FullName))
                {
                    continue;
                }

                // Deleting child files updates LastWriteTimeUtc to now on Windows, so CreationTimeUtc reflects folder age.
                if (respectMinimumAge && minimumAge > TimeSpan.Zero && (utcNow.UtcDateTime - dir.CreationTimeUtc) < minimumAge)
                {
                    continue;
                }

                if (!Directory.EnumerateFileSystemEntries(dir.FullName).Any())
                {
                    if ((dir.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        dir.Attributes &= ~FileAttributes.ReadOnly;
                    }

                    dir.Delete(recursive: false);
                }
            }
            catch
            {
                // Ignore locked or already-removed directories.
            }
        }
    }
}
