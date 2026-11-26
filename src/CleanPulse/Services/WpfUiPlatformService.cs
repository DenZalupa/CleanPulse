using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace CleanPulse.Services;

public interface IUiPlatformService
{
    bool Confirm(string title, string message);
    void ShowInfo(string title, string message);
    string? BrowseFolder();
    string? SaveTextFile(string defaultFileName);
    void OpenFolderInExplorer(string path);
    void ApplyTheme(string themeName);
}

public sealed class WpfUiPlatformService : IUiPlatformService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowInfo(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public string? BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? SaveTextFile(string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = ".txt",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void OpenFolderInExplorer(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                return;
            }

            string? dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
            }
        }
        catch
        {
            // Ignore shell launch failures for transient/virtual items.
        }
    }

    public void ApplyTheme(string themeName)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        bool isDark = !string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase);
        string fileName = isDark ? "DarkTheme.xaml" : "LightTheme.xaml";

        var merged = app.Resources.MergedDictionaries;
        var newTheme = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/CleanPulse;component/Themes/{fileName}", UriKind.Absolute)
        };

        if (merged.Count > 0)
        {
            merged[0] = newTheme;
        }
        else
        {
            merged.Insert(0, newTheme);
        }

        if (app.MainWindow is MainWindow mw)
        {
            mw.ApplyWindowChromeForTheme(isDark);
        }
    }
}
