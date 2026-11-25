using System.Windows;
using CleanPulse.Core.Services;
using CleanPulse.Services;
using CleanPulse.ViewModels;

namespace CleanPulse;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var recycleBin = new WindowsShellRecycleBin();
        var dnsFlusher = new WindowsDnsCacheFlusher();
        var engine = new CleanerEngine(recycleBin, dnsFlusher);
        var settingsService = new SettingsService();
        var historyService = new HistoryService();
        var driveService = new DriveInfoService();
        var privilegeService = new WindowsPrivilegeService();
        var uiService = new WpfUiPlatformService();

        var viewModel = new MainViewModel(
            engine,
            settingsService,
            historyService,
            driveService,
            privilegeService,
            uiService);

        var window = new MainWindow
        {
            DataContext = viewModel
        };

        MainWindow = window;
        window.Show();
    }
}
