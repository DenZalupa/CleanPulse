using System.Text.Json;
using CleanPulse.Core.Models;

namespace CleanPulse.Core.Services;

/// <summary>
/// Persists and loads local user preferences and cleanup history under %LOCALAPPDATA%\CleanPulse.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;

    public SettingsService(string? baseDirectory = null)
    {
        string root = baseDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CleanPulse");
        _settingsPath = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            loaded.CategorySelection = new Dictionary<string, bool>(loaded.CategorySelection ?? new(), StringComparer.OrdinalIgnoreCase);
            loaded.Exclusions ??= new List<string>();
            loaded.CustomRules ??= new List<CustomCleanupRule>();
            if (loaded.Language is not ("ru" or "en"))
            {
                loaded.Language = "ru";
            }

            if (loaded.DesignVersion < 2)
            {
                loaded.Theme = "Light";
                loaded.DesignVersion = 2;
            }

            if (loaded.Theme is not ("Dark" or "Light"))
            {
                loaded.Theme = "Light";
            }

            if (loaded.MinimumFileAgeHours < 0)
            {
                loaded.MinimumFileAgeHours = 24;
            }

            return loaded;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string? dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}

public sealed class HistoryService
{
    private const int MaxEntries = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _historyPath;

    public HistoryService(string? baseDirectory = null)
    {
        string root = baseDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CleanPulse");
        _historyPath = Path.Combine(root, "history.json");
    }

    public List<CleanupHistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(_historyPath))
            {
                return new List<CleanupHistoryEntry>();
            }

            string json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<List<CleanupHistoryEntry>>(json, JsonOptions) ?? new List<CleanupHistoryEntry>();
        }
        catch
        {
            return new List<CleanupHistoryEntry>();
        }
    }

    public List<CleanupHistoryEntry> Append(CleanupHistoryEntry entry)
    {
        var list = Load();
        list.Insert(0, entry);
        if (list.Count > MaxEntries)
        {
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        }

        Save(list);
        return list;
    }

    public void Clear() => Save(new List<CleanupHistoryEntry>());

    private void Save(List<CleanupHistoryEntry> entries)
    {
        string? dir = Path.GetDirectoryName(_historyPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(_historyPath, json);
    }
}

public sealed class DriveInfoService
{
    public IReadOnlyList<DriveSpaceInfo> GetFixedDrives()
    {
        var result = new List<DriveSpaceInfo>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                {
                    continue;
                }

                result.Add(new DriveSpaceInfo(
                    DriveName: drive.Name.TrimEnd('\\'),
                    VolumeLabel: string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Windows" : drive.VolumeLabel,
                    TotalBytes: drive.TotalSize,
                    FreeBytes: drive.AvailableFreeSpace));
            }
        }
        catch
        {
            // Ignore transient drive query failures.
        }

        return result;
    }
}
