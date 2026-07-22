using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(IThemeService themeService, SidebarViewModel sidebar, ILogger<MainWindowViewModel> logger)
    {
        _themeService = themeService;
        _logger = logger;

        Sidebar = sidebar;
        Sidebar.PropertyChanged += OnSidebarPropertyChanged;
        CurrentPage = Sidebar.SelectedItem?.CreatePage?.Invoke();
    }

    /// <summary>Design-time constructor; wires the concrete stub services for the previewer.</summary>
    public MainWindowViewModel()
        : this(new ThemeService(), new SidebarViewModel(), NullLogger<MainWindowViewModel>.Instance)
    {
    }

    public SidebarViewModel Sidebar { get; }

    /// <summary>The page view model rendered in the content area; swaps when the sidebar selection changes.</summary>
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>Probes the container CLI for its version. Call once after construction on the UI thread.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Sidebar.InitializeAsync(cancellationToken);

    [RelayCommand]
    private void SetTheme(AppTheme theme) => _themeService.SetTheme(theme);

    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.SelectedItem))
        {
            // Dispose the outgoing page so its background work (e.g. auto-refresh timers) stops.
            if (CurrentPage is IDisposable disposable)
            {
                disposable.Dispose();
            }

            CurrentPage = Sidebar.SelectedItem?.CreatePage?.Invoke();
        }
    }
}
