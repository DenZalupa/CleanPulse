using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Provides built-in Windows, browser, GPU, and developer cache categories plus conversion of user custom rules.
/// </summary>
public static class DefaultCatalog
{
    public static IReadOnlyList<CleanupCategory> CreateDefaultCategories()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string systemDriveRoot = !string.IsNullOrEmpty(windows) ? (Path.GetPathRoot(windows) ?? @"C:\") : @"C:\";
        string tempPath = Path.GetTempPath();

        var userTempTargets = BuildUniqueDirectoryTargets(
            [tempPath, CombineIfNotEmpty(localAppData, "Temp")],
            searchPattern: "*",
            recursive: true,
            deleteEmptySubdirectories: true,
            respectMinimumAge: true);

        return
        [
            // --- SYSTEM & TEMP ---
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
                Id: "gpu_driver_cache",
                Group: CleanupGroup.SystemAndTemp,
                TitleKey: "cat_gpu_cache_title",
                DescriptionKey: "cat_gpu_cache_desc",
                SafetyNoteKey: "cat_gpu_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(localAppData, "NVIDIA", "DXCache"),
                        CombineIfNotEmpty(localAppData, "NVIDIA", "GLCache"),
                        CombineIfNotEmpty(localAppData, "AMD", "DxCache"),
                        CombineIfNotEmpty(localAppData, "AMD", "GLCache"),
                        CombineIfNotEmpty(localAppData, "Intel", "ShaderCache")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            // --- WINDOWS CACHE & NETWORK ---
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
                Id: "windows_old",
                Group: CleanupGroup.WindowsCache,
                TitleKey: "cat_windows_old_title",
                DescriptionKey: "cat_windows_old_desc",
                SafetyNoteKey: "cat_windows_old_safety",
                RequiresAdmin: true,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [Path.Combine(systemDriveRoot, "Windows.old")],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
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

            // --- BROWSER CACHES (SAFE CACHE ONLY) ---
            new CleanupCategory(
                Id: "browser_chrome_cache",
                Group: CleanupGroup.Browsers,
                TitleKey: "cat_browser_chrome_title",
                DescriptionKey: "cat_browser_chrome_desc",
                SafetyNoteKey: "cat_browser_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildChromiumCacheTargets(CombineIfNotEmpty(localAppData, "Google", "Chrome", "User Data")),
                ProcessNameToWarn: "chrome"),

            new CleanupCategory(
                Id: "browser_edge_cache",
                Group: CleanupGroup.Browsers,
                TitleKey: "cat_browser_edge_title",
                DescriptionKey: "cat_browser_edge_desc",
                SafetyNoteKey: "cat_browser_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildChromiumCacheTargets(CombineIfNotEmpty(localAppData, "Microsoft", "Edge", "User Data")),
                ProcessNameToWarn: "msedge"),

            new CleanupCategory(
                Id: "browser_yandex_cache",
                Group: CleanupGroup.Browsers,
                TitleKey: "cat_browser_yandex_title",
                DescriptionKey: "cat_browser_yandex_desc",
                SafetyNoteKey: "cat_browser_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildChromiumCacheTargets(CombineIfNotEmpty(localAppData, "Yandex", "YandexBrowser", "User Data")),
                ProcessNameToWarn: "browser"),

            new CleanupCategory(
                Id: "browser_firefox_cache",
                Group: CleanupGroup.Browsers,
                TitleKey: "cat_browser_firefox_title",
                DescriptionKey: "cat_browser_firefox_desc",
                SafetyNoteKey: "cat_browser_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildFirefoxCacheTargets(CombineIfNotEmpty(localAppData, "Mozilla", "Firefox", "Profiles")),
                ProcessNameToWarn: "firefox"),

            new CleanupCategory(
                Id: "browser_opera_cache",
                Group: CleanupGroup.Browsers,
                TitleKey: "cat_browser_opera_title",
                DescriptionKey: "cat_browser_opera_desc",
                SafetyNoteKey: "cat_browser_cache_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: true,
                IsCaution: false,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(localAppData, "Opera Software", "Opera Stable", "Cache"),
                        CombineIfNotEmpty(localAppData, "Opera Software", "Opera GX Stable", "Cache"),
                        CombineIfNotEmpty(appData, "Opera Software", "Opera Stable", "GPUCache"),
                        CombineIfNotEmpty(appData, "Opera Software", "Opera GX Stable", "GPUCache")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false),
                ProcessNameToWarn: "opera"),

            // --- DEV & APPS CACHES ---
            new CleanupCategory(
                Id: "dev_nuget_cache",
                Group: CleanupGroup.DevAndApps,
                TitleKey: "cat_dev_nuget_title",
                DescriptionKey: "cat_dev_nuget_desc",
                SafetyNoteKey: "cat_dev_nuget_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(localAppData, "NuGet", "v3-cache"),
                        CombineIfNotEmpty(localAppData, "NuGet", "plugins-cache")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "dev_pip_npm_cache",
                Group: CleanupGroup.DevAndApps,
                TitleKey: "cat_dev_pip_npm_title",
                DescriptionKey: "cat_dev_pip_npm_desc",
                SafetyNoteKey: "cat_dev_pip_npm_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(localAppData, "pip", "Cache"),
                        CombineIfNotEmpty(localAppData, "npm-cache"),
                        CombineIfNotEmpty(appData, "npm-cache")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            new CleanupCategory(
                Id: "dev_vscode_cache",
                Group: CleanupGroup.DevAndApps,
                TitleKey: "cat_dev_vscode_title",
                DescriptionKey: "cat_dev_vscode_desc",
                SafetyNoteKey: "cat_dev_vscode_safety",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: true,
                Targets: BuildUniqueDirectoryTargets(
                    [
                        CombineIfNotEmpty(appData, "Code", "Cache"),
                        CombineIfNotEmpty(appData, "Code", "CachedData"),
                        CombineIfNotEmpty(appData, "Code", "GPUCache"),
                        CombineIfNotEmpty(appData, "Code", "logs")
                    ],
                    searchPattern: "*",
                    recursive: true,
                    deleteEmptySubdirectories: true,
                    respectMinimumAge: false)),

            // --- LOGS & DUMPS ---
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

    /// <summary>
    /// Converts validated user-defined cleanup rules into cleaner categories.
    /// </summary>
    public static IReadOnlyList<CleanupCategory> FromCustomRules(IEnumerable<CustomCleanupRule>? rules)
    {
        if (rules is null)
        {
            return Array.Empty<CleanupCategory>();
        }

        var list = new List<CleanupCategory>();
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.FolderPath) || !SafetyGuard.IsDirectorySafe(rule.FolderPath))
            {
                continue;
            }

            string? normalized = SafetyGuard.TryNormalizePath(rule.FolderPath);
            if (normalized is null)
            {
                continue;
            }

            string pattern = string.IsNullOrWhiteSpace(rule.SearchPattern) ? "*" : rule.SearchPattern.Trim();
            string name = string.IsNullOrWhiteSpace(rule.Name) ? Path.GetFileName(normalized) : rule.Name.Trim();
            string desc = $"{normalized} ({pattern})";

            list.Add(new CleanupCategory(
                Id: $"custom_{rule.Id}",
                Group: CleanupGroup.CustomRules,
                TitleKey: "cat_custom_rule_title",
                DescriptionKey: "cat_custom_rule_desc",
                SafetyNoteKey: rule.MoveToRecycleBin ? "cat_custom_rule_safety_recycle" : "cat_custom_rule_safety_perm",
                RequiresAdmin: false,
                IsRecommendedDefault: false,
                IsCaution: !rule.MoveToRecycleBin,
                Targets:
                [
                    CleanupTarget.Directory(
                        normalized,
                        searchPattern: pattern,
                        recursive: rule.Recursive,
                        deleteEmptySubdirectories: rule.Recursive && pattern == "*",
                        respectMinimumAge: rule.RespectMinimumAge,
                        moveToRecycleBin: rule.MoveToRecycleBin)
                ],
                DisplayNameOverride: name,
                DescriptionOverride: desc,
                IsCustomRule: true));
        }

