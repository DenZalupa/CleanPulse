using System.Runtime.InteropServices;
using System.Windows;
using CleanPulse.Core.Services;
using CleanPulse.Services;
using CleanPulse.ViewModels;

namespace CleanPulse;

public partial class App : Application
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int AttachParentProcess = -1;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var recycleBin = new WindowsShellRecycleBin();
        var dnsFlusher = new WindowsDnsCacheFlusher();
        var fileDeleter = new WindowsFileDeletionService();
        var engine = new CleanerEngine(recycleBin, dnsFlusher, fileDeleter);
        var settingsService = new SettingsService();
        var historyService = new HistoryService();

        if (CliRunner.IsCliInvocation(e.Args))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            bool silent = e.Args.Any(a => string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase));
            if (!silent)
            {
                AttachConsole(AttachParentProcess);
            }

            var cli = new CliRunner(engine, settingsService, historyService);
            int exitCode = await cli.RunAsync(e.Args);
            Shutdown(exitCode);
            return;
        }

        var driveService = new DriveInfoService();
        var privilegeService = new WindowsPrivilegeService();
        var uiService = new WpfUiPlatformService();

        var viewModel = new MainViewModel(
            engine,
            settingsService,
            historyService,
            driveService,
            privilegeService,
            uiService,
            new WindowsProcessDetector(),
            fileDeleter,
            new WindowsDismService(),
            new WindowsScheduledTaskService(),
            new WindowsStartupManagerService(),
            new DiskAnalyzerService(fileDeleter),
            new DuplicateFinderService(fileDeleter));

        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Loaded += async (_, _) => await viewModel.InitializeAsync();
        MainWindow = window;
        window.Show();
    }
}
