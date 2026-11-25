using System.Globalization;

namespace CleanPulse.Core.Formatting;

/// <summary>
/// Formats byte counts into human-readable localized size strings.
/// </summary>
public static class ByteFormatter
{
    private static readonly string[] UnitsEn = ["B", "KB", "MB", "GB", "TB"];
    private static readonly string[] UnitsRu = ["Б", "КБ", "МБ", "ГБ", "ТБ"];

    public static string Format(long bytes, string language = "ru")
    {
        if (bytes < 0)
        {
            bytes = 0;
        }

        var units = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? UnitsEn
            : UnitsRu;

        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024.0 && unitIndex < units.Length - 1)
        {
            value /= 1024.0;
            unitIndex++;
        }

        var culture = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo("ru-RU");

        string format = unitIndex == 0 ? "0" : (value >= 100 ? "0.#" : "0.##");
        return $"{value.ToString(format, culture)} {units[unitIndex]}";
    }
}
