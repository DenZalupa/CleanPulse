using System.IO;
using CleanPulse.Core.Formatting;
using CleanPulse.Core.Models;
using CleanPulse.Core.Services;
using CleanPulse.Services;
using CleanPulse.ViewModels;

namespace CleanPulse.Tests;

internal sealed class FakeRecycleBin : IShellRecycleBin
{
    public RecycleBinInfo NextQuery { get; set; } = new(4096, 3);
    public bool EmptyCalled { get; private set; }
    public RecycleBinInfo Query() => NextQuery;
    public bool Empty(out string? error)
    {
        EmptyCalled = true;
        error = null;
        NextQuery = new RecycleBinInfo(0, 0);
        return true;
    }
}

internal sealed class FakeDnsFlusher : IDnsCacheFlusher
{
    public bool FlushCalled { get; private set; }
    public bool Flush(out string? error)
    {
        FlushCalled = true;
        error = null;
        return true;
    }
}

internal sealed class FakeFileDeletionService : IFileDeletionService
{
    public List<(string Path, bool Recycled)> DeletedCalls { get; } = new();

    public bool TryDeleteFile(FileInfo file, bool moveToRecycleBin)
    {
        DeletedCalls.Add((file.FullName, moveToRecycleBin));
        try
        {
            if ((file.Attributes & FileAttributes.ReadOnly) != 0)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }

            file.Delete();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class FakePrivilegeService : IPrivilegeService
{
    public bool Admin { get; set; }
    public bool IsAdministrator() => Admin;
    public bool RestartAsAdministrator() => false;
}

internal sealed class FakeUiService : IUiPlatformService
{
    private readonly WpfUiPlatformService _themeLoader = new();
    public string LastAppliedTheme { get; private set; } = string.Empty;
    public bool Confirm(string title, string message) => true;
    public void ShowInfo(string title, string message) { }
    public string? BrowseFolder() => null;
    public string? SaveTextFile(string defaultFileName) => null;
    public void OpenFolderInExplorer(string path) { }
    public void ApplyTheme(string themeName)
    {
        LastAppliedTheme = themeName;
        _themeLoader.ApplyTheme(themeName);
    }
}

internal sealed class FakeProcessDetector : IProcessDetector
{
    public HashSet<string> Running { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsProcessRunning(string processName) => Running.Contains(processName);
}

internal sealed class FakeDismService : IDismService
{
    public Task<DismCleanupResult> RunComponentCleanupAsync(IProgress<string>? outputLineProgress = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DismCleanupResult(true, 0, "Completed", TimeSpan.FromSeconds(1.2)));
}

internal sealed class FakeScheduledTaskService : IScheduledTaskService
{
    public ScheduleFrequency Current { get; set; } = ScheduleFrequency.Disabled;
    public ScheduleFrequency GetCurrentFrequency() => Current;
    public bool ApplySchedule(ScheduleFrequency frequency, string executablePath, out string? error)
    {
        Current = frequency;
        error = null;
        return true;
    }
}

internal sealed class FakeStartupManagerService : IStartupManagerService
{
    private readonly List<StartupItem> _items =
    [
        new("AppA", "AppA", "\"C:\\Program Files\\AppA\\app.exe\" --tray", "HKCU\\Run", StartupScope.CurrentUserRun, true, false),
        new("SysSvc", "SysSvc", "C:\\Windows\\sys.exe", "HKLM\\Run", StartupScope.LocalMachineRun, true, true)
    ];

    public IReadOnlyList<StartupItem> GetStartupItems() => _items;

    public bool SetEnabled(StartupItem item, bool enabled, out string? error)
    {
        error = null;
        int idx = _items.FindIndex(i => i.Id == item.Id);
        if (idx >= 0)
        {
            _items[idx] = _items[idx] with { IsEnabled = enabled };
        }

        return true;
    }
}

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var tests = new (string Name, Func<Task> Body)[]
        {
            ("SafetyGuard distinguishes strict cleaner paths vs OS-only paths", TestSafetyGuard),
            ("ExclusionMatcher handles prefixes and wildcards", TestExclusionMatcher),
            ("CleanerEngine dry-run analyzes without deleting", TestCleanerAnalyzeOnly),
            ("CleanerEngine respects minimum file age", TestCleanerMinimumAge),
            ("CleanerEngine cleans files, readonly files, and empty subdirs while skipping locked files", TestCleanerCleanAndLockedFiles),
            ("CleanerEngine handles non-recursive pattern filtering", TestCleanerNonRecursivePattern),
            ("CleanerEngine invokes RecycleBin, DnsCache, and soft-delete Custom Rules", TestSpecialTargetsAndCustomRules),
            ("CleanerEngine supports cancellation", TestCancellation),
            ("DiskAnalyzerService ranks subfolders, top files, and classifies extensions", TestDiskAnalyzer),
            ("DuplicateFinderService verifies 4KB prefix + full SHA-256 without false positives", TestDuplicateFinder),
            ("StartupApprovedCodec and path extractor handle Windows flags", TestStartupCodec),
            ("CliRunner executes headless --help, --analyze, and --clean-selected", TestCliRunner),
            ("SettingsService and HistoryService persist reliably", TestPersistence),
            ("LocalizationCatalog has 100% RU/EN key parity and covers DefaultCatalog", TestLocalizationParity),
            ("ByteFormatter formats RU and EN sizes accurately", TestByteFormatter),
            ("WPF App, all 7 NavSections, Themes, and MainViewModel initialize cleanly", TestWpfUiSmoke)
        };

        int failed = 0;
        foreach (var (name, body) in tests)
        {
            try
            {
                body().GetAwaiter().GetResult();
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"[FAIL] {name}: {ex}");
            }
        }

        Console.WriteLine($"\nSummary: {tests.Length - failed}/{tests.Length} passed.");
        return failed == 0 ? 0 : 1;
    }

