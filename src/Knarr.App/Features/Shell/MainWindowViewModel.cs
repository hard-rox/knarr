using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Common;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;

namespace Knarr.App.Features.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPlatformInfoProvider _platformInfo;
    private readonly IThemeService _themeService;

    public MainWindowViewModel(IPlatformInfoProvider platformInfo, IThemeService themeService)
    {
        _platformInfo = platformInfo;
        _themeService = themeService;

        Sidebar = new SidebarViewModel(platformInfo.PlatformName, platformInfo.CliName);
        Sidebar.PropertyChanged += OnSidebarPropertyChanged;
        CurrentPage = Sidebar.SelectedItem?.CreatePage?.Invoke();
    }

    /// <summary>Design-time constructor; wires the concrete stub services for the previewer.</summary>
    public MainWindowViewModel()
        : this(new PlatformInfoProvider(), new ThemeService())
    {
    }

    public SidebarViewModel Sidebar { get; }

    /// <summary>The page view model rendered in the content area; swaps when the sidebar selection changes.</summary>
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>Probes the container CLI for its version. Call once after construction on the UI thread.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _platformInfo.RefreshCliInfoAsync(cancellationToken).ConfigureAwait(true);
        Sidebar.UpdateCliStatus(_platformInfo.IsCliReachable, _platformInfo.CliVersion);
    }

    [RelayCommand]
    private void SetTheme(AppTheme theme) => _themeService.SetTheme(theme);

    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.SelectedItem))
        {
            CurrentPage = Sidebar.SelectedItem?.CreatePage?.Invoke();
        }
    }
}
