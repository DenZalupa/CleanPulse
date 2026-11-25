using System.Collections.ObjectModel;
using CleanPulse.Core.Formatting;
using CleanPulse.Core.Models;
using CleanPulse.Core.Services;
using CleanPulse.Localization;
using CleanPulse.Services;

namespace CleanPulse.ViewModels;

public sealed record MinSizeOption(long Bytes, string Label);

public sealed class DuplicateFileItemViewModel : ObservableObject
{
    private bool _isSelected;
    private readonly Action _onChanged;

    public DuplicateFileItemViewModel(DuplicateFileEntry model, string language, Action onChanged)
    {
        Model = model;
        SizeText = ByteFormatter.Format(model.SizeBytes, language);
        ModifiedText = model.LastModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        _onChanged = onChanged;
    }

    public DuplicateFileEntry Model { get; }
    public string Name => Model.Name;
    public string DirectoryPath => Model.DirectoryPath;
    public string FullPath => Model.FullPath;
    public string SizeText { get; }
    public string ModifiedText { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onChanged();
            }
        }
    }
}

public sealed class DuplicateGroupViewModel : ObservableObject
{
    public DuplicateGroupViewModel(
        DuplicateGroup model,
        string language,
        Action onSelectionChanged)
    {
        Model = model;
        HeaderText = Loc.Instance.Format(
            "dup_group_header",
            ByteFormatter.Format(model.FileSizeBytes, language),
            model.Files.Count,
            ByteFormatter.Format(model.ReclaimableBytes, language));

        Files = new ObservableCollection<DuplicateFileItemViewModel>(
            model.Files.Select(f => new DuplicateFileItemViewModel(f, language, onSelectionChanged)));
    }

    public DuplicateGroup Model { get; }
    public string HeaderText { get; }
    public ObservableCollection<DuplicateFileItemViewModel> Files { get; }
}

public sealed class DuplicateFinderViewModel : ObservableObject
{
    private readonly DuplicateFinderService _service;
    private readonly IUiPlatformService _uiService;
    private CancellationTokenSource? _cts;
    private DuplicateScanResult? _lastResult;
    private string _targetPath;
    private long _minSizeBytes = 1024 * 1024;
    private bool _isBusy;
    private string _progressPath = string.Empty;
    private string _summaryText = string.Empty;

    public DuplicateFinderViewModel(DuplicateFinderService service, IUiPlatformService uiService)
    {
        _service = service;
        _uiService = uiService;

        string downloads = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        _targetPath = System.IO.Directory.Exists(downloads)
            ? downloads
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Groups = new ObservableCollection<DuplicateGroupViewModel>();
        MinSizeOptions = new ObservableCollection<MinSizeOption>
        {
            new(100 * 1024, "100 KB+"),
            new(1024 * 1024, "1 MB+"),
            new(10 * 1024 * 1024, "10 MB+")
        };

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(TargetPath));
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        BrowseFolderCommand = new RelayCommand(() =>
        {
            string? folder = _uiService.BrowseFolder();
            if (!string.IsNullOrWhiteSpace(folder)) TargetPath = folder;
        }, () => !IsBusy);
        SelectKeepOldestCommand = new RelayCommand(() => SelectCopies(keepOldest: true), () => Groups.Count > 0 && !IsBusy);
        SelectKeepNewestCommand = new RelayCommand(() => SelectCopies(keepOldest: false), () => Groups.Count > 0 && !IsBusy);
        ClearSelectionCommand = new RelayCommand(() =>
        {
            foreach (var g in Groups)
            foreach (var f in g.Files)
                f.IsSelected = false;
        }, () => Groups.Count > 0 && !IsBusy);
        RecycleSelectedCommand = new RelayCommand(RecycleSelected, () => !IsBusy && Groups.Any(g => g.Files.Any(f => f.IsSelected)));
        OpenFolderCommand = new RelayCommand<DuplicateFileItemViewModel>(item =>
        {
            if (item is not null) _uiService.OpenFolderInExplorer(item.FullPath);
        });

