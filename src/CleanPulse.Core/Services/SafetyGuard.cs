namespace CleanPulse.Core.Services;

/// <summary>
/// Enforces hard safety boundaries so the cleaner can never touch OS core or personal user folders.
/// </summary>
public static class SafetyGuard
{
    private static readonly Lazy<HashSet<string>> ExactProtectedPaths = new(BuildExactProtectedPaths);
    private static readonly Lazy<List<string>> ProtectedSubtrees = new(BuildProtectedSubtrees);

    /// <summary>
    /// Normalizes a filesystem path for safe comparison. Returns null if the path is invalid.
    /// </summary>
    public static string? TryNormalizePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        try
        {
            string cleaned = rawPath.Trim().Trim('"');
            if (cleaned.Length == 0)
            {
                return null;
            }

            string full = Path.GetFullPath(cleaned);
            string? root = Path.GetPathRoot(full);
            if (!string.IsNullOrEmpty(root) && string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
            {
                return full;
            }

            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true if the path points to a drive root, Windows core directory, or personal user library.
    /// </summary>
    public static bool IsProtectedPath(string? rawPath)
    {
        string? normalized = TryNormalizePath(rawPath);
        if (normalized is null)
        {
            return true;
        }

        string? root = Path.GetPathRoot(normalized);
        if (!string.IsNullOrEmpty(root))
        {
            string rootTrimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, rootTrimmed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (ExactProtectedPaths.Value.Contains(normalized))
        {
            return true;
        }

        foreach (string subtree in ProtectedSubtrees.Value)
        {
            if (string.Equals(normalized, subtree, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.StartsWith(subtree + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true when the given directory is safe to traverse and does not cross a symlink/junction.
    /// </summary>
    public static bool IsDirectorySafe(string? directoryPath)
    {
        if (IsProtectedPath(directoryPath))
        {
            return false;
        }

        string? normalized = TryNormalizePath(directoryPath);
        if (normalized is null)
        {
            return false;
        }

        if (!Directory.Exists(normalized))
        {
            return true;
        }

        try
        {
            var info = new DirectoryInfo(normalized);
            return !IsReparsePoint(info);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects junctions and symbolic links so recursive cleanup never escapes its target directory.
    /// </summary>
    public static bool IsReparsePoint(FileSystemInfo info) =>
        (info.Attributes & FileAttributes.ReparsePoint) != 0;

    private static HashSet<string> BuildExactProtectedPaths()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? path)
        {
            string? normalized = TryNormalizePath(path);
            if (normalized is not null)
            {
                set.Add(normalized);
            }
        }

        Add(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

        string? windows = TryNormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        if (windows is not null)
        {
            string? sysDrive = Path.GetPathRoot(windows);
            if (!string.IsNullOrEmpty(sysDrive))
            {
                Add(Path.Combine(sysDrive, "Users"));
            }
        }

        return set;
    }

    private static List<string> BuildProtectedSubtrees()
    {
        var list = new List<string>();

        void Add(string? path)
        {
            string? normalized = TryNormalizePath(path);
            if (normalized is not null && !list.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(normalized);
            }
        }

        Add(Environment.GetFolderPath(Environment.SpecialFolder.System));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            Add(Path.Combine(userProfile, "Downloads"));
            Add(Path.Combine(userProfile, "OneDrive"));
        }

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windows))
        {
            Add(Path.Combine(windows, "WinSxS"));
            Add(Path.Combine(windows, "Boot"));
            Add(Path.Combine(windows, "Fonts"));
            Add(Path.Combine(windows, "servicing"));
            Add(Path.Combine(windows, "SystemApps"));
        }

        return list;
    }
}
