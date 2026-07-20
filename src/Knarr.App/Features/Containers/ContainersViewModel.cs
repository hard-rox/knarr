using Knarr.Service.Models;

namespace Knarr.App.Features.Containers;

/// <summary>
/// View model for the Containers feature. Presents the list of containers and exposes the
/// lifecycle actions, each of which maps 1:1 onto a single CLI command via
/// <see cref="IContainerCliProvider"/>. Data is loaded from the host container CLI.
/// </summary>
public partial class ContainersViewModel : ViewModelBase
{
    private readonly IContainerCliProvider _cliProvider;
    private readonly List<ContainerItem> _allContainers = [];

    public ContainersViewModel(IContainerCliProvider cliProvider)
    {
        _cliProvider = cliProvider;
        Containers = new ObservableCollection<ContainerItem>();

        // Kick off the initial load; property updates marshal back to the UI thread.
        _ = LoadAsync();
    }

    /// <summary>Design-time constructor; serves sample data via the in-memory provider.</summary>
    public ContainersViewModel()
        : this(new DesignTimeContainerCliProvider())
    {
    }

    public ObservableCollection<ContainerItem> Containers { get; }

    /// <summary>Total number of containers, independent of the current search filter.</summary>
    public int TotalCount => _allContainers.Count;

    /// <summary>Number of running containers.</summary>
    public int RunningCount => _allContainers.Count(c => c.Status == ContainerStatus.Running);

    /// <summary>Number of stopped (exited) containers.</summary>
    public int StoppedCount => _allContainers.Count(c => c.Status == ContainerStatus.Exited);

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
            foreach (var container in Containers)
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
                c.Name.Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                c.Image.Contains(term, System.StringComparison.OrdinalIgnoreCase));
        }

        Containers.Clear();
        foreach (var container in filtered)
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
    /// calls are coalesced. Failures are surfaced via <see cref="ErrorMessage"/> and never throw.
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var summaries = await _cliProvider
                .ListContainersAsync(includeAll: true, cancellationToken)
                .ConfigureAwait(true);

            foreach (var existing in _allContainers)
            {
                existing.PropertyChanged -= OnContainerPropertyChanged;
            }

            _allContainers.Clear();
            foreach (var summary in summaries)
            {
                var item = new ContainerItem
                {
                    Name = summary.Name,
                    Id = summary.Id,
                    Image = summary.Image,
                    Status = summary.Status,
                    Ports = summary.Ports,
                    Cpu = summary.Cpu,
                    Memory = summary.Memory,
                    Uptime = summary.Uptime,
                };
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
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Lifecycle commands — each maps 1:1 onto a CLI invocation via the provider, then reloads.
    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private void RunContainer()
    {
        // Run wizard is a later milestone.
    }

    // Bulk (multiselect) commands — operate on every ticked row.
    [RelayCommand]
    private async Task StartSelected()
    {
        foreach (var container in SelectedContainers.ToList())
        {
            await Start(container).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task StopSelected()
    {
        foreach (var container in SelectedContainers.ToList())
        {
            await Stop(container).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        foreach (var container in SelectedContainers.ToList())
        {
            await Remove(container).ConfigureAwait(true);
        }
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
        }

        await LoadAsync().ConfigureAwait(true);
    }
}
