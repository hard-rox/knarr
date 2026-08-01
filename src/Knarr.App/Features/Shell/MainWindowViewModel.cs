using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(IThemeService themeService, SidebarViewModel sidebar,
        ILogger<MainWindowViewModel> logger)
    {
        _themeService = themeService;
        _logger = logger;

        Sidebar = sidebar;
        Sidebar.PropertyChanged += OnSidebarPropertyChanged;
        CurrentPage = Sidebar.SelectedItem?.CreatePage?.Invoke();
    }

    public MainWindowViewModel()
        : this(new ThemeService(), new SidebarViewModel(), NullLogger<MainWindowViewModel>.Instance)
    {
    }

    public SidebarViewModel Sidebar { get; }

    [ObservableProperty] private ViewModelBase? _currentPage;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Sidebar.InitializeAsync(cancellationToken);

    [RelayCommand]
    private void SetTheme(AppTheme theme) => _themeService.SetTheme(theme);

    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SidebarViewModel.SelectedItem)) return;
        // Dispose the outgoing page so its background work (e.g. auto-refresh timers) stops.
        if (CurrentPage is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CurrentPage = Sidebar.SelectedItem?.CreatePage?.Invoke();
    }
}
