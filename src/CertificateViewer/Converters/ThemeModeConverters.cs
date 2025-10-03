using System.Collections.Generic;
using Avalonia.Data.Converters;
using Lucide.Avalonia;
using ShadUI;

namespace CertificateViewer.Converters;

public static class ThemeModeConverters
{
    private static readonly Dictionary<ThemeMode, LucideIconKind> Icons = new()
    {
        // { ThemeMode.System, ""  },
        // { ThemeMode.Light, "" },
        // { ThemeMode.Dark, "" },
        {
            ThemeMode.System, LucideIconKind.SunMoon
        },
        {
            ThemeMode.Light, LucideIconKind.Sun
        },
        {
            ThemeMode.Dark, LucideIconKind.Moon
        }

    };

    public static IValueConverter ToLucideIcon
    {
        get
        {
            var converter = new FuncValueConverter<ThemeMode, LucideIconKind>(mode =>
                Icons.TryGetValue(mode, out var icon) ? icon : Icons[0]);
            return converter;
        }
    }
}
