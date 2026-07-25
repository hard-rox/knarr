using Avalonia.Threading;
using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.Settings;
using Knarr.Service.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Sidebar;

public partial class SidebarViewModel : ViewModelBase
{
    private static readonly TimeSpan _badgeRefreshInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _services;
    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<SidebarViewModel> _logger;
    private readonly NavigationItem _containersItem;
    private readonly NavigationItem _imagesItem;

    private DispatcherTimer? _badgeTimer;

    public SidebarViewModel(
        IServiceProvider services,
        IContainerCliProvider cliProvider,
        ILogger<SidebarViewModel> logger)
    {
        _services = services;
        _cliProvider = cliProvider;
        _logger = logger;

        _containersItem = new NavigationItem(
            "Containers", "CubeRegular", createPage: () => _services.GetRequiredService<ContainersViewModel>());
        _imagesItem = new NavigationItem(
            "Images", "CloudRegular", createPage: () => _services.GetRequiredService<ImagesViewModel>());

        NavigationItems =
        [
            new NavigationItem("Dashboard", "BoardRegular", createPage: () => _services.GetRequiredService<DashboardViewModel>()),
            _containersItem,
            _imagesItem,
            new NavigationItem("Networks", "GlobeRegular", "3"),
            new NavigationItem("Volumes", "StorageRegular", "5"),
            new NavigationItem("Registries", "LibraryRegular"),
            new NavigationItem("Settings", "SettingsRegular", createPage: () => _services.GetRequiredService<SettingsViewModel>()),
        ];

        SelectedItem = NavigationItems[0];
    }

    /// <summary>Design-time constructor; renders navigation without a container CLI.</summary>
    public SidebarViewModel()
        : this(null!, null!, NullLogger<SidebarViewModel>.Instance)
    {
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSidebarCollapsed))]
    private bool _isSidebarExpanded = true;

    public bool IsSidebarCollapsed => !IsSidebarExpanded;

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

        await RefreshBadgeCountsAsync(cancellationToken).ConfigureAwait(true);
        StartBadgeRefresh();
    }

    /// <summary>Starts the periodic refresh of the container/image count badges.</summary>
    private void StartBadgeRefresh()
    {
        if (_badgeTimer is not null)
        {
            return;
        }

        _badgeTimer = new DispatcherTimer { Interval = _badgeRefreshInterval };
        _badgeTimer.Tick += async (_, _) => await RefreshBadgeCountsAsync().ConfigureAwait(true);
        _badgeTimer.Start();
    }

    /// <summary>Refreshes the Containers and Images badge counts from the CLI. Never throws.</summary>
    private async Task RefreshBadgeCountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<Knarr.Service.Models.Container> containers = await _cliProvider.ListContainersAsync(cancellationToken).ConfigureAwait(true);
            _containersItem.Badge = containers.Count > 0 ? containers.Count.ToString() : null;

            IReadOnlyList<ContainerImage> images = await _cliProvider.ListImagesAsync(cancellationToken).ConfigureAwait(true);
            _imagesItem.Badge = images.Count > 0 ? images.Count.ToString() : null;

            _logger.LogDebug("Sidebar badges updated: {Containers} containers, {Images} images", containers.Count, images.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh sidebar badge counts");
        }
    }

    partial void OnSelectedItemChanged(NavigationItem? value)
        => _logger.LogInformation("Navigated to {Page}", value?.Title ?? "(none)");

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
