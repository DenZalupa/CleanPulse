using System.Collections.ObjectModel;
using CleanPulse.Core.Formatting;
using CleanPulse.Core.Models;
using CleanPulse.Core.Services;
using CleanPulse.Localization;
using CleanPulse.Services;

namespace CleanPulse.ViewModels;

public sealed record FolderBarViewModel(
    string Name,
    string FullPath,
    string SizeText,
    string FileCountText,
    double PercentOfTotal);

public sealed record LargeFileItemViewModel(
    string Name,
    string DirectoryPath,
    string FullPath,
    string SizeText,
    string ModifiedText,
    long SizeBytes,
    DiskFileCategory FileCategory,
    bool IsProtectedOs);

public sealed class DiskAnalyzerViewModel : ObservableObject
{
    private readonly DiskAnalyzerService _service;
    private readonly IUiPlatformService _uiService;
    private CancellationTokenSource? _cts;
    private DiskAnalysisResult? _lastResult;
    private string _targetPath;
    private bool _isBusy;
    private string _progressPath = string.Empty;
    private string _summaryText = string.Empty;
    private DiskFileCategory _selectedFilter = DiskFileCategory.All;

    public DiskAnalyzerViewModel(
        DiskAnalyzerService service,
        DriveInfoService driveService,
        IUiPlatformService uiService)
    {
        _service = service;
        _uiService = uiService;

        var drives = driveService.GetFixedDrives();
        _targetPath = drives.FirstOrDefault()?.DriveName + @"\" ?? @"C:\";
        AvailableRoots = new ObservableCollection<string>(drives.Select(d => d.DriveName + @"\"));
        string userDownloads = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        if (System.IO.Directory.Exists(userDownloads))
        {
            AvailableRoots.Add(userDownloads);
        }

        TopFolders = new ObservableCollection<FolderBarViewModel>();
        TopFiles = new ObservableCollection<LargeFileItemViewModel>();

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(TargetPath));
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        BrowseFolderCommand = new RelayCommand(BrowseFolder, () => !IsBusy);
        OpenFolderCommand = new RelayCommand<LargeFileItemViewModel>(item =>
        {
            if (item is not null) _uiService.OpenFolderInExplorer(item.FullPath);
        });
        OpenSubfolderCommand = new RelayCommand<FolderBarViewModel>(folder =>
        {
            if (folder is not null) _uiService.OpenFolderInExplorer(folder.FullPath);
        });
        DrillDownFolderCommand = new RelayCommand<FolderBarViewModel>(async folder =>
        {
            if (folder is not null && !IsBusy)
            {
                TargetPath = folder.FullPath;
                await ScanAsync();
            }
        });
        RecycleFileCommand = new RelayCommand<LargeFileItemViewModel>(item => DeleteFile(item, moveToRecycleBin: true));
        DeleteFileCommand = new RelayCommand<LargeFileItemViewModel>(item => DeleteFile(item, moveToRecycleBin: false));

        RefreshLocalization();
    }

    public ObservableCollection<string> AvailableRoots { get; }
    public ObservableCollection<FolderBarViewModel> TopFolders { get; }
    public ObservableCollection<LargeFileItemViewModel> TopFiles { get; }

    public AsyncRelayCommand ScanCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand BrowseFolderCommand { get; }
    public RelayCommand<LargeFileItemViewModel> OpenFolderCommand { get; }
    public RelayCommand<FolderBarViewModel> OpenSubfolderCommand { get; }
    public RelayCommand<FolderBarViewModel> DrillDownFolderCommand { get; }
    public RelayCommand<LargeFileItemViewModel> RecycleFileCommand { get; }
    public RelayCommand<LargeFileItemViewModel> DeleteFileCommand { get; }

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

    public DiskFileCategory SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                RebuildFilesList();
            }
        }
    }

    public bool HasNoResults => _lastResult is null;

    private void BrowseFolder()
    {
        string? folder = _uiService.BrowseFolder();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            TargetPath = folder;
        }
    }

    private async Task ScanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        SummaryText = Loc.Instance["analyzer_scanning"];

        var progress = new Progress<string>(p => ProgressPath = p);
        try
        {
            _lastResult = await _service.AnalyzeAsync(TargetPath, 150, 25, progress, _cts.Token);
            OnPropertyChanged(nameof(HasNoResults));
            RefreshLocalization();
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

    private void DeleteFile(LargeFileItemViewModel? item, bool moveToRecycleBin)
    {
        if (item is null)
        {
            return;
        }

        if (item.IsProtectedOs)
        {
            _uiService.ShowInfo(Loc.Instance["analyzer_title"], Loc.Instance["analyzer_protected_warn"]);
            return;
        }

        if (!moveToRecycleBin)
        {
            string msg = Loc.Instance.Format("confirm_delete_file", item.FullPath);
            if (!_uiService.Confirm(Loc.Instance["confirm_title"], msg))
            {
                return;
            }
        }

        if (_service.TryDeleteFile(item.FullPath, moveToRecycleBin))
        {
            TopFiles.Remove(item);
            if (_lastResult is not null)
            {
                var updatedFiles = _lastResult.TopFiles.Where(f => !string.Equals(f.FullPath, item.FullPath, StringComparison.OrdinalIgnoreCase)).ToList();
                _lastResult = _lastResult with { TopFiles = updatedFiles };
            }
        }
    }

    public void RefreshLocalization()
    {
        if (_lastResult is null)
        {
            SummaryText = Loc.Instance["analyzer_empty"];
            return;
        }

        string lang = Loc.Instance.Language;
        SummaryText = Loc.Instance.Format(
            "analyzer_summary",
            ByteFormatter.Format(_lastResult.TotalBytes, lang),
            _lastResult.TotalFiles.ToString("N0"),
            _lastResult.Duration.TotalSeconds.ToString("0.0"),
            _lastResult.SkippedDirectories.ToString("N0"));

        TopFolders.Clear();
        foreach (var f in _lastResult.TopFolders)
        {
            TopFolders.Add(new FolderBarViewModel(
                f.Name,
                f.FullPath,
                ByteFormatter.Format(f.SizeBytes, lang),
                $"{f.FileCount:N0} {Loc.Instance["common_files_word"]} ({f.PercentOfTotal:0.#}%)",
                f.PercentOfTotal));
        }

        RebuildFilesList();
    }

    private void RebuildFilesList()
    {
        TopFiles.Clear();
        if (_lastResult is null)
        {
            return;
        }

        string lang = Loc.Instance.Language;
        var filtered = _selectedFilter == DiskFileCategory.All
            ? _lastResult.TopFiles
            : _lastResult.TopFiles.Where(f => f.FileCategory == _selectedFilter);

        foreach (var file in filtered)
        {
            TopFiles.Add(new LargeFileItemViewModel(
                file.Name,
                file.DirectoryPath,
                file.FullPath,
                ByteFormatter.Format(file.SizeBytes, lang),
                file.LastModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                file.SizeBytes,
                file.FileCategory,
                SafetyGuard.IsProtectedOsPath(file.FullPath)));
        }
    }
}
