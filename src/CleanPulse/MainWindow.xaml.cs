using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CleanPulse;

public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaCaptionColor = 35;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            bool isDark = DataContext is ViewModels.MainViewModel vm && vm.IsDarkTheme;
            ApplyWindowChromeForTheme(isDark);
        };
    }

    public void ApplyWindowChromeForTheme(bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            int rounded = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref rounded, sizeof(int));

            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));

            // COLORREF is 0x00BBGGRR: #EAECEF -> 0x00EFECEA, #12151B -> 0x001B1512
            int captionColor = isDark ? 0x001B1512 : 0x00EFECEA;
            DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref captionColor, sizeof(int));
        }
        catch
        {
            // Ignore on Windows 10 builds where Windows 11 DWM attributes are unavailable.
        }
    }
}
