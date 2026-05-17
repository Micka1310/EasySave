using System.Windows;

namespace EasySave.WPF.Services;

public static class ThemeService
{
    public const string Dark = "Dark";
    public const string Light = "Light";

    private const string DarkPath = "/EasySave.WPF;component/Themes/Colors.Dark.xaml";
    private const string LightPath = "/EasySave.WPF;component/Themes/Colors.Light.xaml";

    private static ResourceDictionary? _colorsHost;

    public static string Normalize(string? theme) =>
        string.Equals(theme, Light, StringComparison.OrdinalIgnoreCase) ? Light : Dark;

    public static void Apply(string? theme)
    {
        System.Windows.Application? app = System.Windows.Application.Current;
        if (app is null) return;

        ResourceDictionary loaded = LoadPalette(Normalize(theme));
        EnsureHost(app);
        CopyIntoHost(loaded);
    }

    private static ResourceDictionary LoadPalette(string normalized)
    {
        string path = normalized == Light ? LightPath : DarkPath;
        return (ResourceDictionary)System.Windows.Application.LoadComponent(new Uri(path, UriKind.Relative));
    }

    private static void EnsureHost(System.Windows.Application app)
    {
        if (_colorsHost is not null) return;

        IList<ResourceDictionary> merged = app.Resources.MergedDictionaries;
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            string? src = merged[i].Source?.OriginalString;
            if (src is not null && src.Contains("Colors", StringComparison.OrdinalIgnoreCase))
                merged.RemoveAt(i);
        }

        _colorsHost = new ResourceDictionary();
        merged.Insert(0, _colorsHost);
    }

    private static void CopyIntoHost(ResourceDictionary loaded)
    {
        if (_colorsHost is null) return;

        foreach (object key in loaded.Keys)
            _colorsHost[key] = loaded[key];
    }
}