        RefreshLocalization();
    }

    public ObservableCollection<DuplicateGroupViewModel> Groups { get; }
    public ObservableCollection<MinSizeOption> MinSizeOptions { get; }

    public AsyncRelayCommand ScanCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand BrowseFolderCommand { get; }
    public RelayCommand SelectKeepOldestCommand { get; }
    public RelayCommand SelectKeepNewestCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand RecycleSelectedCommand { get; }
    public RelayCommand<DuplicateFileItemViewModel> OpenFolderCommand { get; }

    public string TargetPath
    {
        get => _targetPath;
        set
        {
            if (SetProperty(ref _targetPath, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public long MinSizeBytes
    {
        get => _minSizeBytes;
        set => SetProperty(ref _minSizeBytes, value);
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

    public string ProgressPath
    {
        get => _progressPath;
        private set => SetProperty(ref _progressPath, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public bool HasNoGroups => Groups.Count == 0;

    private async Task ScanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        SummaryText = Loc.Instance["dup_scanning"];

        var progress = new Progress<string>(p => ProgressPath = p);
        try
        {
            _lastResult = await _service.ScanAsync(TargetPath, MinSizeBytes, progress, _cts.Token);
            RefreshLocalization();
            SelectCopies(keepOldest: true);
        }
        catch (OperationCanceledException)
        {
            SummaryText = Loc.Instance["status_cancelled"];
        }
        finally
        {
            ProgressPath = string.Empty;
            IsBusy = false;
        }
    }

    private void SelectCopies(bool keepOldest)
    {
        foreach (var group in Groups)
        {
            int keepIndex = keepOldest ? 0 : group.Files.Count - 1;
            for (int i = 0; i < group.Files.Count; i++)
            {
                group.Files[i].IsSelected = i != keepIndex;
            }
        }

        RelayCommand.RaiseCanExecuteChanged();
    }

    private void RecycleSelected()
    {
        var selected = Groups
            .SelectMany(g => g.Files)
            .Where(f => f.IsSelected)
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        var (deleted, skipped, freed) = _service.DeleteFiles(selected.Select(s => s.FullPath), moveToRecycleBin: true);
        foreach (var group in Groups.ToList())
        {
            foreach (var file in group.Files.Where(f => f.IsSelected && !System.IO.File.Exists(f.FullPath)).ToList())
            {
                group.Files.Remove(file);
            }

            if (group.Files.Count <= 1)
            {
                Groups.Remove(group);
            }
        }

        OnPropertyChanged(nameof(HasNoGroups));
        SummaryText = Loc.Instance.Format(
            "dup_deleted_result",
            deleted,
            ByteFormatter.Format(freed, Loc.Instance.Language),
            skipped);
        RelayCommand.RaiseCanExecuteChanged();
    }

    public void RefreshLocalization()
    {
        Groups.Clear();
        if (_lastResult is null)
        {
            SummaryText = Loc.Instance["dup_empty"];
            OnPropertyChanged(nameof(HasNoGroups));
            return;
        }

        string lang = Loc.Instance.Language;
        if (_lastResult.Groups.Count == 0)
        {
            SummaryText = Loc.Instance["dup_none_found"];
        }
        else
        {
            SummaryText = Loc.Instance.Format(
                "dup_summary",
                _lastResult.Groups.Count,
                ByteFormatter.Format(_lastResult.TotalReclaimableBytes, lang),
                _lastResult.ScannedFiles.ToString("N0"),
                _lastResult.Duration.TotalSeconds.ToString("0.0"));
        }

        foreach (var g in _lastResult.Groups)
        {
            Groups.Add(new DuplicateGroupViewModel(g, lang, () => RelayCommand.RaiseCanExecuteChanged()));
        }

        OnPropertyChanged(nameof(HasNoGroups));
        RelayCommand.RaiseCanExecuteChanged();
    }
}
