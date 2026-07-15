using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Services;

namespace Knarr.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPlatformInfoProvider _platformInfo;
    private readonly IThemeService _themeService;

    public MainWindowViewModel(IPlatformInfoProvider platformInfo, IThemeService themeService)
    {
        _platformInfo = platformInfo;
        _themeService = themeService;

        Sidebar = new SidebarViewModel(platformInfo.PlatformName, platformInfo.CliName);
    }

    /// <summary>Design-time constructor; wires the concrete stub services for the previewer.</summary>
    public MainWindowViewModel()
        : this(new PlatformInfoProvider(), new ThemeService())
    {
    }

    public SidebarViewModel Sidebar { get; }

    /// <summary>Probes the container CLI for its version. Call once after construction on the UI thread.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _platformInfo.RefreshCliInfoAsync(cancellationToken).ConfigureAwait(true);
        Sidebar.UpdateCliStatus(_platformInfo.IsCliReachable, _platformInfo.CliVersion);
    }

    [RelayCommand]
    private void SetTheme(AppTheme theme) => _themeService.SetTheme(theme);
}