    private static Task TestSafetyGuard()
    {
        AssertEx.True(SafetyGuard.IsProtectedPath(null), "null should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(@"C:\"), "drive root should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)), "Windows root should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(Environment.GetFolderPath(Environment.SpecialFolder.System)), "System32 should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)), "Program Files should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), "UserProfile root should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)), "Desktop should be protected");
        AssertEx.True(SafetyGuard.IsProtectedPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)), "Documents should be protected");

        string downloadsSub = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "installer.exe");
        AssertEx.True(SafetyGuard.IsProtectedPath(downloadsSub), "Cleaner must block personal Downloads");
        AssertEx.False(SafetyGuard.IsProtectedOsPath(downloadsSub), "Manual tools may act on user Downloads files");
        AssertEx.True(SafetyGuard.IsProtectedOsPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe")), "Manual tools must never touch System32");

        string safeTemp = Path.Combine(Path.GetTempPath(), "CleanPulseUnitSandbox");
        AssertEx.False(SafetyGuard.IsProtectedPath(safeTemp), "Temp subfolder must not be marked protected");
        return Task.CompletedTask;
    }

    private static Task TestExclusionMatcher()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "CleanPulseExcl");
        var matcher = new ExclusionMatcher(
        [
            Path.Combine(tempRoot, "keep"),
            "*.keep",
            "specific_file.tmp"
        ]);

        AssertEx.True(matcher.IsExcluded(Path.Combine(tempRoot, "keep", "sub", "a.txt")), "Excluded dir subtree should match");
        AssertEx.False(matcher.IsExcluded(Path.Combine(tempRoot, "keep2", "a.txt")), "Sibling prefix must not falsely match");
        AssertEx.True(matcher.IsExcluded(Path.Combine(tempRoot, "nested", "notes.KEEP")), "Wildcard should match case-insensitively");
        AssertEx.True(matcher.IsExcluded(Path.Combine(tempRoot, "specific_file.tmp")), "Exact filename should match");
        AssertEx.False(matcher.IsExcluded(Path.Combine(tempRoot, "delete_me.tmp")), "Unmatched file must not be excluded");
        return Task.CompletedTask;
    }

    private static async Task TestCleanerAnalyzeOnly()
    {
        using var sandbox = new TempSandbox();
        string fileA = sandbox.WriteFile("a.tmp", 100);
        string fileB = sandbox.WriteFile(Path.Combine("sub", "b.tmp"), 500);

        var category = MakeFolderCategory("test_temp", sandbox.RootPath, respectAge: false);
        var engine = new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher());

        var result = await engine.AnalyzeCategoryAsync(
            category,
            new CleanerOptions(TimeSpan.Zero, Array.Empty<string>()));

        AssertEx.Equal(CategoryStatus.Analyzed, result.Status, "Status should be Analyzed");
        AssertEx.Equal(2, result.FileCount, "Should find 2 files");
        AssertEx.Equal(600L, result.TotalBytes, "Total bytes should be 600");
        AssertEx.Equal(0L, result.CleanedBytes, "Dry-run must clean 0 bytes");
        AssertEx.Equal(fileB, result.Items[0].FullPath, "Preview items should sort largest first");
        AssertEx.True(File.Exists(fileA) && File.Exists(fileB), "Dry-run must leave all files on disk");
    }

    private static async Task TestCleanerMinimumAge()
    {
        using var sandbox = new TempSandbox();
        var now = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

        string oldFile = sandbox.WriteFile("old.tmp", 250, now.AddHours(-30));
        string freshFile = sandbox.WriteFile("fresh.tmp", 900, now.AddHours(-2));

        var category = MakeFolderCategory("age_test", sandbox.RootPath, respectAge: true);
        var engine = new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher());
        var options = new CleanerOptions(TimeSpan.FromHours(24), Array.Empty<string>(), UtcNowOverride: now);

        var summary = await engine.CleanAllAsync([category], options);

        AssertEx.Equal(1, summary.CleanedFiles, "Only the file older than 24h should be deleted");
        AssertEx.Equal(250L, summary.CleanedBytes, "Only old file size should be reclaimed");
        AssertEx.False(File.Exists(oldFile), "Old file should be deleted");
        AssertEx.True(File.Exists(freshFile), "Fresh file must be preserved");
    }

    private static async Task TestCleanerCleanAndLockedFiles()
    {
        using var sandbox = new TempSandbox();
        var oldTime = DateTimeOffset.UtcNow.AddDays(-3);

        string removable = sandbox.WriteFile(Path.Combine("nested", "old.tmp"), 300, oldTime);
        string readOnlyFile = sandbox.WriteFile(Path.Combine("nested", "ro.tmp"), 150, oldTime);
        File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);
        Directory.SetCreationTimeUtc(Path.Combine(sandbox.RootPath, "nested"), oldTime.UtcDateTime);

        string freshEmptyDir = Path.Combine(sandbox.RootPath, "fresh_empty");
        Directory.CreateDirectory(freshEmptyDir);

        string lockedFile = sandbox.WriteFile("locked.tmp", 200, oldTime);
        string excludedFile = sandbox.WriteFile(Path.Combine("keepdir", "important.tmp"), 400, oldTime);

        var category = MakeFolderCategory("clean_test", sandbox.RootPath, respectAge: true, deleteEmptySubdirs: true);
        var engine = new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher());
        var options = new CleanerOptions(TimeSpan.FromHours(24), [Path.Combine(sandbox.RootPath, "keepdir")]);

        CleanupSummary summary;
        using (new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            summary = await engine.CleanAllAsync([category], options);
        }

        AssertEx.Equal(2, summary.CleanedFiles, "Should delete removable + readonly file");
        AssertEx.Equal(450L, summary.CleanedBytes, "Should reclaim 300 + 150 bytes");
        AssertEx.Equal(1, summary.SkippedCount, "Should report 1 locked file skipped");
        AssertEx.False(File.Exists(removable), "Removable file should be deleted");
        AssertEx.False(File.Exists(readOnlyFile), "Read-only temp file should be deleted");
        AssertEx.True(File.Exists(lockedFile), "Locked file should remain");
        AssertEx.True(File.Exists(excludedFile), "Excluded file should remain");
        AssertEx.False(Directory.Exists(Path.Combine(sandbox.RootPath, "nested")), "Empty old nested folder should be removed");
        AssertEx.True(Directory.Exists(freshEmptyDir), "Fresh empty folder must be preserved by min-age guard");
        AssertEx.True(Directory.Exists(sandbox.RootPath), "Root target folder must remain intact");
    }

    private static async Task TestCleanerNonRecursivePattern()
    {
        using var sandbox = new TempSandbox();
        string rootLog = sandbox.WriteFile("root.log", 120);
        string rootTxt = sandbox.WriteFile("keep.txt", 120);
        string nestedLog = sandbox.WriteFile(Path.Combine("sub", "nested.log"), 120);

        var category = new CleanupCategory(
            "logs_only",
            CleanupGroup.LogsAndDumps,
            "cat_windows_logs_title",
            "cat_windows_logs_desc",
            "cat_windows_logs_safety",
            false,
            true,
            false,
            [CleanupTarget.Directory(sandbox.RootPath, searchPattern: "*.log", recursive: false, deleteEmptySubdirectories: false, respectMinimumAge: false)]);

        var engine = new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher());
        var summary = await engine.CleanAllAsync([category], new CleanerOptions(TimeSpan.Zero, Array.Empty<string>()));

        AssertEx.Equal(1, summary.CleanedFiles, "Only root .log should match");
        AssertEx.False(File.Exists(rootLog), "root.log should be deleted");
        AssertEx.True(File.Exists(rootTxt), "keep.txt must remain");
        AssertEx.True(File.Exists(nestedLog), "nested.log must remain when recursive=false");
    }

    private static async Task TestSpecialTargetsAndCustomRules()
    {
        using var sandbox = new TempSandbox();
        string ruleFile = sandbox.WriteFile("custom.log", 512);

        var customCategories = DefaultCatalog.FromCustomRules(
        [
            new CustomCleanupRule { Name = "Forbidden", FolderPath = @"C:\Windows\System32", SearchPattern = "*" },
            new CustomCleanupRule { Id = "r1", Name = "My Logs", FolderPath = sandbox.RootPath, SearchPattern = "*.log", Recursive = false, RespectMinimumAge = false, MoveToRecycleBin = true }
        ]);

        AssertEx.Equal(1, customCategories.Count, "Protected custom rule must be filtered out");

        var rb = new FakeRecycleBin();
        var dns = new FakeDnsFlusher();
        var deleter = new FakeFileDeletionService();
        var engine = new CleanerEngine(rb, dns, deleter);

        var categories = new[]
        {
            new CleanupCategory("recycle_bin", CleanupGroup.SystemAndTemp, "cat_recycle_bin_title", "cat_recycle_bin_desc", "cat_recycle_bin_safety", false, true, false, [CleanupTarget.RecycleBin()]),
            new CleanupCategory("dns_cache", CleanupGroup.WindowsCache, "cat_dns_cache_title", "cat_dns_cache_desc", "cat_dns_cache_safety", false, true, false, [CleanupTarget.DnsCache()]),
            customCategories[0]
        };

        var options = new CleanerOptions(TimeSpan.Zero, Array.Empty<string>());
        var analyze = await engine.AnalyzeAllAsync(categories, options);
        AssertEx.Equal(4096L + 512L, analyze.TotalBytes, "Analyze should sum RecycleBin + custom rule");
        AssertEx.False(rb.EmptyCalled, "Analyze must not empty Recycle Bin");
        AssertEx.False(dns.FlushCalled, "Analyze must not flush DNS");

        var clean = await engine.CleanAllAsync(categories, options);
        AssertEx.True(rb.EmptyCalled, "Clean must empty Recycle Bin");
        AssertEx.True(dns.FlushCalled, "Clean must flush DNS");
        AssertEx.Equal(1, deleter.DeletedCalls.Count, "Custom rule should invoke deleter once");
        AssertEx.True(deleter.DeletedCalls[0].Recycled, "Custom rule MoveToRecycleBin=true must pass recycled=true");
        AssertEx.False(File.Exists(ruleFile), "Custom rule file should be deleted");
    }

    private static async Task TestCancellation()
    {
        using var sandbox = new TempSandbox();
        string file = sandbox.WriteFile("stay.tmp", 128);
        var category = MakeFolderCategory("cancel", sandbox.RootPath, respectAge: false);
        var engine = new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var summary = await engine.CleanAllAsync(
            [category],
            new CleanerOptions(TimeSpan.Zero, Array.Empty<string>()),
            cancellationToken: cts.Token);

        AssertEx.True(summary.WasCancelled, "Cancelled token should mark summary cancelled");
        AssertEx.True(File.Exists(file), "Cancelled run must not delete file");
    }

    private static async Task TestDiskAnalyzer()
    {
        using var sandbox = new TempSandbox();
        sandbox.WriteFile(Path.Combine("Alpha", "big.iso"), 5000);
        sandbox.WriteFile(Path.Combine("Alpha", "small.zip"), 500);
        sandbox.WriteFile(Path.Combine("Beta", "video.mp4"), 2500);

        var deleter = new FakeFileDeletionService();
        var analyzer = new DiskAnalyzerService(deleter);

        var result = await analyzer.AnalyzeAsync(sandbox.RootPath, maxTopFiles: 2, maxTopFolders: 5);
        AssertEx.Equal(8000L, result.TotalBytes, "Total analyzed bytes");
        AssertEx.Equal(3, result.TotalFiles, "Total analyzed files");
        AssertEx.Equal("Alpha", result.TopFolders[0].Name, "Alpha should be top folder");
        AssertEx.Equal(2, result.TopFiles.Count, "Should cap TopFiles at 2");
        AssertEx.Equal("big.iso", result.TopFiles[0].Name, "Largest file first");
        AssertEx.Equal(DiskFileCategory.DiskImages, result.TopFiles[0].FileCategory, ".iso classification");
        AssertEx.Equal(DiskFileCategory.Media, result.TopFiles[1].FileCategory, ".mp4 classification");

        AssertEx.True(analyzer.TryDeleteFile(result.TopFiles[0].FullPath, moveToRecycleBin: true), "Should delete file in sandbox");
        AssertEx.False(analyzer.TryDeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe"), false), "Must block System32 deletion");
    }

    private static async Task TestDuplicateFinder()
    {
        using var sandbox = new TempSandbox();
        byte[] sharedHeader = Enumerable.Repeat((byte)0x41, 5000).ToArray();
        byte[] dupPayload = (byte[])sharedHeader.Clone();
        byte[] diffTailPayload = (byte[])sharedHeader.Clone();
        diffTailPayload[4999] = 0x99; // Identical size & 4KB header, differs only in tail byte!

        string orig = sandbox.WriteBytes("a_orig.bin", dupPayload, DateTimeOffset.UtcNow.AddDays(-5));
        string copy = sandbox.WriteBytes(Path.Combine("sub", "b_copy.bin"), dupPayload, DateTimeOffset.UtcNow.AddDays(-1));
        string uniqueTail = sandbox.WriteBytes("c_diff_tail.bin", diffTailPayload, DateTimeOffset.UtcNow);
        sandbox.WriteFile("tiny.bin", 10); // Below minSizeBytes

        var deleter = new FakeFileDeletionService();
        var finder = new DuplicateFinderService(deleter);

        var result = await finder.ScanAsync(sandbox.RootPath, minimumFileSizeBytes: 100);
        AssertEx.Equal(1, result.Groups.Count, "Only true SHA-256 matches should form duplicate group");
        AssertEx.Equal(2, result.Groups[0].Files.Count, "Group should have 2 matching files");
        AssertEx.Equal(5000L, result.TotalReclaimableBytes, "Reclaimable bytes = (2-1) * 5000");
        AssertEx.Equal(orig, result.Groups[0].Files[0].FullPath, "Oldest file should be first");

        var (deleted, skipped, freed) = finder.DeleteFiles([copy], moveToRecycleBin: true);
        AssertEx.Equal(1, deleted, "1 duplicate deleted");
        AssertEx.Equal(0, skipped, "0 skipped");
        AssertEx.Equal(5000L, freed, "5000 bytes freed");
        AssertEx.True(File.Exists(orig) && File.Exists(uniqueTail), "Original and tail-different file must remain");
        AssertEx.False(File.Exists(copy), "Copy must be removed");
    }

    private static Task TestStartupCodec()
    {
        AssertEx.True(StartupApprovedCodec.IsEnabled(null), "Null StartupApproved means enabled");
        AssertEx.True(StartupApprovedCodec.IsEnabled([0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]), "0x02 means enabled");
        AssertEx.True(StartupApprovedCodec.IsEnabled([0x06, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]), "0x06 means enabled");
        AssertEx.False(StartupApprovedCodec.IsEnabled([0x03, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8]), "0x03 means disabled");
        AssertEx.False(StartupApprovedCodec.IsEnabled([0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]), "0x01 means disabled");

        byte[] disabled = StartupApprovedCodec.EncodeState(false);
        AssertEx.Equal(12, disabled.Length, "Encoded length must be 12");
        AssertEx.False(StartupApprovedCodec.IsEnabled(disabled), "Encoded disabled state must decode disabled");

        byte[] enabled = StartupApprovedCodec.EncodeState(true);
        AssertEx.True(StartupApprovedCodec.IsEnabled(enabled), "Encoded enabled state must decode enabled");

        AssertEx.Equal(@"C:\Program Files\App\my.exe", StartupManagerViewModel.ExtractExecutablePath("\"C:\\Program Files\\App\\my.exe\" --minimized"), "Quoted exe path extraction");
        return Task.CompletedTask;
    }

    private static async Task TestCliRunner()
    {
        using var sandbox = new TempSandbox();
        var settingsSvc = new SettingsService(sandbox.RootPath);
        string targetFolder = Path.Combine(sandbox.RootPath, "cli_logs");
        Directory.CreateDirectory(targetFolder);
        string logFile = Path.Combine(targetFolder, "old.log");
        File.WriteAllBytes(logFile, new byte[256]);

        settingsSvc.Save(new AppSettings
        {
            Language = "en",
            MinimumFileAgeHours = 0,
            CustomRules = [new CustomCleanupRule { Id = "c1", Name = "CLI Logs", FolderPath = targetFolder, SearchPattern = "*.log", RespectMinimumAge = false, MoveToRecycleBin = false }],
            CategorySelection = new Dictionary<string, bool> { ["custom_c1"] = true }
        });

        var historySvc = new HistoryService(sandbox.RootPath);
        var cli = new CliRunner(
            new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher(), new FakeFileDeletionService()),
            settingsSvc,
            historySvc,
            () => Array.Empty<CleanupCategory>());

        using var swHelp = new StringWriter();
        int helpCode = await cli.RunAsync(["--help"], swHelp);
        AssertEx.Equal(0, helpCode, "--help exit code");
        AssertEx.True(swHelp.ToString().Contains("--clean-safe"), "--help prints options");

        using var swAnalyze = new StringWriter();
        int analyzeCode = await cli.RunAsync(["--analyze"], swAnalyze);
        AssertEx.Equal(0, analyzeCode, "--analyze exit code");
        AssertEx.True(File.Exists(logFile), "--analyze must not delete files");

        using var swClean = new StringWriter();
        int cleanCode = await cli.RunAsync(["--clean-selected"], swClean);
        AssertEx.Equal(0, cleanCode, "--clean-selected exit code");
        AssertEx.False(File.Exists(logFile), "--clean-selected must remove custom rule file");
        AssertEx.Equal(2, historySvc.Load().Count, "CLI should record 2 history entries");
    }

    private static Task TestPersistence()
    {
        using var sandbox = new TempSandbox();
        var settingsSvc = new SettingsService(sandbox.RootPath);

        var settings = new AppSettings
        {
            Language = "en",
            Theme = "Light",
            MinimumFileAgeHours = 1,
            ConfirmBeforeClean = false,
            AutoAnalyzeOnStartup = true,
            ScheduledCleanFrequency = ScheduleFrequency.Weekly,
            TotalLifetimeFreedBytes = 987654,
            Exclusions = [@"C:\Temp\keep", "*.bak"],
            CustomRules = [new CustomCleanupRule { Name = "Logs", FolderPath = @"C:\Temp\Logs", SearchPattern = "*.log" }],
            CategorySelection = new Dictionary<string, bool> { ["user_temp"] = false }
        };
        settingsSvc.Save(settings);

        var loaded = settingsSvc.Load();
        AssertEx.Equal("en", loaded.Language, "Language should persist");
        AssertEx.Equal("Light", loaded.Theme, "Theme should persist");
        AssertEx.Equal(1, loaded.MinimumFileAgeHours, "MinAge should persist");
        AssertEx.False(loaded.ConfirmBeforeClean, "Confirm flag should persist");
        AssertEx.True(loaded.AutoAnalyzeOnStartup, "AutoAnalyze should persist");
        AssertEx.Equal(ScheduleFrequency.Weekly, loaded.ScheduledCleanFrequency, "Schedule should persist");
        AssertEx.Equal(1, loaded.CustomRules.Count, "CustomRules should persist");

        var historySvc = new HistoryService(sandbox.RootPath);
        for (int i = 0; i < 105; i++)
        {
            historySvc.Append(new CleanupHistoryEntry { TimestampUtc = DateTimeOffset.UtcNow, BytesAffected = i });
        }

        var entries = historySvc.Load();
        AssertEx.Equal(100, entries.Count, "History should cap at 100 entries");
        AssertEx.Equal(104L, entries[0].BytesAffected, "Newest entry should be first");

        historySvc.Clear();
        AssertEx.Equal(0, historySvc.Load().Count, "Clear should empty history");
        return Task.CompletedTask;
    }

    private static Task TestLocalizationParity()
    {
        var ru = LocalizationCatalog.GetDictionary("ru");
        var en = LocalizationCatalog.GetDictionary("en");

        foreach (string key in ru.Keys)
        {
            AssertEx.True(en.ContainsKey(key), $"EN missing key '{key}'");
            AssertEx.False(string.IsNullOrWhiteSpace(en[key]), $"EN key '{key}' is blank");
        }

        foreach (string key in en.Keys)
        {
            AssertEx.True(ru.ContainsKey(key), $"RU missing key '{key}'");
            AssertEx.False(string.IsNullOrWhiteSpace(ru[key]), $"RU key '{key}' is blank");
        }

        foreach (var cat in DefaultCatalog.CreateDefaultCategories())
        {
            AssertEx.True(ru.ContainsKey(cat.TitleKey) && en.ContainsKey(cat.TitleKey), $"Missing title key {cat.TitleKey}");
            AssertEx.True(ru.ContainsKey(cat.DescriptionKey) && en.ContainsKey(cat.DescriptionKey), $"Missing desc key {cat.DescriptionKey}");
            AssertEx.True(ru.ContainsKey(cat.SafetyNoteKey) && en.ContainsKey(cat.SafetyNoteKey), $"Missing safety key {cat.SafetyNoteKey}");
        }

        return Task.CompletedTask;
    }

    private static Task TestByteFormatter()
    {
        AssertEx.Equal("0 Б", ByteFormatter.Format(0, "ru"), "0 bytes RU");
        AssertEx.Equal("0 B", ByteFormatter.Format(0, "en"), "0 bytes EN");
        AssertEx.Equal("1.5 KB", ByteFormatter.Format(1536, "en"), "1.5 KB EN");
        AssertEx.True(ByteFormatter.Format(1024 * 1024, "ru").EndsWith("МБ"), "1 MB RU");
        return Task.CompletedTask;
    }

    private static Task TestWpfUiSmoke()
    {
        using var sandbox = new TempSandbox();
        var app = new CleanPulse.App();
        app.InitializeComponent();

        var ui = new FakeUiService();
        var proc = new FakeProcessDetector();
        proc.Running.Add("chrome");

        var vm = new MainViewModel(
            new CleanerEngine(new FakeRecycleBin(), new FakeDnsFlusher(), new FakeFileDeletionService()),
            new SettingsService(sandbox.RootPath),
            new HistoryService(sandbox.RootPath),
            new DriveInfoService(),
            new FakePrivilegeService(),
            ui,
            proc,
            new FakeFileDeletionService(),
            new FakeDismService(),
            new FakeScheduledTaskService(),
            new FakeStartupManagerService(),
            new DiskAnalyzerService(new FakeFileDeletionService()),
            new DuplicateFinderService(new FakeFileDeletionService()));

        var window = new MainWindow { DataContext = vm };

        // Exercise every single NavSection layout + bindings
        foreach (NavSection section in Enum.GetValues<NavSection>())
        {
            vm.ActiveSection = section;
            window.Measure(new System.Windows.Size(1260, 800));
            window.Arrange(new System.Windows.Rect(0, 0, 1260, 800));
            window.UpdateLayout();
        }

        // Exercise live language, theme, schedule, and custom rule addition
        vm.Language = "en";
        AssertEx.Equal("en", Localization.Loc.Instance.Language, "Loc language should switch to en");
        AssertEx.Equal(24, vm.MinimumFileAgeHours, "MinimumFileAgeHours must survive language reload");

        AssertEx.Equal("Light", vm.Theme, "Default theme should be silver-white Liquid Glass (Light)");
        vm.Theme = "Dark";
        AssertEx.Equal("Dark", ui.LastAppliedTheme, "Theme should switch to Dark");
        window.UpdateLayout();
        vm.Theme = "Light";
        AssertEx.Equal("Light", ui.LastAppliedTheme, "Theme should switch back to Light");
        window.UpdateLayout();

        vm.ScheduledCleanFrequency = ScheduleFrequency.Weekly;
        AssertEx.Equal(ScheduleFrequency.Weekly, vm.ScheduledCleanFrequency, "Schedule should update");

        // Protected custom rule blocked
        vm.NewRulePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        vm.AddCustomRuleCommand.Execute(null);
        AssertEx.True(vm.HasRuleValidationError, "Protected Desktop folder must trigger validation error");

        // Safe custom rule accepted
        string customDir = Path.Combine(sandbox.RootPath, "my_temp_logs");
        Directory.CreateDirectory(customDir);
        vm.NewRuleName = "Sandbox Logs";
        vm.NewRulePath = customDir;
        vm.NewRulePattern = "*.log";
        vm.AddCustomRuleCommand.Execute(null);
        AssertEx.False(vm.HasRuleValidationError, "Safe custom rule should succeed");
        AssertEx.True(vm.Categories.Any(c => c.IsCustomRule && c.Title == "Sandbox Logs"), "Custom rule must appear in Cleaner categories");

        window.Close();
        app.Shutdown();
        return Task.CompletedTask;
    }

    private static CleanupCategory MakeFolderCategory(string id, string path, bool respectAge, bool deleteEmptySubdirs = false) =>
        new(
            id,
            CleanupGroup.SystemAndTemp,
            "cat_user_temp_title",
            "cat_user_temp_desc",
            "cat_user_temp_safety",
            RequiresAdmin: false,
            IsRecommendedDefault: true,
            IsCaution: false,
            Targets: [CleanupTarget.Directory(path, "*", recursive: true, deleteEmptySubdirectories: deleteEmptySubdirs, respectMinimumAge: respectAge)]);

    private sealed class TempSandbox : IDisposable
    {
        public string RootPath { get; }

        public TempSandbox()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"CleanPulseTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);
        }

        public string WriteFile(string relativePath, int sizeBytes, DateTimeOffset? modifiedUtc = null) =>
            WriteBytes(relativePath, new byte[sizeBytes], modifiedUtc);

        public string WriteBytes(string relativePath, byte[] bytes, DateTimeOffset? modifiedUtc = null)
        {
            string full = Path.Combine(RootPath, relativePath);
            string? dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(full, bytes);
            if (modifiedUtc.HasValue)
            {
                File.SetCreationTimeUtc(full, modifiedUtc.Value.UtcDateTime);
                File.SetLastWriteTimeUtc(full, modifiedUtc.Value.UtcDateTime);
            }

            return full;
        }

        public void Dispose()
        {
            try
            {
                if (!Directory.Exists(RootPath))
                {
                    return;
                }

                foreach (string file in Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(RootPath, recursive: true);
            }
            catch
            {
                // Ignore locked test file cleanup edges on dispose.
            }
        }
    }
}
