using System.Collections.Generic;
using Avalonia.Data.Converters;
using ShadUI;

namespace CertificateViewer.Converters;

public static class ThemeModeConverters
{
    private static readonly Dictionary<ThemeMode, string> Icons = new()
    {
        { ThemeMode.System, "" },
        { ThemeMode.Light, "" },
        { ThemeMode.Dark, "" }
    };

    public static IValueConverter ToLucideIcon
    {
        get
        {
            var converter = new FuncValueConverter<ThemeMode, string>(mode =>
                Icons.TryGetValue(mode, out var icon) ? icon : Icons[0]);
            return converter;
        }
    }
}