        return list;
    }

    private static IReadOnlyList<CleanupTarget> BuildChromiumCacheTargets(string userDataRoot)
    {
        if (string.IsNullOrWhiteSpace(userDataRoot))
        {
            return Array.Empty<CleanupTarget>();
        }

        var profileDirs = new List<string>();
        if (Directory.Exists(userDataRoot))
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(userDataRoot))
                {
                    string name = Path.GetFileName(dir);
                    if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    {
                        profileDirs.Add(dir);
                    }
                }
            }
            catch
            {
                // Fall back to Default if directory enumeration fails.
            }
        }

        if (profileDirs.Count == 0)
        {
            profileDirs.Add(Path.Combine(userDataRoot, "Default"));
        }

        var paths = new List<string>();
        foreach (string profile in profileDirs)
        {
            paths.Add(Path.Combine(profile, "Cache"));
            paths.Add(Path.Combine(profile, "Code Cache"));
            paths.Add(Path.Combine(profile, "GPUCache"));
            paths.Add(Path.Combine(profile, "GrShaderCache"));
        }

        return BuildUniqueDirectoryTargets(paths, "*", recursive: true, deleteEmptySubdirectories: true, respectMinimumAge: false);
    }

    private static IReadOnlyList<CleanupTarget> BuildFirefoxCacheTargets(string profilesRoot)
    {
        if (string.IsNullOrWhiteSpace(profilesRoot))
        {
            return Array.Empty<CleanupTarget>();
        }

        var paths = new List<string>();
        if (Directory.Exists(profilesRoot))
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(profilesRoot))
                {
                    paths.Add(Path.Combine(dir, "cache2"));
                    paths.Add(Path.Combine(dir, "startupCache"));
                }
            }
            catch
            {
                // Ignore enumeration error.
            }
        }

        if (paths.Count == 0)
        {
            paths.Add(profilesRoot);
        }

        return BuildUniqueDirectoryTargets(paths, "*", recursive: true, deleteEmptySubdirectories: true, respectMinimumAge: false);
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
