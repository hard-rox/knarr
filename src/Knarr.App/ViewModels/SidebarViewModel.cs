using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Models;

namespace Knarr.App.ViewModels;

public partial class SidebarViewModel : ViewModelBase
{
    public SidebarViewModel(string platformName, string cliName)
    {
        PlatformName = platformName;
        CliName = cliName;

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

        SelectedItem = NavigationItems[0];
    }

    /// <summary>Design-time constructor with sample platform information.</summary>
    public SidebarViewModel()
        : this("Windows", "wslc")
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

    public void UpdateCliStatus(bool isCliReachable, string cliVersion)
    {
        IsCliReachable = isCliReachable;
        CliVersion = cliVersion;
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
