using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Knarr.App.Services;
using Knarr.App.ViewModels;
using Knarr.App.Views;

namespace Knarr.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var platformInfo = new PlatformInfoProvider();
            var themeService = new ThemeService();

            var viewModel = new MainWindowViewModel(platformInfo, themeService);
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            // Probe the CLI in the background; property updates marshal back to the UI thread.
            _ = viewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
