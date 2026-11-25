using System.IO.Enumeration;

namespace CleanPulse.Core.Services;

/// <summary>
/// Matches file and directory paths against user-configured exclusion rules (paths, prefixes, wildcards).
/// </summary>
public sealed class ExclusionMatcher
{
    private readonly List<string> _exactOrPrefixPaths = new();
    private readonly List<string> _wildcardPatterns = new();

    public ExclusionMatcher(IEnumerable<string>? exclusions)
    {
        if (exclusions is null)
        {
            return;
        }

        foreach (string raw in exclusions)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            string trimmed = raw.Trim().Trim('"');
            bool hasSeparator = trimmed.Contains(Path.DirectorySeparatorChar) || trimmed.Contains(Path.AltDirectorySeparatorChar);
            bool hasWildcard = trimmed.Contains('*') || trimmed.Contains('?');

            if (hasWildcard || (!hasSeparator && !Path.IsPathRooted(trimmed)))
            {
                _wildcardPatterns.Add(trimmed);
            }
            else
            {
                string? normalized = SafetyGuard.TryNormalizePath(trimmed);
                if (normalized is not null)
                {
                    _exactOrPrefixPaths.Add(normalized);
                }
            }
        }
    }

    public bool IsExcluded(string fullPath)
    {
        if (_exactOrPrefixPaths.Count == 0 && _wildcardPatterns.Count == 0)
        {
            return false;
        }

        string? normalized = SafetyGuard.TryNormalizePath(fullPath);
        if (normalized is null)
        {
            return false;
        }

        foreach (string prefix in _exactOrPrefixPaths)
        {
            if (string.Equals(normalized, prefix, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        string fileName = Path.GetFileName(normalized);
        foreach (string pattern in _wildcardPatterns)
        {
            bool hasSeparator = pattern.Contains(Path.DirectorySeparatorChar) || pattern.Contains(Path.AltDirectorySeparatorChar);
            string target = hasSeparator ? normalized : fileName;
            string normalizedPattern = pattern.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (FileSystemName.MatchesSimpleExpression(normalizedPattern, target, ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }
}
