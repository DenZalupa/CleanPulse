using System.Collections.ObjectModel;
using CleanPulse.Core.Models;
using CleanPulse.Core.Services;
using CleanPulse.Localization;
using CleanPulse.Services;

namespace CleanPulse.ViewModels;

public sealed class StartupItemViewModel : ObservableObject
{
    private bool _isEnabled;
    private readonly Action<StartupItemViewModel, bool> _onToggle;

    public StartupItemViewModel(StartupItem model, bool isAdmin, Action<StartupItemViewModel, bool> onToggle)
    {
        Model = model;
        _isEnabled = model.IsEnabled;
        CanToggle = !model.RequiresAdmin || isAdmin;
        _onToggle = onToggle;
    }

    public StartupItem Model { get; }
    public string Name => Model.Name;
    public string Command => Model.Command;
    public string SourceLocation => Model.SourceLocation;
    public bool RequiresAdmin => Model.RequiresAdmin;
    public bool CanToggle { get; }

    public string ScopeBadge => Model.Scope switch
    {
        StartupScope.CurrentUserRun => "HKCU Run",
        StartupScope.LocalMachineRun => "HKLM Run",
        StartupScope.CurrentUserStartupFolder => "User Folder",
        StartupScope.CommonStartupFolder => "All Users",
        _ => string.Empty
    };

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _onToggle(this, value);
        }
    }

    public void SetEnabledSilently(bool value)
    {
        SetProperty(ref _isEnabled, value, nameof(IsEnabled));
        OnPropertyChanged(nameof(StateText));
    }

    public string StateText => _isEnabled ? Loc.Instance["startup_enabled"] : Loc.Instance["startup_disabled"];

    public void RefreshLocalization() => OnPropertyChanged(nameof(StateText));
}

public sealed class StartupManagerViewModel : ObservableObject
{
    private readonly IStartupManagerService _service;
    private readonly IUiPlatformService _uiService;
    private readonly bool _isAdmin;
    private readonly List<StartupItemViewModel> _allItems = new();
    private string _searchText = string.Empty;

    public StartupManagerViewModel(
        IStartupManagerService service,
        IUiPlatformService uiService,
        bool isAdmin)
    {
        _service = service;
        _uiService = uiService;
        _isAdmin = isAdmin;

        FilteredItems = new ObservableCollection<StartupItemViewModel>();
        RefreshCommand = new RelayCommand(Load);
        OpenCommandLocationCommand = new RelayCommand<StartupItemViewModel>(OpenCommandLocation);

        Load();
    }

    public ObservableCollection<StartupItemViewModel> FilteredItems { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand<StartupItemViewModel> OpenCommandLocationCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool HasNoItems => FilteredItems.Count == 0;
    public bool ShowAdminWarning => !_isAdmin;

    public void Load()
    {
        _allItems.Clear();
        foreach (var item in _service.GetStartupItems())
        {
            _allItems.Add(new StartupItemViewModel(item, _isAdmin, OnToggleItem));
        }

        ApplyFilter();
    }

    private void OnToggleItem(StartupItemViewModel vm, bool targetEnabled)
    {
        if (!vm.CanToggle)
        {
            _uiService.ShowInfo(Loc.Instance["startup_title"], Loc.Instance["startup_admin_warn"]);
            vm.SetEnabledSilently(vm.Model.IsEnabled);
            return;
        }

        if (_service.SetEnabled(vm.Model, targetEnabled, out string? error))
        {
            vm.SetEnabledSilently(targetEnabled);
        }
        else
        {
            _uiService.ShowInfo(Loc.Instance["startup_title"], error ?? Loc.Instance["startup_admin_warn"]);
            vm.SetEnabledSilently(!targetEnabled);
        }
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        IEnumerable<StartupItemViewModel> query = _allItems;
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string q = _searchText.Trim();
            query = query.Where(i =>
                i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Command.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }

        OnPropertyChanged(nameof(HasNoItems));
    }

    private void OpenCommandLocation(StartupItemViewModel? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Command))
        {
            return;
        }

        string path = ExtractExecutablePath(item.Command);
        if (!string.IsNullOrWhiteSpace(path))
        {
            _uiService.OpenFolderInExplorer(path);
        }
    }

    public static string ExtractExecutablePath(string command)
    {
        string trimmed = command.Trim();
        if (trimmed.StartsWith('"'))
        {
            int endQuote = trimmed.IndexOf('"', 1);
            return endQuote > 1 ? trimmed[1..endQuote] : trimmed.Trim('"');
        }

        int exeIdx = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIdx >= 0)
        {
            return trimmed[..(exeIdx + 4)];
        }

        int lnkIdx = trimmed.IndexOf(".lnk", StringComparison.OrdinalIgnoreCase);
        if (lnkIdx >= 0)
        {
            return trimmed[..(lnkIdx + 4)];
        }

        return trimmed;
    }

    public void RefreshLocalization()
    {
        foreach (var item in _allItems)
        {
            item.RefreshLocalization();
        }
    }
}
