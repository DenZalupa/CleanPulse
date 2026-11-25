namespace CleanPulse.Core.Services;

/// <summary>
/// Enforces hard safety boundaries so automated and manual cleanup cannot damage OS core or personal libraries.
/// </summary>
public static class SafetyGuard
{
    private static readonly Lazy<HashSet<string>> ExactOsPaths = new(BuildExactOsPaths);
    private static readonly Lazy<List<string>> ProtectedOsSubtrees = new(BuildProtectedOsSubtrees);
    private static readonly Lazy<List<string>> PersonalLibrarySubtrees = new(BuildPersonalLibrarySubtrees);

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
    /// Strict check for automated cleaner categories and custom cleanup rules:
    /// blocks drive roots, core Windows/Program Files paths, AND personal user libraries (Desktop, Documents, Downloads, etc.).
    /// </summary>
    public static bool IsProtectedPath(string? rawPath)
    {
        if (IsProtectedOsPath(rawPath))
        {
            return true;
        }

        string? normalized = TryNormalizePath(rawPath);
        if (normalized is null)
        {
            return true;
        }

        return MatchesAnySubtree(normalized, PersonalLibrarySubtrees.Value);
    }

    /// <summary>
    /// Core OS protection check used before manual deletion in Disk Analyzer or Duplicate Finder:
    /// blocks drive roots, Windows/System32/WinSxS/Program Files/UserProfile root, while allowing user data subfolders.
    /// </summary>
    public static bool IsProtectedOsPath(string? rawPath)
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

        if (ExactOsPaths.Value.Contains(normalized))
        {
            return true;
        }

        return MatchesAnySubtree(normalized, ProtectedOsSubtrees.Value);
    }

    /// <summary>
    /// Returns true when the given directory is safe for automated cleaner traversal and is not a symlink/junction.
    /// </summary>
    public static bool IsDirectorySafe(string? directoryPath)
    {
        if (IsProtectedPath(directoryPath))
        {
            return false;
        }

        return IsNonReparseDirectoryOrMissing(directoryPath);
    }

    /// <summary>
    /// Returns true when the given directory can be safely inspected (read-only) without crossing a symlink/junction.
    /// Allows drive roots and personal folders for Disk Analyzer / Duplicate Finder.
    /// </summary>
    public static bool IsDirectorySafeForAnalysis(string? directoryPath)
    {
        string? normalized = TryNormalizePath(directoryPath);
        if (normalized is null || !Directory.Exists(normalized))
        {
            return false;
        }

        try
        {
            var info = new DirectoryInfo(normalized);
            // Drive roots (e.g. C:\) have Attributes Directory (and sometimes Hidden/System), never ReparsePoint.
            return !IsReparsePoint(info);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects junctions and symbolic links so recursive traversal never escapes or loops.
    /// </summary>
    public static bool IsReparsePoint(FileSystemInfo info) =>
        (info.Attributes & FileAttributes.ReparsePoint) != 0;

    private static bool IsNonReparseDirectoryOrMissing(string? directoryPath)
    {
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

    private static bool MatchesAnySubtree(string normalized, List<string> subtrees)
    {
        foreach (string subtree in subtrees)
        {
            if (string.Equals(normalized, subtree, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(subtree + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> BuildExactOsPaths()
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

    private static List<string> BuildProtectedOsSubtrees()
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

    private static List<string> BuildPersonalLibrarySubtrees()
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

        return list;
    }
}
