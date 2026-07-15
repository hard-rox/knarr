using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Models;
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

        PlatformName = platformInfo.PlatformName;
        CliName = platformInfo.CliName;

        NavigationItems =
        [
            new NavigationItem("Dashboard", "board_regular"),
            new NavigationItem("Containers", "cube_regular", "4"),
            new NavigationItem("Images", "cloud_regular", "7"),
            new NavigationItem("Networks", "globe_regular", "3"),
            new NavigationItem("Volumes", "storage_regular", "5"),
            new NavigationItem("Registries", "library_regular"),
            new NavigationItem("Settings", "settings_regular"),
        ];

        _selectedItem = NavigationItems[0];
    }

    /// <summary>Design-time constructor; wires the concrete stub services for the previewer.</summary>
    public MainWindowViewModel()
        : this(new PlatformInfoProvider(), new ThemeService())
    {
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    public string PlatformName { get; }

    public string CliName { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CliDisplay))]
    private string _cliVersion = "detecting\u2026";

    [ObservableProperty]
    private bool _isCliReachable;

    public string CliDisplay => $"{CliName} {CliVersion}";

    /// <summary>Probes the container CLI for its version. Call once after construction on the UI thread.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _platformInfo.RefreshCliInfoAsync(cancellationToken).ConfigureAwait(true);
        IsCliReachable = _platformInfo.IsCliReachable;
        CliVersion = _platformInfo.CliVersion;
    }

    [RelayCommand]
    private void SetTheme(AppTheme theme) => _themeService.SetTheme(theme);

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
