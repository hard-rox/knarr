using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.Settings;
using Knarr.Service.Models;

namespace Knarr.App.Features.Sidebar;

public partial class SidebarViewModel : ViewModelBase
{
    private readonly IContainerCliProvider _cliProvider;

    public SidebarViewModel(IContainerCliProvider cliProvider)
    {
        _cliProvider = cliProvider;

        NavigationItems =
        [
            new NavigationItem("Dashboard", "BoardRegular", createPage: () => new DashboardViewModel()),
            new NavigationItem("Containers", "CubeRegular", "4", createPage: () => new ContainersViewModel(cliProvider)),
            new NavigationItem("Images", "CloudRegular", "7", createPage: () => new ImagesViewModel(cliProvider)),
            new NavigationItem("Networks", "GlobeRegular", "3"),
            new NavigationItem("Volumes", "StorageRegular", "5"),
            new NavigationItem("Registries", "LibraryRegular"),
            new NavigationItem("Settings", "SettingsRegular", createPage: () => new SettingsViewModel()),
        ];

        SelectedItem = NavigationItems[0];
    }

    /// <summary>Design-time constructor; renders navigation without a container CLI.</summary>
    public SidebarViewModel()
        : this(null!)
    {
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private string _platformName = "Windows";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CliDisplay))]
    private string _cliName = "wslc";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CliDisplay))]
    private string _cliVersion = "detecting\u2026";

    [ObservableProperty]
    private bool _isCliReachable;

    public string CliDisplay => $"{CliName} {CliVersion}";

    /// <summary>Probes the container CLI for its version. Call once after construction on the UI thread.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        PlatformInfo info = await _cliProvider.GetPlatformInfoAsync(cancellationToken).ConfigureAwait(true);
        PlatformName = info.PlatformName;
        CliName = info.CliName;
        CliVersion = info.CliVersion;
        IsCliReachable = info.IsCliReachable;
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
