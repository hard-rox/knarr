using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Common;
using Knarr.App.Models;

namespace Knarr.App.Features.Containers;

/// <summary>
/// View model for the Containers feature. Presents the list of containers and exposes the
/// lifecycle actions that (in a later milestone) will map 1:1 onto CLI commands. For now the
/// commands are stubs and the data is sample/design data.
/// </summary>
public partial class ContainersViewModel : ViewModelBase
{
    private readonly List<ContainerItem> _allContainers;

    public ContainersViewModel()
    {
        _allContainers =
        [
            new ContainerItem
            {
                Name = "web-api",
                Id = "a1b2c3d4e5f6",
                Image = "nginx:latest",
                Status = ContainerStatus.Running,
                Ports = "8080\u219280",
                Cpu = "6%",
                Memory = "128MB",
                Uptime = "2h 14m"
            },
            new ContainerItem
            {
                Name = "postgres-db",
                Id = "f6e5d4c3b2a1",
                Image = "postgres:16",
                Status = ContainerStatus.Running,
                Ports = "5432\u21925432",
                Cpu = "9%",
                Memory = "512MB",
                Uptime = "2h 14m"
            },
            new ContainerItem
            {
                Name = "redis-cache",
                Id = "99aa88bb77cc",
                Image = "redis:7-alpine",
                Status = ContainerStatus.Running,
                Ports = "6379\u21926379",
                Cpu = "3%",
                Memory = "64MB",
                Uptime = "48m"
            },
            new ContainerItem
            {
                Name = "batch-worker",
                Id = "1234abcd5678",
                Image = "worker:dev",
                Status = ContainerStatus.Exited
            }
        ];

        Containers = new ObservableCollection<ContainerItem>(_allContainers);
        foreach (var container in _allContainers)
        {
            container.PropertyChanged += OnContainerPropertyChanged;
        }
    }

    public ObservableCollection<ContainerItem> Containers { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ContainerItem? _selectedContainer;

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
    }

    // Lifecycle commands — stubs for the design milestone; real CLI wiring lands later.
    [RelayCommand]
    private void Refresh()
    {
    }

    [RelayCommand]
    private void RunContainer()
    {
    }

    // Bulk (multiselect) commands — operate on every ticked row. Stubs for the design milestone.
    [RelayCommand]
    private void StartSelected()
    {
        foreach (var container in SelectedContainers)
        {
            Start(container);
        }
    }

    [RelayCommand]
    private void StopSelected()
    {
        foreach (var container in SelectedContainers)
        {
            Stop(container);
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        foreach (var container in SelectedContainers.ToList())
        {
            Remove(container);
        }
    }

    [RelayCommand]
    private void Start(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Stop(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Restart(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Kill(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Remove(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Logs(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Exec(ContainerItem container)
    {
    }

    [RelayCommand]
    private void Inspect(ContainerItem container)
    {
    }
}
