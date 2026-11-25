using CleanPulse.Core.Formatting;
using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Headless CLI runner enabling scheduled background cleanup and terminal automation.
/// </summary>
public sealed class CliRunner
{
    private readonly CleanerEngine _engine;
    private readonly SettingsService _settingsService;
    private readonly HistoryService _historyService;
    private readonly Func<IReadOnlyList<CleanupCategory>> _catalogProvider;

    public CliRunner(
        CleanerEngine engine,
        SettingsService settingsService,
        HistoryService historyService,
        Func<IReadOnlyList<CleanupCategory>>? catalogProvider = null)
    {
        _engine = engine;
        _settingsService = settingsService;
        _historyService = historyService;
        _catalogProvider = catalogProvider ?? DefaultCatalog.CreateDefaultCategories;
    }

    public static bool IsCliInvocation(string[] args) =>
        args.Any(a => a.StartsWith("--", StringComparison.Ordinal) ||
                      string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(a, "/?", StringComparison.Ordinal));

    public async Task<int> RunAsync(string[] args, TextWriter? output = null, CancellationToken cancellationToken = default)
    {
        bool silent = args.Any(a => string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase));
        bool help = args.Any(a => a is "--help" or "-h" or "/?");
        bool analyze = args.Any(a => string.Equals(a, "--analyze", StringComparison.OrdinalIgnoreCase));
        bool cleanSafe = args.Any(a => string.Equals(a, "--clean-safe", StringComparison.OrdinalIgnoreCase));
        bool cleanSelected = args.Any(a => string.Equals(a, "--clean-selected", StringComparison.OrdinalIgnoreCase));

        TextWriter writer = silent ? TextWriter.Null : (output ?? Console.Out);

        if (help || (!analyze && !cleanSafe && !cleanSelected))
        {
            writer.WriteLine("CleanPulse CLI");
            writer.WriteLine("  --analyze         Scan enabled categories without deleting");
            writer.WriteLine("  --clean-safe      Clean recommended safe categories (ideal for Task Scheduler)");
            writer.WriteLine("  --clean-selected  Clean currently selected categories + custom rules");
            writer.WriteLine("  --silent          Suppress console output");
            writer.WriteLine("  --help            Show this help message");
            return help ? 0 : 1;
        }

        var settings = _settingsService.Load();
        var builtIn = _catalogProvider();
        var custom = DefaultCatalog.FromCustomRules(settings.CustomRules);
        var allCategories = builtIn.Concat(custom).ToList();

        List<CleanupCategory> targetCategories;
        if (cleanSafe)
        {
            targetCategories = builtIn.Where(c => c.IsRecommendedDefault).ToList();
        }
        else
        {
            targetCategories = allCategories
                .Where(c => settings.CategorySelection.TryGetValue(c.Id, out bool enabled)
                    ? enabled
                    : (c.IsCustomRule || c.IsRecommendedDefault))
                .ToList();
        }

        var options = new CleanerOptions(
            MinimumFileAge: TimeSpan.FromHours(settings.MinimumFileAgeHours),
            Exclusions: settings.Exclusions,
            MaxPreviewItemsPerCategory: 100);

        CleanupSummary summary = analyze
            ? await _engine.AnalyzeAllAsync(targetCategories, options, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await _engine.CleanAllAsync(targetCategories, options, cancellationToken: cancellationToken).ConfigureAwait(false);

        long bytes = summary.WasDryRun ? summary.TotalBytes : summary.CleanedBytes;
        int files = summary.WasDryRun ? summary.TotalFiles : summary.CleanedFiles;

        if (!summary.WasDryRun && summary.CleanedBytes > 0)
        {
            settings.TotalLifetimeFreedBytes += summary.CleanedBytes;
            _settingsService.Save(settings);
        }

        _historyService.Append(new CleanupHistoryEntry
        {
            TimestampUtc = summary.CompletedAtUtc,
            ActionType = summary.WasDryRun ? "Analyze" : "Clean",
            BytesAffected = bytes,
            FilesAffected = files,
            SkippedCount = summary.SkippedCount,
            DurationSeconds = Math.Round(summary.Duration.TotalSeconds, 2),
            CategoriesCount = summary.CategoryResults.Count,
            SummaryText = $"CLI ({targetCategories.Count} cat.)"
        });

        string sizeText = ByteFormatter.Format(bytes, settings.Language);
        writer.WriteLine(summary.WasDryRun
            ? $"[Analyze] Found {sizeText} across {files} files in {summary.Duration.TotalSeconds:0.0}s"
            : $"[Clean] Freed {sizeText}, removed {files} files, skipped {summary.SkippedCount} in {summary.Duration.TotalSeconds:0.0}s");

        return summary.WasCancelled ? 2 : 0;
    }
}
