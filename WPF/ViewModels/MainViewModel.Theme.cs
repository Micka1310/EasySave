using System.Windows.Input;
using EasySave.WPF.Services;

namespace EasySave.WPF.ViewModels;

public partial class MainViewModel
{
    private string _appTheme = ThemeService.Dark;

    public ICommand ToggleThemeCommand { get; private set; } = null!;

    public string AppTheme
    {
        get => _appTheme;
        set
        {
            string v = ThemeService.Normalize(value);
            if (!SetField(ref _appTheme, v)) return;
            _settings.AppTheme = v;
            _generalSettingsService.Save(_settings);
            ThemeService.Apply(v);
            NotifyThemeUi();
        }
    }

    public bool IsDarkTheme => AppTheme == ThemeService.Dark;
    public bool IsLightTheme => AppTheme == ThemeService.Light;

    /// <summary>Glyph Segoe MDL2 (soleil = passer en clair, lune = passer en sombre).</summary>
    public string ThemeToggleGlyph => AppTheme == ThemeService.Dark ? "\uE706" : "\uE4C0";

    public string ThemeToggleToolTip => AppTheme == ThemeService.Dark
        ? (IsFrench ? "Mode clair" : "Light mode")
        : (IsFrench ? "Mode sombre" : "Dark mode");

    internal void InitializeThemeCommands()
    {
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
    }

    private void ToggleTheme()
    {
        AppTheme = AppTheme == ThemeService.Dark ? ThemeService.Light : ThemeService.Dark;
    }

    private void InitializeThemeFromSettings()
    {
        _appTheme = ThemeService.Normalize(_settings.AppTheme);
        _settings.AppTheme = _appTheme;
        ThemeService.Apply(_appTheme);
        NotifyThemeUi();
    }

    private void NotifyThemeUi()
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(ThemeToggleGlyph));
        OnPropertyChanged(nameof(ThemeToggleToolTip));
    }
}
