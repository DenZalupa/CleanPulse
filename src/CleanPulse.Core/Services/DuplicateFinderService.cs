using System.Diagnostics;
using System.Security.Cryptography;
using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Finds byte-identical duplicate files using 3-stage filtering: size -> 4KB prefix hash -> full SHA-256.
/// </summary>
public sealed class DuplicateFinderService
{
    private const int PrefixBytes = 4096;
    private readonly IFileDeletionService _fileDeleter;

    public DuplicateFinderService(IFileDeletionService? fileDeleter = null)
    {
        _fileDeleter = fileDeleter ?? new WindowsFileDeletionService();
    }

    public Task<DuplicateScanResult> ScanAsync(
        string rootPath,
        long minimumFileSizeBytes = 1024 * 1024,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ExecuteScan(rootPath, minimumFileSizeBytes, progress, cancellationToken), cancellationToken);

    public (int Deleted, int Skipped, long FreedBytes) DeleteFiles(IEnumerable<string> filePaths, bool moveToRecycleBin)
    {
        int deleted = 0;
        int skipped = 0;
        long freed = 0;

        foreach (string path in filePaths)
        {
            if (SafetyGuard.IsProtectedOsPath(path) || !File.Exists(path))
            {
                skipped++;
                continue;
            }

            var info = new FileInfo(path);
            if (SafetyGuard.IsReparsePoint(info))
            {
                skipped++;
                continue;
            }

            long size = 0;
            try
            {
                size = info.Length;
            }
            catch
            {
                // Ignore size read failures.
            }

            if (_fileDeleter.TryDeleteFile(info, moveToRecycleBin))
            {
                deleted++;
                freed += size;
            }
            else
            {
                skipped++;
            }
        }

        return (deleted, skipped, freed);
    }

    private static DuplicateScanResult ExecuteScan(
        string rootPath,
        long minimumFileSizeBytes,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        if (!SafetyGuard.IsDirectorySafeForAnalysis(rootPath))
        {
            return new DuplicateScanResult(rootPath, 0, 0, 0, sw.Elapsed, Array.Empty<DuplicateGroup>());
        }

        string normalizedRoot = SafetyGuard.TryNormalizePath(rootPath) ?? rootPath;
        var bySize = new Dictionary<long, List<DuplicateFileEntry>>();
        int scannedFiles = 0;
        int tick = 0;

        var stack = new Stack<DirectoryInfo>();
        stack.Push(new DirectoryInfo(normalizedRoot));

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dir = stack.Pop();
            if (++tick % 20 == 1)
            {
                progress?.Report(dir.FullName);
            }

            try
            {
                foreach (var file in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
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
                        if (length < minimumFileSizeBytes || length == 0)
                        {
                            continue;
                        }

                        modified = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);
                    }
                    catch
                    {
                        continue;
                    }

                    scannedFiles++;
                    if (!bySize.TryGetValue(length, out var list))
                    {
                        list = new List<DuplicateFileEntry>();
                        bySize[length] = list;
                    }

                    list.Add(new DuplicateFileEntry(
                        file.FullName,
                        file.Name,
                        file.DirectoryName ?? string.Empty,
                        length,
                        modified));
                }
            }
            catch
            {
                // Skip protected directories gracefully.
            }

            try
            {
                foreach (var subDir in dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!SafetyGuard.IsReparsePoint(subDir))
                    {
                        stack.Push(subDir);
                    }
                }
            }
            catch
            {
                // Ignore access errors.
            }
        }

        var sizeCandidates = bySize.Values.Where(g => g.Count > 1).ToList();
        var duplicateGroups = new List<DuplicateGroup>();

        foreach (var sizeBucket in sizeCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var byPrefix = new Dictionary<string, List<DuplicateFileEntry>>(StringComparer.Ordinal);

            foreach (var entry in sizeBucket)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? prefixHash = TryComputePrefixHash(entry.FullPath);
                if (prefixHash is null)
                {
                    continue;
                }

                if (!byPrefix.TryGetValue(prefixHash, out var prefixList))
                {
                    prefixList = new List<DuplicateFileEntry>();
                    byPrefix[prefixHash] = prefixList;
                }

                prefixList.Add(entry);
            }

            foreach (var prefixBucket in byPrefix.Values.Where(v => v.Count > 1))
            {
                var byFullHash = new Dictionary<string, List<DuplicateFileEntry>>(StringComparer.Ordinal);
                foreach (var entry in prefixBucket)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(entry.FullPath);
                    string? fullHash = TryComputeFullHash(entry.FullPath);
                    if (fullHash is null)
                    {
                        continue;
                    }

                    if (!byFullHash.TryGetValue(fullHash, out var fullList))
                    {
                        fullList = new List<DuplicateFileEntry>();
                        byFullHash[fullHash] = fullList;
                    }

                    fullList.Add(entry);
                }

                foreach (var (hash, files) in byFullHash)
                {
                    if (files.Count <= 1)
                    {
                        continue;
                    }

                    files.Sort((a, b) => a.LastModifiedUtc.CompareTo(b.LastModifiedUtc));
                    duplicateGroups.Add(new DuplicateGroup(hash, files[0].SizeBytes, files));
                }
            }
        }

        duplicateGroups.Sort((a, b) => b.ReclaimableBytes.CompareTo(a.ReclaimableBytes));
        long totalReclaimable = duplicateGroups.Sum(g => g.ReclaimableBytes);
        sw.Stop();

        return new DuplicateScanResult(
            normalizedRoot,
            scannedFiles,
            sizeCandidates.Count,
            totalReclaimable,
            sw.Elapsed,
            duplicateGroups);
    }

    private static string? TryComputePrefixHash(string path)
    {
        try
        {
            byte[] buffer = new byte[PrefixBytes];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int read = fs.Read(buffer, 0, buffer.Length);
            byte[] hash = SHA256.HashData(buffer.AsSpan(0, read));
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryComputeFullHash(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536);
            byte[] hash = SHA256.HashData(fs);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;
        }
    }
}
