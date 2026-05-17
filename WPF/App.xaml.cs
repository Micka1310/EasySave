using System.Windows;
using EasySave.WPF.Services;
using Application = System.Windows.Application;

namespace EasySave.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        GeneralSettings settings = new GeneralSettingsService().Load();
        ThemeService.Apply(settings.AppTheme);
        base.OnStartup(e);
    }
}
