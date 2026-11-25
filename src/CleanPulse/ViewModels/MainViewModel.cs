using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using CleanPulse.Core.Formatting;
using CleanPulse.Core.Models;
using CleanPulse.Core.Services;
using CleanPulse.Localization;
using CleanPulse.Services;

namespace CleanPulse.ViewModels;

public enum NavSection
{
    Cleaner,
    Analyzer,
    Duplicates,
    Startup,
    Rules,
    Logs,
    Settings
}

public sealed record DriveTileViewModel(
    string DriveName,
    string VolumeLabel,
    string FreeSummaryText,
    double UsedPercentage);

public sealed record HistoryRowViewModel(
    string TimestampText,
    string ActionLabel,
    bool IsCleanAction,
    string SizeText,
    string FilesText,
    string SkippedText,
    string DurationText,
    string SummaryText);

public sealed record AgeOption(int Hours, string Label);
public sealed record ScheduleOption(ScheduleFrequency Frequency, string Label);

public sealed class MainViewModel : ObservableObject
{
    private readonly CleanerEngine _engine;
    private readonly SettingsService _settingsService;
    private readonly HistoryService _historyService;
    private readonly DriveInfoService _driveService;
    private readonly IPrivilegeService _privilegeService;
    private readonly IProcessDetector _processDetector;
    private readonly IFileDeletionService _fileDeleter;
    private readonly IDismService _dismService;
    private readonly IScheduledTaskService _scheduledTaskService;
    private readonly IUiPlatformService _uiService;
    private readonly AppSettings _settings;

    private CancellationTokenSource? _activeCts;
    private NavSection _activeSection = NavSection.Cleaner;
    private CategoryItemViewModel? _selectedCategory;
    private bool _isBusy;
    private bool _isDismBusy;
    private double _progressPercent;
    private string _currentScanPath = string.Empty;
    private string _heroTitle = string.Empty;
    private string _heroSubtitle = string.Empty;
    private string _dismStatusText = string.Empty;
    private string _newExclusionInput = string.Empty;
    private string _newRuleName = string.Empty;
    private string _newRulePath = string.Empty;
    private string _newRulePattern = "*";
    private bool _newRuleRecursive = true;
    private bool _newRuleRespectAge = true;
    private bool _newRuleToRecycle = true;
    private string _ruleValidationError = string.Empty;
    private CleanupSummary? _lastSummary;
    private List<CleanupHistoryEntry> _rawHistory = new();

