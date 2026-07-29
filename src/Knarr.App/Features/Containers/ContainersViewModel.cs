using Avalonia.Threading;
using Knarr.App.Features.Containers.ContainerLogs;
using Knarr.App.Features.RunContainer;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Container = Knarr.Service.Models.Container;

namespace Knarr.App.Features.Containers;

public partial class ContainersViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(5);

    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<ContainersViewModel> _logger;
    private readonly Func<RunContainerDialogViewModel>? _runDialogFactory;
    private readonly Func<ContainerLogsDialogViewModel>? _logsDialogFactory;
    private readonly List<ContainerItem> _allContainers = [];

    private DispatcherTimer? _refreshTimer;
    private bool _loadInFlight;

    public ContainersViewModel(
        IContainerCliProvider cliProvider,
        ILogger<ContainersViewModel> logger,
        Func<RunContainerDialogViewModel>? runDialogFactory = null,
        Func<ContainerLogsDialogViewModel>? logsDialogFactory = null)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        _runDialogFactory = runDialogFactory;
        _logsDialogFactory = logsDialogFactory;
        Containers = new ObservableCollection<ContainerItem>();

        // Kick off the initial load; property updates marshal back to the UI thread.
        _ = LoadAsync();
        StartAutoRefresh();
    }

    public ContainersViewModel()
    {
        _cliProvider = null!;
        _logger = NullLogger<ContainersViewModel>.Instance;
        Containers = new ObservableCollection<ContainerItem>();
    }

    public ObservableCollection<ContainerItem> Containers { get; }

    public event EventHandler<RunContainerDialogViewModel>? RunDialogRequested;

    public event EventHandler<ContainerLogsDialogViewModel>? LogsDialogRequested;

    public int TotalCount => _allContainers.Count;

    public int RunningCount => _allContainers.Count(c => c.Status == ContainerState.Running);

    public int StoppedCount => _allContainers.Count(c => c.Status == ContainerState.Exited);

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ContainerItem? _selectedContainer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasItems => !IsLoading && !HasError && Containers.Count > 0;

    public bool IsEmpty => !IsLoading && !HasError && _allContainers.Count == 0;

    public bool HasNoResults => !IsLoading && !HasError && _allContainers.Count > 0 && Containers.Count == 0;

    public IReadOnlyList<ContainerItem> SelectedContainers =>
        Containers.Where(c => c.IsSelected).ToList();

    public int SelectedCount => Containers.Count(c => c.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    public bool? AllSelected
    {
        get
        {
            if (Containers.Count == 0)
            {
                return false;
            }

            int selected = SelectedCount;
            if (selected == 0)
            {
                return false;
            }

            return selected == Containers.Count ? true : null;
        }
        set
        {
            // A null assignment comes from the indeterminate state; treat it as "select all".
            bool target = value ?? true;
            foreach (ContainerItem container in Containers)
            {
                container.IsSelected = target;
            }
        }
    }

    private void OnContainerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContainerItem.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedContainers));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(AllSelected));
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ContainerItem> filtered = _allContainers;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string term = SearchText.Trim();
            filtered = _allContainers.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Image.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Containers.Clear();
        foreach (ContainerItem container in filtered)
        {
            Containers.Add(container);
        }

        OnPropertyChanged(nameof(SelectedContainers));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(AllSelected));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoResults));
    }

    // Concurrent calls are coalesced; showLoading=false (background auto-refresh) skips the loading
    // flag so the table stays visible without flicker.
    private async Task LoadAsync(bool showLoading = true, CancellationToken cancellationToken = default)
    {
        if (_loadInFlight)
        {
            return;
        }

        _loadInFlight = true;
        if (showLoading)
        {
            IsLoading = true;
        }

        ErrorMessage = null;
        try
        {
            IReadOnlyList<Container> summaries = await _cliProvider
                .ListContainersAsync(cancellationToken)
                .ConfigureAwait(true);

            foreach (ContainerItem existing in _allContainers)
            {
                existing.PropertyChanged -= OnContainerPropertyChanged;
            }

            _allContainers.Clear();
            foreach (Container summary in summaries)
            {
                ContainerItem item = new(summary);
                item.PropertyChanged += OnContainerPropertyChanged;
                _allContainers.Add(item);
            }

            ApplyFilter();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(RunningCount));
            OnPropertyChanged(nameof(StoppedCount));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to load containers");
        }
        finally
        {
            if (showLoading)
            {
                IsLoading = false;
            }

            _loadInFlight = false;
        }
    }

    // Lifecycle commands — each maps 1:1 onto a CLI invocation via the provider, then reloads.
    [RelayCommand]
    private Task Refresh()
    {
        _logger.LogInformation("Manual containers refresh requested");
        return LoadAsync();
    }

    [RelayCommand]
    private void RunContainer()
    {
        if (_runDialogFactory is null)
        {
            return;
        }

        RunContainerDialogViewModel dialogViewModel = _runDialogFactory();
        dialogViewModel.Reset(initialImage: null, imageEditable: true);
        dialogViewModel.ContainerStarted += OnContainerStarted;
        RunDialogRequested?.Invoke(this, dialogViewModel);
    }

    private void OnContainerStarted(object? sender, EventArgs e) => _ = LoadAsync();

    // Bulk (multiselect) commands — the provider runs each batch as a single command session.
    [RelayCommand]
    private Task StartSelected()
    {
        List<string> ids = SelectedContainers.Select(c => c.Id).ToList();
        return ids.Count == 0
            ? Task.CompletedTask
            : ExecuteAndReloadAsync(ct => _cliProvider.StartContainersAsync(ids, ct));
    }

    [RelayCommand]
    private Task StopSelected()
    {
        List<string> ids = SelectedContainers.Select(c => c.Id).ToList();
        return ids.Count == 0
            ? Task.CompletedTask
            : ExecuteAndReloadAsync(ct => _cliProvider.StopContainersAsync(ids, ct));
    }

    [RelayCommand]
    private Task DeleteSelected()
    {
        List<string> ids = SelectedContainers.Select(c => c.Id).ToList();
        return ids.Count == 0
            ? Task.CompletedTask
            : ExecuteAndReloadAsync(ct => _cliProvider.RemoveContainersAsync(ids, force: true, ct));
    }

    [RelayCommand]
    private Task Start(ContainerItem container)
        => ExecuteAndReloadAsync(ct => _cliProvider.StartContainerAsync(container.Id, ct));

    [RelayCommand]
    private Task Stop(ContainerItem container)
        => ExecuteAndReloadAsync(ct => _cliProvider.StopContainerAsync(container.Id, ct));

    [RelayCommand]
    private Task Restart(ContainerItem container)
        => ExecuteAndReloadAsync(ct => _cliProvider.RestartContainerAsync(container.Id, ct));

    [RelayCommand]
    private Task Remove(ContainerItem container)
        => ExecuteAndReloadAsync(ct => _cliProvider.RemoveContainerAsync(container.Id, force: true, ct));

    [RelayCommand]
    private void Logs(ContainerItem container)
    {
        if (_logsDialogFactory is null)
        {
            return;
        }

        ContainerLogsDialogViewModel dialogViewModel = _logsDialogFactory();
        dialogViewModel.Reset(container.Id, container.Name);
        LogsDialogRequested?.Invoke(this, dialogViewModel);
    }

    [RelayCommand]
    private void Exec(ContainerItem container)
    {
        // Exec terminal is a later milestone.
    }

    [RelayCommand]
    private void Inspect(ContainerItem container)
    {
        // Inspect viewer is a later milestone.
    }

    private async Task ExecuteAndReloadAsync(Func<CancellationToken, Task> action)
    {
        try
        {
            await action(CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Container action failed");
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private void StartAutoRefresh()
    {
        if (_refreshTimer is not null)
        {
            return;
        }

        _refreshTimer = new DispatcherTimer { Interval = _refreshInterval };
        _refreshTimer.Tick += async (_, _) => await LoadAsync(showLoading: false).ConfigureAwait(true);
        _refreshTimer.Start();
        _logger.LogDebug("Containers auto-refresh started ({Interval}s)", _refreshInterval.TotalSeconds);
    }

    public void Dispose()
    {
        if (_refreshTimer is not null)
        {
            _refreshTimer.Stop();
            _refreshTimer = null;
            _logger.LogDebug("Containers auto-refresh stopped");
        }

        foreach (ContainerItem item in _allContainers)
        {
            item.PropertyChanged -= OnContainerPropertyChanged;
        }

        GC.SuppressFinalize(this);
    }
}
