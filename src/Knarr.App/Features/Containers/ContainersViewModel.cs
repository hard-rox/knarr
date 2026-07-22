using Avalonia.Threading;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Container = Knarr.Service.Models.Container;

namespace Knarr.App.Features.Containers;

/// <summary>
/// View model for the Containers feature. Presents the list of containers and exposes the
/// lifecycle actions, each of which maps 1:1 onto a single CLI command via
/// <see cref="IContainerCliProvider"/>. Data is loaded from the host container CLI.
/// </summary>
public partial class ContainersViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(5);

    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<ContainersViewModel> _logger;
    private readonly List<ContainerItem> _allContainers = [];

    private DispatcherTimer? _refreshTimer;
    private bool _loadInFlight;

    public ContainersViewModel(IContainerCliProvider cliProvider, ILogger<ContainersViewModel> logger)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        Containers = new ObservableCollection<ContainerItem>();

        // Kick off the initial load; property updates marshal back to the UI thread.
        _ = LoadAsync();
        StartAutoRefresh();
    }

    /// <summary>Design-time constructor; renders an empty list without a container CLI.</summary>
    public ContainersViewModel()
    {
        _cliProvider = null!;
        _logger = NullLogger<ContainersViewModel>.Instance;
        Containers = new ObservableCollection<ContainerItem>();
    }

    public ObservableCollection<ContainerItem> Containers { get; }

    /// <summary>Total number of containers, independent of the current search filter.</summary>
    public int TotalCount => _allContainers.Count;

    /// <summary>Number of running containers.</summary>
    public int RunningCount => _allContainers.Count(c => c.Status == ContainerState.Running);

    /// <summary>Number of stopped (exited) containers.</summary>
    public int StoppedCount => _allContainers.Count(c => c.Status == ContainerState.Exited);

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ContainerItem? _selectedContainer;

    /// <summary>True while a CLI list/refresh is in flight.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    private bool _isLoading;

    /// <summary>Message from the most recent failed CLI action, or null when the last action succeeded.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    private string? _errorMessage;

    /// <summary>True when the last CLI action failed and <see cref="ErrorMessage"/> is set.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>True when there are rows to display in the table.</summary>
    public bool HasItems => !IsLoading && !HasError && Containers.Count > 0;

    /// <summary>True when the CLI returned no containers at all (not merely filtered out).</summary>
    public bool IsEmpty => !IsLoading && !HasError && _allContainers.Count == 0;

    /// <summary>True when containers exist but the current search filter matches none.</summary>
    public bool HasNoResults => !IsLoading && !HasError && _allContainers.Count > 0 && Containers.Count == 0;

    /// <summary>Rows currently ticked for a bulk action.</summary>
    public IReadOnlyList<ContainerItem> SelectedContainers =>
        Containers.Where(c => c.IsSelected).ToList();

    public int SelectedCount => Containers.Count(c => c.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    /// <summary>
    /// Header "select all" checkbox state: true/false when uniform, null (indeterminate) when mixed.
    /// </summary>
    public bool? AllSelected
    {
        get
        {
            if (Containers.Count == 0)
            {
                return false;
            }

            var selected = SelectedCount;
            if (selected == 0)
            {
                return false;
            }

            return selected == Containers.Count ? true : null;
        }
        set
        {
            // A null assignment comes from the indeterminate state; treat it as "select all".
            var target = value ?? true;
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
            var term = SearchText.Trim();
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

    /// <summary>
    /// Loads (or reloads) the container list from the CLI. Safe to call repeatedly; concurrent
    /// calls are coalesced. When <paramref name="showLoading"/> is false (background auto-refresh)
    /// the loading indicator is not toggled, so the table stays visible without flicker. Failures
    /// are surfaced via <see cref="ErrorMessage"/> and never throw.
    /// </summary>
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
                ContainerItem item = new ContainerItem(summary);
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
        // Run wizard is a later milestone.
    }

    // Bulk (multiselect) commands — the provider runs each batch as a single command session.
    [RelayCommand]
    private Task StartSelected()
    {
        var ids = SelectedContainers.Select(c => c.Id).ToList();
        return ids.Count == 0
            ? Task.CompletedTask
            : ExecuteAndReloadAsync(ct => _cliProvider.StartContainersAsync(ids, ct));
    }

    [RelayCommand]
    private Task StopSelected()
    {
        var ids = SelectedContainers.Select(c => c.Id).ToList();
        return ids.Count == 0
            ? Task.CompletedTask
            : ExecuteAndReloadAsync(ct => _cliProvider.StopContainersAsync(ids, ct));
    }

    [RelayCommand]
    private Task DeleteSelected()
    {
        var ids = SelectedContainers.Select(c => c.Id).ToList();
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
        // Logs viewer is a later milestone.
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

    /// <summary>Runs a mutating CLI action, surfacing failures via <see cref="ErrorMessage"/>, then reloads.</summary>
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

    /// <summary>Starts the periodic background refresh of the container list.</summary>
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