    public MainViewModel(
        CleanerEngine engine,
        SettingsService settingsService,
        HistoryService historyService,
        DriveInfoService driveService,
        IPrivilegeService privilegeService,
        IUiPlatformService uiService,
        IProcessDetector? processDetector = null,
        IFileDeletionService? fileDeleter = null,
        IDismService? dismService = null,
        IScheduledTaskService? scheduledTaskService = null,
        IStartupManagerService? startupService = null,
        DiskAnalyzerService? diskAnalyzerService = null,
        DuplicateFinderService? duplicateFinderService = null)
    {
        _engine = engine;
        _settingsService = settingsService;
        _historyService = historyService;
        _driveService = driveService;
        _privilegeService = privilegeService;
        _uiService = uiService;
        _processDetector = processDetector ?? new WindowsProcessDetector();
        _fileDeleter = fileDeleter ?? new WindowsFileDeletionService();
        _dismService = dismService ?? new WindowsDismService();
        _scheduledTaskService = scheduledTaskService ?? new WindowsScheduledTaskService();

        _settings = _settingsService.Load();
        IsAdministrator = _privilegeService.IsAdministrator();

        Loc.Instance.Language = _settings.Language;
        _uiService.ApplyTheme(_settings.Theme);

        DiskAnalyzer = new DiskAnalyzerViewModel(
            diskAnalyzerService ?? new DiskAnalyzerService(_fileDeleter),
            _driveService,
            _uiService);

        DuplicateFinder = new DuplicateFinderViewModel(
            duplicateFinderService ?? new DuplicateFinderService(_fileDeleter),
            _uiService);

        StartupManager = new StartupManagerViewModel(
            startupService ?? new WindowsStartupManagerService(),
            _uiService,
            IsAdministrator);

        Categories = new ObservableCollection<CategoryItemViewModel>();
        CategoriesView = CollectionViewSource.GetDefaultView(Categories);
        CategoriesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CategoryItemViewModel.GroupTitle)));

        Exclusions = new ObservableCollection<string>(_settings.Exclusions);
        CustomRules = new ObservableCollection<CustomCleanupRule>(_settings.CustomRules);
        Drives = new ObservableCollection<DriveTileViewModel>();
        HistoryRows = new ObservableCollection<HistoryRowViewModel>();
        AgeOptions = new ObservableCollection<AgeOption>();
        ScheduleOptions = new ObservableCollection<ScheduleOption>();

        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => !IsBusy && Categories.Any(c => c.IsChecked));
        CleanCommand = new AsyncRelayCommand(CleanAsync, () => !IsBusy && Categories.Any(c => c.IsChecked));
        CancelCommand = new RelayCommand(CancelOperation, () => IsBusy);
        SelectRecommendedCommand = new RelayCommand(() => ApplySelectionPreset(c => c.Model.IsRecommendedDefault), () => !IsBusy);
        SelectAllCommand = new RelayCommand(() => ApplySelectionPreset(_ => true), () => !IsBusy);
        SelectNoneCommand = new RelayCommand(() => ApplySelectionPreset(_ => false), () => !IsBusy);
        RestartAsAdminCommand = new RelayCommand(RestartAsAdmin, () => !IsAdministrator && !IsBusy);
        RunDismCommand = new AsyncRelayCommand(RunDismAsync, () => !IsDismBusy && !IsBusy);

        AddExclusionCommand = new RelayCommand(AddExclusion, () => !string.IsNullOrWhiteSpace(NewExclusionInput));
        BrowseExclusionFolderCommand = new RelayCommand(BrowseExclusionFolder);
        RemoveExclusionCommand = new RelayCommand<string>(RemoveExclusion);

        AddCustomRuleCommand = new RelayCommand(AddCustomRule, () => !string.IsNullOrWhiteSpace(NewRulePath));
        BrowseCustomRuleFolderCommand = new RelayCommand(BrowseCustomRuleFolder);
        RemoveCustomRuleCommand = new RelayCommand<CustomCleanupRule>(RemoveCustomRule);

        ExcludeItemCommand = new RelayCommand<ScannedItemViewModel>(ExcludeScannedItem);
        OpenItemFolderCommand = new RelayCommand<ScannedItemViewModel>(OpenItemFolder);
        RecycleScannedItemCommand = new RelayCommand<ScannedItemViewModel>(RecycleScannedItem);
        ClearLogsCommand = new RelayCommand(ClearLogs, () => HistoryRows.Count > 0);
        ExportLogsCommand = new RelayCommand(ExportLogs, () => HistoryRows.Count > 0);

        RebuildCategories();
        RefreshDrives();
        ReloadHistory();
        RefreshAllLocalizedText();
    }

    public bool IsAdministrator { get; }
    public bool IsStandardUser => !IsAdministrator;

    public DiskAnalyzerViewModel DiskAnalyzer { get; }
    public DuplicateFinderViewModel DuplicateFinder { get; }
    public StartupManagerViewModel StartupManager { get; }

    public ObservableCollection<CategoryItemViewModel> Categories { get; }
    public ICollectionView CategoriesView { get; }
    public ObservableCollection<string> Exclusions { get; }
    public ObservableCollection<CustomCleanupRule> CustomRules { get; }
    public ObservableCollection<DriveTileViewModel> Drives { get; }
    public ObservableCollection<HistoryRowViewModel> HistoryRows { get; }
    public ObservableCollection<AgeOption> AgeOptions { get; }
    public ObservableCollection<ScheduleOption> ScheduleOptions { get; }

    public AsyncRelayCommand AnalyzeCommand { get; }
    public AsyncRelayCommand CleanCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SelectRecommendedCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand SelectNoneCommand { get; }
    public RelayCommand RestartAsAdminCommand { get; }
    public AsyncRelayCommand RunDismCommand { get; }
    public RelayCommand AddExclusionCommand { get; }
    public RelayCommand BrowseExclusionFolderCommand { get; }
    public RelayCommand<string> RemoveExclusionCommand { get; }
    public RelayCommand AddCustomRuleCommand { get; }
    public RelayCommand BrowseCustomRuleFolderCommand { get; }
    public RelayCommand<CustomCleanupRule> RemoveCustomRuleCommand { get; }
    public RelayCommand<ScannedItemViewModel> ExcludeItemCommand { get; }
    public RelayCommand<ScannedItemViewModel> OpenItemFolderCommand { get; }
    public RelayCommand<ScannedItemViewModel> RecycleScannedItemCommand { get; }
    public RelayCommand ClearLogsCommand { get; }
    public RelayCommand ExportLogsCommand { get; }

    public NavSection ActiveSection
    {
        get => _activeSection;
        set => SetProperty(ref _activeSection, value);
    }

    public CategoryItemViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsDismBusy
    {
        get => _isDismBusy;
        private set
        {
            if (SetProperty(ref _isDismBusy, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => SetProperty(ref _progressPercent, value);
    }

    public string CurrentScanPath
    {
        get => _currentScanPath;
        private set => SetProperty(ref _currentScanPath, value);
    }

    public string HeroTitle
    {
        get => _heroTitle;
        private set => SetProperty(ref _heroTitle, value);
    }

    public string HeroSubtitle
    {
        get => _heroSubtitle;
        private set => SetProperty(ref _heroSubtitle, value);
    }

    public string DismStatusText
    {
        get => _dismStatusText;
        private set => SetProperty(ref _dismStatusText, value);
    }

    public bool HasDismStatus => !string.IsNullOrWhiteSpace(DismStatusText);

    public string NewExclusionInput
    {
        get => _newExclusionInput;
        set
        {
            if (SetProperty(ref _newExclusionInput, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewRuleName
    {
        get => _newRuleName;
        set => SetProperty(ref _newRuleName, value);
    }

    public string NewRulePath
    {
        get => _newRulePath;
        set
        {
            if (SetProperty(ref _newRulePath, value))
            {
                RuleValidationError = string.Empty;
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewRulePattern
    {
        get => _newRulePattern;
        set => SetProperty(ref _newRulePattern, value);
    }

    public bool NewRuleRecursive
    {
        get => _newRuleRecursive;
        set => SetProperty(ref _newRuleRecursive, value);
    }

    public bool NewRuleRespectAge
    {
        get => _newRuleRespectAge;
        set => SetProperty(ref _newRuleRespectAge, value);
    }

    public bool NewRuleToRecycle
    {
        get => _newRuleToRecycle;
        set => SetProperty(ref _newRuleToRecycle, value);
    }

    public string RuleValidationError
    {
        get => _ruleValidationError;
        private set
        {
            if (SetProperty(ref _ruleValidationError, value))
            {
                OnPropertyChanged(nameof(HasRuleValidationError));
            }
        }
    }

    public bool HasRuleValidationError => !string.IsNullOrWhiteSpace(RuleValidationError);

    public string Language
    {
        get => _settings.Language;
        set
        {
            string normalized = string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
            if (_settings.Language == normalized)
            {
                return;
            }

            _settings.Language = normalized;
            _settingsService.Save(_settings);
            Loc.Instance.Language = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRussian));
            OnPropertyChanged(nameof(IsEnglish));
            RefreshAllLocalizedText();
        }
    }

    public bool IsRussian
    {
        get => _settings.Language == "ru";
        set { if (value) Language = "ru"; }
    }

    public bool IsEnglish
    {
        get => _settings.Language == "en";
        set { if (value) Language = "en"; }
    }

    public string Theme
    {
        get => _settings.Theme;
        set
        {
            string normalized = string.Equals(value, "Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
            if (_settings.Theme == normalized)
            {
                return;
            }

            _settings.Theme = normalized;
            _settingsService.Save(_settings);
            _uiService.ApplyTheme(normalized);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
        }
    }

    public bool IsDarkTheme
    {
        get => _settings.Theme == "Dark";
        set { if (value) Theme = "Dark"; }
    }

    public bool IsLightTheme
    {
        get => _settings.Theme == "Light";
        set { if (value) Theme = "Light"; }
    }

    public int MinimumFileAgeHours
    {
        get => _settings.MinimumFileAgeHours;
        set
        {
            if (_settings.MinimumFileAgeHours != value)
            {
                _settings.MinimumFileAgeHours = value;
                _settingsService.Save(_settings);
                OnPropertyChanged();
            }
        }
    }

    public bool ConfirmBeforeClean
    {
        get => _settings.ConfirmBeforeClean;
        set
        {
            if (_settings.ConfirmBeforeClean != value)
            {
                _settings.ConfirmBeforeClean = value;
                _settingsService.Save(_settings);
                OnPropertyChanged();
            }
        }
    }

    public bool AutoAnalyzeOnStartup
    {
        get => _settings.AutoAnalyzeOnStartup;
        set
        {
            if (_settings.AutoAnalyzeOnStartup != value)
            {
                _settings.AutoAnalyzeOnStartup = value;
                _settingsService.Save(_settings);
                OnPropertyChanged();
            }
        }
    }

    public ScheduleFrequency ScheduledCleanFrequency
    {
        get => _settings.ScheduledCleanFrequency;
        set
        {
            if (_settings.ScheduledCleanFrequency == value)
            {
                return;
            }

            string exe = Environment.ProcessPath ?? "CleanPulse.exe";
            if (_scheduledTaskService.ApplySchedule(value, exe, out string? error))
            {
                _settings.ScheduledCleanFrequency = value;
                _settingsService.Save(_settings);
                OnPropertyChanged();
            }
            else
            {
                _uiService.ShowInfo(
                    Loc.Instance["settings_schedule"],
                    Loc.Instance.Format("sched_error", error ?? "Unknown error"));
                OnPropertyChanged();
            }
        }
    }

    public string PrivilegeStatusText => IsAdministrator
        ? Loc.Instance["privilege_admin"]
        : Loc.Instance["privilege_user"];

    public string LogsSubtitleText => Loc.Instance.Format(
        "logs_subtitle",
        ByteFormatter.Format(_settings.TotalLifetimeFreedBytes, _settings.Language));

    public DriveTileViewModel? PrimaryDrive => Drives.FirstOrDefault();
    public bool HasNoExclusions => Exclusions.Count == 0;
    public bool HasNoCustomRules => CustomRules.Count == 0;
    public bool HasNoHistory => HistoryRows.Count == 0;

    public async Task InitializeAsync()
    {
        if (_settings.AutoAnalyzeOnStartup && !IsBusy && Categories.Any(c => c.IsChecked))
        {
            await AnalyzeAsync();
        }
    }

    private void RebuildCategories()
    {
        string? prevSelectedId = SelectedCategory?.Id;
        var existingResults = Categories.ToDictionary(c => c.Id, c => c.Result, StringComparer.OrdinalIgnoreCase);

        Categories.Clear();
        var all = DefaultCatalog.CreateDefaultCategories()
            .Concat(DefaultCatalog.FromCustomRules(_settings.CustomRules));

        foreach (var cat in all)
        {
            bool isChecked = _settings.CategorySelection.TryGetValue(cat.Id, out bool saved)
                ? saved
                : (cat.IsCustomRule || cat.IsRecommendedDefault);

            var vm = new CategoryItemViewModel(cat, isChecked, IsAdministrator, _processDetector, OnCategoryCheckedChanged);
            if (existingResults.TryGetValue(cat.Id, out var prevRes))
            {
                vm.Result = prevRes;
            }

            Categories.Add(vm);
        }

        SelectedCategory = Categories.FirstOrDefault(c => string.Equals(c.Id, prevSelectedId, StringComparison.OrdinalIgnoreCase))
            ?? Categories.FirstOrDefault();

        CategoriesView.Refresh();
        RelayCommand.RaiseCanExecuteChanged();
    }

    private void OnCategoryCheckedChanged(CategoryItemViewModel vm)
    {
        _settings.CategorySelection[vm.Id] = vm.IsChecked;
        _settingsService.Save(_settings);
        RelayCommand.RaiseCanExecuteChanged();
    }

    private void ApplySelectionPreset(Func<CategoryItemViewModel, bool> predicate)
    {
        foreach (var cat in Categories)
        {
            bool target = predicate(cat);
            cat.SetCheckedSilently(target);
            _settings.CategorySelection[cat.Id] = target;
        }

        _settingsService.Save(_settings);
        RelayCommand.RaiseCanExecuteChanged();
    }

    private async Task AnalyzeAsync()
    {
        var selected = Categories.Where(c => c.IsChecked).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        _activeCts?.Dispose();
        _activeCts = new CancellationTokenSource();
        var token = _activeCts.Token;

        IsBusy = true;
        ProgressPercent = 5;
        HeroTitle = Loc.Instance["hero_scanning_title"];
        HeroSubtitle = string.Empty;

        foreach (var cat in selected)
        {
            cat.RefreshProcessWarning();
            cat.Result = CategoryScanResult.Empty(cat.Id, CategoryStatus.Scanning);
        }

        var options = BuildCleanerOptions();
        var progress = CreateProgressReporter();

        try
        {
            var models = selected.Select(c => c.Model).ToList();
            var summary = await _engine.AnalyzeAllAsync(models, options, progress, token);
            ApplyBatchResults(summary);

            if (!summary.WasCancelled)
            {
                RecordHistory("Analyze", summary);
            }
        }
        finally
        {
            ProgressPercent = 100;
            CurrentScanPath = string.Empty;
            IsBusy = false;
        }
    }

    private async Task CleanAsync()
    {
        var selected = Categories.Where(c => c.IsChecked).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        if (_settings.ConfirmBeforeClean)
        {
            long estimatedBytes = selected
                .Where(c => c.Result.Status == CategoryStatus.Analyzed)
                .Sum(c => c.Result.TotalBytes);

            string sizeDisplay = estimatedBytes > 0
                ? ByteFormatter.Format(estimatedBytes, _settings.Language)
                : "?";

            string msg = Loc.Instance.Format("confirm_message", selected.Count, sizeDisplay);
            if (!_uiService.Confirm(Loc.Instance["confirm_title"], msg))
            {
                return;
            }
        }

        _activeCts?.Dispose();
        _activeCts = new CancellationTokenSource();
        var token = _activeCts.Token;

        IsBusy = true;
        ProgressPercent = 5;
        HeroTitle = Loc.Instance["hero_cleaning_title"];
        HeroSubtitle = string.Empty;

        foreach (var cat in selected)
        {
            cat.RefreshProcessWarning();
            cat.Result = CategoryScanResult.Empty(cat.Id, CategoryStatus.Cleaning);
        }

        var options = BuildCleanerOptions();
        var progress = CreateProgressReporter();

        try
        {
            var models = selected.Select(c => c.Model).ToList();
            var summary = await _engine.CleanAllAsync(models, options, progress, token);
            ApplyBatchResults(summary);

            if (summary.CleanedBytes > 0)
            {
                _settings.TotalLifetimeFreedBytes += summary.CleanedBytes;
                _settingsService.Save(_settings);
                OnPropertyChanged(nameof(LogsSubtitleText));
            }

            if (!summary.WasCancelled)
            {
                RecordHistory("Clean", summary);
            }

            RefreshDrives();
        }
        finally
        {
            ProgressPercent = 100;
            CurrentScanPath = string.Empty;
            IsBusy = false;
        }
    }

    private async Task RunDismAsync()
    {
        if (!IsAdministrator)
        {
            _uiService.ShowInfo(Loc.Instance["dism_title"], Loc.Instance["dism_admin_needed"]);
            return;
        }

        IsDismBusy = true;
        DismStatusText = Loc.Instance["dism_running"];
        OnPropertyChanged(nameof(HasDismStatus));

        var lineProgress = new Progress<string>(line =>
        {
            DismStatusText = line;
            OnPropertyChanged(nameof(HasDismStatus));
        });

        try
        {
            var res = await _dismService.RunComponentCleanupAsync(lineProgress);
            DismStatusText = res.Success
                ? Loc.Instance.Format("dism_done", res.Duration.TotalSeconds.ToString("0.0"))
                : Loc.Instance.Format("dism_failed", res.ExitCode, res.Output);
            OnPropertyChanged(nameof(HasDismStatus));
            RefreshDrives();
        }
        finally
        {
            IsDismBusy = false;
        }
    }

    private void CancelOperation() => _activeCts?.Cancel();

    private CleanerOptions BuildCleanerOptions() =>
        new(
            MinimumFileAge: TimeSpan.FromHours(_settings.MinimumFileAgeHours),
            Exclusions: _settings.Exclusions.ToList(),
            MaxPreviewItemsPerCategory: 5000);

    private IProgress<ScanProgress> CreateProgressReporter() =>
        new Progress<ScanProgress>(p =>
        {
            CurrentScanPath = p.CurrentPath;
            if (p.TotalCategories > 0)
            {
                double pct = ((double)p.CompletedCategories / p.TotalCategories) * 95.0 + 5.0;
                ProgressPercent = Math.Clamp(pct, 5, 99);
            }
        });

    private void ApplyBatchResults(CleanupSummary summary)
    {
        _lastSummary = summary;
        var map = summary.CategoryResults.ToDictionary(r => r.CategoryId, StringComparer.OrdinalIgnoreCase);

        foreach (var cat in Categories)
        {
            if (map.TryGetValue(cat.Id, out var res))
            {
                cat.Result = res;
            }
        }

        UpdateHeroFromSummary();
        SelectedCategory?.RefreshLocalizedProperties();
    }

    private void UpdateHeroFromSummary()
    {
        if (_lastSummary is null)
        {
            HeroTitle = Loc.Instance["hero_ready_title"];
            HeroSubtitle = Loc.Instance["hero_ready_subtitle"];
            return;
        }

        if (_lastSummary.WasCancelled)
        {
            HeroTitle = Loc.Instance["hero_cancelled_title"];
            HeroSubtitle = Loc.Instance["hero_cancelled_subtitle"];
            return;
        }

        string sec = _lastSummary.Duration.TotalSeconds.ToString("0.0");
        if (_lastSummary.WasDryRun)
        {
            HeroTitle = Loc.Instance.Format("hero_analyzed_title", ByteFormatter.Format(_lastSummary.TotalBytes, _settings.Language));
            HeroSubtitle = Loc.Instance.Format("hero_analyzed_subtitle", _lastSummary.TotalFiles.ToString("N0"), sec);
        }
        else
        {
            HeroTitle = Loc.Instance.Format("hero_cleaned_title", ByteFormatter.Format(_lastSummary.CleanedBytes, _settings.Language));
            HeroSubtitle = Loc.Instance.Format(
                "hero_cleaned_subtitle",
                _lastSummary.CleanedFiles.ToString("N0"),
                _lastSummary.SkippedCount.ToString("N0"),
                sec);
        }
    }

    private void RecordHistory(string actionType, CleanupSummary summary)
    {
        long bytes = summary.WasDryRun ? summary.TotalBytes : summary.CleanedBytes;
        int files = summary.WasDryRun ? summary.TotalFiles : summary.CleanedFiles;

        var entry = new CleanupHistoryEntry
        {
            TimestampUtc = summary.CompletedAtUtc,
            ActionType = actionType,
            BytesAffected = bytes,
            FilesAffected = files,
            SkippedCount = summary.SkippedCount,
            DurationSeconds = Math.Round(summary.Duration.TotalSeconds, 2),
            CategoriesCount = summary.CategoryResults.Count,
            SummaryText = $"{summary.CategoryResults.Count} cat."
        };

        _rawHistory = _historyService.Append(entry);
        RebuildHistoryRows();
    }

    private void ReloadHistory()
    {
        _rawHistory = _historyService.Load();
        RebuildHistoryRows();
    }

    private void RebuildHistoryRows()
    {
        HistoryRows.Clear();
        foreach (var entry in _rawHistory)
        {
            bool isClean = string.Equals(entry.ActionType, "Clean", StringComparison.OrdinalIgnoreCase);
            HistoryRows.Add(new HistoryRowViewModel(
                TimestampText: entry.TimestampUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
                ActionLabel: isClean ? Loc.Instance["logs_action_clean"] : Loc.Instance["logs_action_analyze"],
                IsCleanAction: isClean,
                SizeText: ByteFormatter.Format(entry.BytesAffected, _settings.Language),
                FilesText: $"{entry.FilesAffected:N0} {Loc.Instance["common_files_word"]}",
                SkippedText: entry.SkippedCount > 0 ? $"-{entry.SkippedCount}" : "0",
                DurationText: $"{entry.DurationSeconds:0.0} s",
                SummaryText: entry.SummaryText));
        }

        OnPropertyChanged(nameof(HasNoHistory));
        RelayCommand.RaiseCanExecuteChanged();
    }

    private void ClearLogs()
    {
        _historyService.Clear();
        _rawHistory.Clear();
        RebuildHistoryRows();
    }

    private void ExportLogs()
    {
        string? filePath = _uiService.SaveTextFile($"CleanPulse-History-{DateTime.Now:yyyyMMdd-HHmm}.txt");
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("CleanPulse Operation History");
        sb.AppendLine($"Lifetime Freed: {ByteFormatter.Format(_settings.TotalLifetimeFreedBytes, _settings.Language)}");
        sb.AppendLine(new string('-', 60));

        foreach (var row in HistoryRows)
        {
            sb.AppendLine($"{row.TimestampText} | {row.ActionLabel,-10} | {row.SizeText,10} | {row.FilesText,14} | skip: {row.SkippedText} | {row.DurationText}");
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        _uiService.ShowInfo(Loc.Instance["logs_title"], Loc.Instance.Format("logs_export_done", filePath));
    }

    private void AddExclusion()
    {
        AddExclusionValue(NewExclusionInput);
        NewExclusionInput = string.Empty;
    }

    private void BrowseExclusionFolder()
    {
        string? folder = _uiService.BrowseFolder();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            AddExclusionValue(folder);
        }
    }

    private void ExcludeScannedItem(ScannedItemViewModel? item)
    {
        if (item is null || item.IsSpecialAction || string.IsNullOrWhiteSpace(item.FullPath))
        {
            return;
        }

        AddExclusionValue(item.FullPath);
    }

    private void OpenItemFolder(ScannedItemViewModel? item)
    {
        if (item is null || item.IsSpecialAction || string.IsNullOrWhiteSpace(item.FullPath))
        {
            return;
        }

        _uiService.OpenFolderInExplorer(item.FullPath);
    }

    private void RecycleScannedItem(ScannedItemViewModel? item)
    {
        if (item is null || item.IsSpecialAction || SelectedCategory is null)
        {
            return;
        }

        if (SafetyGuard.IsProtectedPath(item.FullPath) || !File.Exists(item.FullPath))
        {
            return;
        }

        var fileInfo = new FileInfo(item.FullPath);
        if (_fileDeleter.TryDeleteFile(fileInfo, moveToRecycleBin: true))
        {
            var prev = SelectedCategory.Result;
            var remaining = prev.Items.Where(i => !string.Equals(i.FullPath, item.FullPath, StringComparison.OrdinalIgnoreCase)).ToList();
            SelectedCategory.Result = prev with
            {
                TotalBytes = Math.Max(0, prev.TotalBytes - item.SizeBytes),
                FileCount = Math.Max(0, prev.FileCount - 1),
                Items = remaining
            };
            RefreshDrives();
        }
    }

    private void AddExclusionValue(string raw)
    {
        string trimmed = raw.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        if (_settings.Exclusions.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _settings.Exclusions.Add(trimmed);
        _settingsService.Save(_settings);
        Exclusions.Add(trimmed);
        OnPropertyChanged(nameof(HasNoExclusions));
    }

    private void RemoveExclusion(string? item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            return;
        }

        _settings.Exclusions.RemoveAll(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));
        _settingsService.Save(_settings);
        Exclusions.Remove(item);
        OnPropertyChanged(nameof(HasNoExclusions));
    }

    private void BrowseCustomRuleFolder()
    {
        string? folder = _uiService.BrowseFolder();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            NewRulePath = folder;
            if (string.IsNullOrWhiteSpace(NewRuleName))
            {
                NewRuleName = Path.GetFileName(folder.TrimEnd('\\', '/'));
            }
        }
    }

    private void AddCustomRule()
    {
        string? normalized = SafetyGuard.TryNormalizePath(NewRulePath);
        if (normalized is null || !SafetyGuard.IsDirectorySafe(normalized))
        {
            RuleValidationError = Loc.Instance["rules_protected_error"];
            return;
        }

        RuleValidationError = string.Empty;
        var rule = new CustomCleanupRule
        {
            Name = string.IsNullOrWhiteSpace(NewRuleName) ? Path.GetFileName(normalized) : NewRuleName.Trim(),
            FolderPath = normalized,
            SearchPattern = string.IsNullOrWhiteSpace(NewRulePattern) ? "*" : NewRulePattern.Trim(),
            Recursive = NewRuleRecursive,
            RespectMinimumAge = NewRuleRespectAge,
            MoveToRecycleBin = NewRuleToRecycle
        };

        _settings.CustomRules.Add(rule);
        _settingsService.Save(_settings);
        CustomRules.Add(rule);
        OnPropertyChanged(nameof(HasNoCustomRules));

        NewRuleName = string.Empty;
        NewRulePath = string.Empty;
        NewRulePattern = "*";

        RebuildCategories();
    }

    private void RemoveCustomRule(CustomCleanupRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        _settings.CustomRules.RemoveAll(r => string.Equals(r.Id, rule.Id, StringComparison.OrdinalIgnoreCase));
        _settings.CategorySelection.Remove($"custom_{rule.Id}");
        _settingsService.Save(_settings);
        CustomRules.Remove(rule);
        OnPropertyChanged(nameof(HasNoCustomRules));

        RebuildCategories();
    }

    private void RestartAsAdmin()
    {
        if (_privilegeService.RestartAsAdministrator())
        {
            Application.Current?.Shutdown();
        }
    }

    private void RefreshDrives()
    {
        Drives.Clear();
        foreach (var drive in _driveService.GetFixedDrives())
        {
            string freeText = $"{ByteFormatter.Format(drive.FreeBytes, _settings.Language)} {Loc.Instance["drive_free_of"]} {ByteFormatter.Format(drive.TotalBytes, _settings.Language)}";
            Drives.Add(new DriveTileViewModel(drive.DriveName, drive.VolumeLabel, freeText, drive.UsedPercentage));
        }

        OnPropertyChanged(nameof(PrimaryDrive));
    }

    private void RefreshAllLocalizedText()
    {
        foreach (var cat in Categories)
        {
            cat.RefreshLocalizedProperties();
        }

        CategoriesView.Refresh();

        int savedAge = _settings.MinimumFileAgeHours;
        AgeOptions.Clear();
        AgeOptions.Add(new AgeOption(0, Loc.Instance["settings_age_0"]));
        AgeOptions.Add(new AgeOption(1, Loc.Instance["settings_age_1"]));
        AgeOptions.Add(new AgeOption(24, Loc.Instance["settings_age_24"]));
        AgeOptions.Add(new AgeOption(168, Loc.Instance["settings_age_168"]));
        _settings.MinimumFileAgeHours = savedAge;
        OnPropertyChanged(nameof(MinimumFileAgeHours));

        var savedSchedule = _settings.ScheduledCleanFrequency;
        ScheduleOptions.Clear();
        ScheduleOptions.Add(new ScheduleOption(ScheduleFrequency.Disabled, Loc.Instance["sched_disabled"]));
        ScheduleOptions.Add(new ScheduleOption(ScheduleFrequency.Daily, Loc.Instance["sched_daily"]));
        ScheduleOptions.Add(new ScheduleOption(ScheduleFrequency.Weekly, Loc.Instance["sched_weekly"]));
        ScheduleOptions.Add(new ScheduleOption(ScheduleFrequency.Monthly, Loc.Instance["sched_monthly"]));
        _settings.ScheduledCleanFrequency = savedSchedule;
        OnPropertyChanged(nameof(ScheduledCleanFrequency));

        OnPropertyChanged(nameof(PrivilegeStatusText));
        OnPropertyChanged(nameof(LogsSubtitleText));
        RefreshDrives();
        RebuildHistoryRows();
        UpdateHeroFromSummary();
        DiskAnalyzer.RefreshLocalization();
        DuplicateFinder.RefreshLocalization();
        StartupManager.RefreshLocalization();
    }
}
