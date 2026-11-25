using System.ComponentModel;
using CleanPulse.Core.Services;

namespace CleanPulse.Localization;

/// <summary>
/// Observable localization bridge allowing live RU/EN language switching in XAML bindings.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    private string _language = "ru";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            string normalized = string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
            if (_language == normalized)
            {
                return;
            }

            _language = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }

    public string this[string key] => LocalizationCatalog.Get(key, _language);

    public string Format(string key, params object[] args) => LocalizationCatalog.Format(key, _language, args);
}
