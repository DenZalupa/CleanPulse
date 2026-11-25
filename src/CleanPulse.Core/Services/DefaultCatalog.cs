using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Provides the built-in catalog of safe Windows system, cache, and log cleanup categories.
/// </summary>
public static class DefaultCatalog
{
    public static IReadOnlyList<CleanupCategory> CreateDefaultCategories()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string tempPath = Path.GetTempPath();

        var userTempTargets = BuildUniqueDirectoryTargets(
            [tempPath, CombineIfNotEmpty(localAppData, "Temp")],
            searchPattern: "*",
            recursive: true,
            deleteEmptySubdirectories: true,
            respectMinimumAge: true);

        return
        [
            new CleanupCategory(
                Id: "user_temp",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_user_temp_title",
                DescriptionKey: "cat_user_temp_desc",
                SafetyNoteKey: "cat_user_temp_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: userTempTargets),

            new CleanupCategory(
                Id: "windows_temp",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_windows_temp_title",
                DescriptionKey: "cat_windows_temp_desc",
                SafetyNoteKey: "cat_windows_temp_safety",
                RequiresAdmin: true,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildUniqueDirectoryTargets(
                    [CombineIfNotEmpty(windows, "Temp")],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: true)),

            new CleanupCategory(
                Id: "recycle_bin",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_recycle_bin_title",
                DescriptionKey: "cat_recycle_bin_desc",
                SafetyNoteKey: "cat_recycle_bin_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: [CleanupTarget.RecycleBin()]),

            new CleanupCategory(
                Id: "thumbnail_cache",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_thumbnail_cache_title",
                DescriptionKey: "cat_thumbnail_cache_desc",
                SafetyNoteKey: "cat_thumbnail_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildMultiPatternTargets(
                    CombineIfNotEmpty(localAppData, "Microsoft", "Windows", "Explorer"),
                    ["thumbcache_*.db", "iconcache_*.db"],
                    recursive: false,
                    deleteEmptySubdirectories: false,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "recent_items",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_recent_items_title",
                DescriptionKey: "cat_recent_items_desc",
                SafetyNoteKey: "cat_recent_items_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [CombineIfNotEmpty(appData, "Microsoft", "Windows", "Recent")],
                    searchPattern: "*.lnk",
                    recursive: false,
                    deleteEmptySubdirectories: false,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "directx_shader_cache",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_directx_shader_title",
                DescriptionKey: "cat_directx_shader_desc",
                SafetyNoteKey: "cat_directx_shader_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [CombineIfNotEmpty(localAppData, "D3DSCache")],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "windows_update_cache",
                Group: CleanupGroup.WindowsCache,
                TitleKey: "cat_windows_update_title",
                DescriptionKey: "cat_windows_update_desc",
                SafetyNoteKey: "cat_windows_update_safety",
                RequiresAdmin: true,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildUniqueDirectoryTargets(
                    [CombineIfNotEmpty(windows, "SoftwareDistribution", "Download")],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "delivery_optimization",
                Group: CleanupGroup.WindowsCache,
                TitleKey: "cat_delivery_opt_title",
                DescriptionKey: "cat_delivery_opt_desc",
                SafetyNoteKey: "cat_delivery_opt_safety",
                RequiresAdmin: true,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(windows, "SoftwareDistribution", "DeliveryOptimization"),
                        CombineIfNotEmpty(windows, "ServiceProfiles", "NetworkService", "AppData", "Local", "Microsoft", "Windows", "DeliveryOptimization", "Cache")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "windows_prefetch",
                Group: CleanupGroup.WindowsCache,
                TitleKey: "cat_windows_prefetch_title",
                DescriptionKey: "cat_windows_prefetch_desc",
                SafetyNoteKey: "cat_windows_prefetch_safety",
                RequiresAdmin: true,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [CombineIfNotEmpty(windows, "Prefetch")],
                    searchPattern: "*.pf",
                    recursive: false,
                    deleteEmptySubdirectories: false,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "dns_cache",
                Group: CleanupGroup.WindowsCache,
                TitleKey: "cat_dns_cache_title",
                DescriptionKey: "cat_dns_cache_desc",
                SafetyNoteKey: "cat_dns_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: [CleanupTarget.DnsCache()]),

            new CleanupCategory(
                Id: "wer_reports",
                Group: CleanupGroup.LogsAndDumps,
                TitleKey: "cat_wer_reports_title",
                DescriptionKey: "cat_wer_reports_desc",
                SafetyNoteKey: "cat_wer_reports_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(localAppData, "Microsoft", "Windows", "WER", "ReportArchive"),
                        CombineIfNotEmpty(localAppData, "Microsoft", "Windows", "WER", "ReportQueue"),
                        CombineIfNotEmpty(commonAppData, "Microsoft", "Windows", "WER", "ReportArchive"),
                        CombineIfNotEmpty(commonAppData, "Microsoft", "Windows", "WER", "ReportQueue")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "crash_dumps",
                Group: CleanupGroup.LogsAndDumps,
                TitleKey: "cat_crash_dumps_title",
                DescriptionKey: "cat_crash_dumps_desc",
                SafetyNoteKey: "cat_crash_dumps_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(localAppData, "CrashDumps"),
                        CombineIfNotEmpty(windows, "Minidump")
                    ],
                    searchPattern: "*.dmp",
                    recursive: false,
                    deleteEmptySubdirectories: false,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "windows_logs",
                Group: CleanupGroup.LogsAndDumps,
                TitleKey: "cat_windows_logs_title",
                DescriptionKey: "cat_windows_logs_desc",
                SafetyNoteKey: "cat_windows_logs_safety",
                RequiresAdmin: true,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets:
                [
                    ..BuildUniqueDirectoryTargets(
                        [
                            CombineIfNotEmpty(windows, "Logs", "CBS"),
                            CombineIfNotEmpty(windows, "Logs", "DISM"),
                            CombineIfNotEmpty(windows, "Panther")
                        ],
                        searchPattern: "*.log",
                        recursive: false,
                        deleteEmptySubdirectories: false,
                        respectMinimumAge: true),
                    ..BuildUniqueDirectoryTargets(
                        [CombineIfNotEmpty(windows, "Logs", "WindowsUpdate")],
                        searchPattern: "*.etl",
                        recursive: false,
                        deleteEmptySubdirectories: false,
                        respectMinimumAge: true),
                    ..BuildUniqueDirectoryTargets(
                        [CombineIfNotEmpty(windows, "INF")],
                        searchPattern: "setupapi*.log",
                        recursive: false,
                        deleteEmptySubdirectories: false,
                        respectMinimumAge: true)
                ])
        ];
    }

    private static string CombineIfNotEmpty(string root, params string[] parts)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return string.Empty;
        }

        string[] all = new string[parts.Length + 1];
        all[0] = root;
        Array.Copy(parts, 0, all, 1, parts.Length);
        return Path.Combine(all);
    }

    private static IReadOnlyList<CleanupTarget> BuildUniqueDirectoryTargets(
        IEnumerable<string> paths,
        string searchPattern,
        bool recursive,
        bool deleteEmptySubdirectories,
        bool respectMinimumAge)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<CleanupTarget>();

        foreach (string raw in paths)
        {
            string? normalized = SafetyGuard.TryNormalizePath(raw);
            if (normalized is null || !seen.Add(normalized))
            {
                continue;
            }

            list.Add(CleanupTarget.Directory(
                normalized,
                searchPattern,
                recursive,
                deleteEmptySubdirectories,
                respectMinimumAge));
        }

        return list;
    }

    private static IReadOnlyList<CleanupTarget> BuildMultiPatternTargets(
        string path,
        IEnumerable<string> patterns,
        bool recursive,
        bool deleteEmptySubdirectories,
        bool respectMinimumAge)
    {
        string? normalized = SafetyGuard.TryNormalizePath(path);
        if (normalized is null)
        {
            return Array.Empty<CleanupTarget>();
        }

        return patterns
            .Select(pattern => CleanupTarget.Directory(
                normalized,
                pattern,
                recursive,
                deleteEmptySubdirectories,
                respectMinimumAge))
            .ToList();
    }
}
