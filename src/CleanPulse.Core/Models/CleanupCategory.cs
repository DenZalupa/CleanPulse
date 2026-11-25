namespace CleanPulse.Core.Models;

/// <summary>
/// Defines a user-selectable cleanup category with metadata and targets.
/// </summary>
public sealed record CleanupCategory(
    string Id,
    CleanupGroup Group,
    string TitleKey,
    string DescriptionKey,
    string SafetyNoteKey,
    bool RequiresAdmin,
    bool IsRecommendedDefault,
    bool IsCaution,
    IReadOnlyList<CleanupTarget> Targets,
    string? DisplayNameOverride = null,
    string? DescriptionOverride = null,
    string? ProcessNameToWarn = null,
    bool IsCustomRule = false);
