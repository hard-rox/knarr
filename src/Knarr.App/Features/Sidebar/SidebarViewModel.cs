using Knarr.App.Controls;
using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.Settings;
using Knarr.App.Services;
using Knarr.Service.Exceptions;
using Knarr.Service.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Sidebar;

public partial class SidebarViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<SidebarViewModel> _logger;
    private readonly IAutoRefreshService? _autoRefresh;
    private readonly NavigationItem _containersItem;
    private readonly NavigationItem _imagesItem;

    // Only registered on macOS; stays null elsewhere and the sidebar's system control is hidden.
    private readonly IContainerSystemService? _systemService;

    private IDisposable? _badgeSubscription;

    public SidebarViewModel(
        IServiceProvider services,
        IContainerCliProvider cliProvider,
        ILogger<SidebarViewModel> logger,
        IAutoRefreshService? autoRefresh = null)
    {
        _services = services;
        _cliProvider = cliProvider;
        _logger = logger;
        _autoRefresh = autoRefresh;
        _systemService = services?.GetService<IContainerSystemService>();

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

    public SidebarViewModel()
        : this(null!, null!, NullLogger<SidebarViewModel>.Instance)
    {
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSidebarCollapsed))]
    [NotifyPropertyChangedFor(nameof(IsCollapsedSystemControlVisible))]
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

    public bool IsSystemControlVisible => _systemService is not null;

    public bool IsCollapsedSystemControlVisible => IsSystemControlVisible && IsSidebarCollapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSystemRunning))]
    [NotifyPropertyChangedFor(nameof(SystemStatusText))]
    [NotifyPropertyChangedFor(nameof(SystemPillStatus))]
    [NotifyPropertyChangedFor(nameof(SystemActionIcon))]
    [NotifyPropertyChangedFor(nameof(SystemActionTooltip))]
    private ContainerSystemState _systemState = ContainerSystemState.Unknown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SystemStatusText))]
    [NotifyPropertyChangedFor(nameof(SystemPillStatus))]
    private string? _systemErrorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleSystemCommand))]
    [NotifyPropertyChangedFor(nameof(SystemStatusText))]
    private bool _isSystemBusy;

    public bool IsSystemRunning => SystemState is ContainerSystemState.Running;

    public string SystemStatusText
    {
        get
        {
            if (IsSystemBusy)
            {
                return IsSystemRunning ? "Stopping\u2026" : "Starting\u2026";
            }

            if (SystemErrorMessage is not null)
            {
                return "Error";
            }

            return SystemState switch
            {
                ContainerSystemState.Running => "System running",
                ContainerSystemState.Unregistered => "System unregistered",
                ContainerSystemState.NotRunning => "System stopped",
                _ => "System unknown",
            };
        }
    }

    public PillStatus SystemPillStatus
    {
        get
        {
            if (SystemErrorMessage is not null)
            {
                return PillStatus.Paused;
            }

            return SystemState switch
            {
                ContainerSystemState.Running => PillStatus.Running,
                ContainerSystemState.Unregistered or ContainerSystemState.NotRunning => PillStatus.Stopped,
                _ => PillStatus.Neutral,
            };
        }
    }

    public string SystemActionIcon => IsSystemRunning ? "StopRegular" : "PlayRegular";

    public string SystemActionTooltip => SystemErrorMessage
        ?? (IsSystemRunning ? "Stop the container system" : "Start the container system");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        PlatformInfo info = await _cliProvider.GetPlatformInfoAsync(cancellationToken).ConfigureAwait(true);
        PlatformName = info.PlatformName;
        CliName = info.CliName;
        CliVersion = info.CliVersion;
        IsCliReachable = info.IsCliReachable;

        await RefreshBadgeCountsAsync(cancellationToken).ConfigureAwait(true);
        _badgeSubscription ??= _autoRefresh?.Subscribe(RefreshBadgeCountsAsync);
    }

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

        await RefreshSystemStatusAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task RefreshSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_systemService is null || IsSystemBusy)
        {
            return;
        }

        ContainerSystemStatus status = await _systemService.GetStatusAsync(cancellationToken).ConfigureAwait(true);
        SystemState = status.State;
    }

    [RelayCommand(CanExecute = nameof(CanToggleSystem))]
    private async Task ToggleSystemAsync(CancellationToken cancellationToken)
    {
        if (_systemService is null)
        {
            return;
        }

        IsSystemBusy = true;
        SystemErrorMessage = null;

        try
        {
            if (IsSystemRunning)
            {
                await _systemService.StopAsync(cancellationToken).ConfigureAwait(true);
            }
            else
            {
                await _systemService.StartAsync(cancellationToken).ConfigureAwait(true);
            }
        }
        catch (CliCommandException ex)
        {
            _logger.LogWarning(ex, "Container system toggle failed");
            SystemErrorMessage = ex.Message;
        }
        finally
        {
            IsSystemBusy = false;
        }

        await RefreshSystemStatusAsync(cancellationToken).ConfigureAwait(true);
    }

    private bool CanToggleSystem() => _systemService is not null && !IsSystemBusy;

    partial void OnSelectedItemChanged(NavigationItem? value)
        => _logger.LogInformation("Navigated to {Page}", value?.Title ?? "(none)");

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
