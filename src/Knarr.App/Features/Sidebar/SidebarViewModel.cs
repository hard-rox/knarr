using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Common;
using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Settings;
using Knarr.App.Models;
using Knarr.App.Services;

namespace Knarr.App.Features.Sidebar;

public partial class SidebarViewModel : ViewModelBase
{
    private readonly IPlatformInfoProvider _platformInfo;

    public SidebarViewModel(IPlatformInfoProvider platformInfo)
    {
        _platformInfo = platformInfo;
        PlatformName = platformInfo.PlatformName;
        CliName = platformInfo.CliName;

        NavigationItems =
        [
            new NavigationItem("Dashboard", "board_regular", createPage: () => new DashboardViewModel()),
            new NavigationItem("Containers", "cube_regular", "4", createPage: () => new ContainersViewModel()),
            new NavigationItem("Images", "cloud_regular", "7"),
            new NavigationItem("Networks", "globe_regular", "3"),
            new NavigationItem("Volumes", "storage_regular", "5"),
            new NavigationItem("Registries", "library_regular"),
            new NavigationItem("Settings", "settings_regular", createPage: () => new SettingsViewModel()),
        ];

        SelectedItem = NavigationItems[0];
    }

    /// <summary>Design-time constructor with sample platform information.</summary>
    public SidebarViewModel()
        : this(new PlatformInfoProvider())
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
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
