using CleanPulse.Core.Formatting;
using CleanPulse.Core.Models;
using CleanPulse.Core.Services;
using CleanPulse.Localization;

namespace CleanPulse.ViewModels;

public sealed record ScannedItemViewModel(
    string DisplayName,
    string FullPath,
    string SizeText,
    string ModifiedText,
    long SizeBytes,
    bool IsSpecialAction)
{
    public static ScannedItemViewModel FromModel(ScannedItem item, string language) =>
        new(
            DisplayName: item.DisplayName,
            FullPath: item.FullPath,
            SizeText: item.IsSpecialAction && item.SizeBytes == 0 ? "—" : ByteFormatter.Format(item.SizeBytes, language),
            ModifiedText: item.IsSpecialAction ? "—" : item.LastModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            SizeBytes: item.SizeBytes,
            IsSpecialAction: item.IsSpecialAction);
}

public sealed class CategoryItemViewModel : ObservableObject
{
    private bool _isChecked;
    private CategoryScanResult _result;
    private readonly bool _isAdmin;
    private readonly Action<CategoryItemViewModel> _onSelectionChanged;

    public CategoryItemViewModel(
        CleanupCategory model,
        bool isChecked,
        bool isAdmin,
        Action<CategoryItemViewModel> onSelectionChanged)
    {
        Model = model;
        _isChecked = isChecked;
        _isAdmin = isAdmin;
        _result = CategoryScanResult.Empty(model.Id);
        _onSelectionChanged = onSelectionChanged;
    }

    public CleanupCategory Model { get; }

    public string Id => Model.Id;
    public CleanupGroup Group => Model.Group;
    public bool RequiresAdmin => Model.RequiresAdmin;
    public bool ShowAdminBadge => Model.RequiresAdmin && !_isAdmin;
    public bool IsCaution => Model.IsCaution;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
            {
                _onSelectionChanged(this);
            }
        }
    }

    public void SetCheckedSilently(bool value) => SetProperty(ref _isChecked, value, nameof(IsChecked));

    public CategoryScanResult Result
    {
        get => _result;
        set
        {
            if (SetProperty(ref _result, value))
            {
                RefreshLocalizedProperties();
            }
        }
    }

    public string Title => Loc.Instance[Model.TitleKey];
    public string Description => Loc.Instance[Model.DescriptionKey];
    public string SafetyNote => Loc.Instance[Model.SafetyNoteKey];
    public string GroupTitle => Model.Group switch
    {
        CleanupGroup.SystemAndTemp => Loc.Instance["group_system_temp"],
        CleanupGroup.WindowsCache => Loc.Instance["group_windows_cache"],
        CleanupGroup.LogsAndDumps => Loc.Instance["group_logs_dumps"],
        _ => string.Empty
    };

    public string TargetPathsText => string.Join(
        Environment.NewLine,
        Model.Targets.Select(t => t.Kind == TargetKind.DirectoryContents ? $"{t.Path} ({t.SearchPattern})" : t.Path));

    public long DisplayBytes => _result.Status == CategoryStatus.Cleaned ? _result.CleanedBytes : _result.TotalBytes;

    public string SizeText => _result.Status switch
    {
        CategoryStatus.Idle or CategoryStatus.Scanning => "—",
        CategoryStatus.AccessDenied => Loc.Instance["badge_admin"],
        _ => ByteFormatter.Format(DisplayBytes, Loc.Instance.Language)
    };

    public string FileCountText => _result.Status switch
    {
        CategoryStatus.Analyzed => $"{_result.FileCount:N0} {Loc.Instance["common_files_word"]}",
        CategoryStatus.Cleaned => _result.SkippedCount > 0
            ? $"{_result.CleanedCount:N0} (-{_result.SkippedCount})"
            : $"{_result.CleanedCount:N0} {Loc.Instance["common_files_word"]}",
        _ => string.Empty
    };

    public string StatusText => _result.Status switch
    {
        CategoryStatus.Idle => Loc.Instance["status_idle"],
        CategoryStatus.Scanning => Loc.Instance["status_scanning"],
        CategoryStatus.Analyzed => Loc.Instance["status_analyzed"],
        CategoryStatus.Cleaning => Loc.Instance["status_cleaning"],
        CategoryStatus.Cleaned => Loc.Instance["status_cleaned"],
        CategoryStatus.AccessDenied => Loc.Instance["status_access_denied"],
        CategoryStatus.NotFound => Loc.Instance["status_not_found"],
        CategoryStatus.Cancelled => Loc.Instance["status_cancelled"],
        CategoryStatus.Error => Loc.Instance["status_error"],
        _ => string.Empty
    };

    public bool IsAccessDenied => _result.Status == CategoryStatus.AccessDenied;
    public bool IsCleaned => _result.Status == CategoryStatus.Cleaned;
    public bool IsAnalyzedWithItems => _result.Status == CategoryStatus.Analyzed && (_result.TotalBytes > 0 || _result.FileCount > 0);
    public bool ShowTruncatedNotice => _result.Items.Count < _result.FileCount && _result.Items.Count > 0;
    public string TruncatedNoticeText => Loc.Instance.Format("inspector_truncated_note", _result.FileCount.ToString("N0"));

    public IReadOnlyList<ScannedItemViewModel> PreviewItems =>
        _result.Items.Select(i => ScannedItemViewModel.FromModel(i, Loc.Instance.Language)).ToList();

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(SafetyNote));
        OnPropertyChanged(nameof(GroupTitle));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(FileCountText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsAccessDenied));
        OnPropertyChanged(nameof(IsCleaned));
        OnPropertyChanged(nameof(IsAnalyzedWithItems));
        OnPropertyChanged(nameof(ShowTruncatedNotice));
        OnPropertyChanged(nameof(TruncatedNoticeText));
        OnPropertyChanged(nameof(PreviewItems));
    }
}
