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
            },
            new ContainerItem
            {
                Name = "rabbitmq-broker",
                Id = "aa11bb22cc33",
                Image = "rabbitmq:3-management",
                Status = ContainerStatus.Running,
                Ports = "5672\u21925672",
                Cpu = "4%",
                Memory = "256MB",
                Uptime = "5h 02m"
            },
            new ContainerItem
            {
                Name = "mongo-primary",
                Id = "dd44ee55ff66",
                Image = "mongo:7",
                Status = ContainerStatus.Running,
                Ports = "27017\u219227017",
                Cpu = "11%",
                Memory = "768MB",
                Uptime = "1d 3h"
            },
            new ContainerItem
            {
                Name = "elasticsearch",
                Id = "77gg88hh99ii",
                Image = "elasticsearch:8.13.0",
                Status = ContainerStatus.Running,
                Ports = "9200\u21929200",
                Cpu = "18%",
                Memory = "1.5GB",
                Uptime = "6h 41m"
            },
            new ContainerItem
            {
                Name = "kibana-ui",
                Id = "0a1b2c3d4e5f",
                Image = "kibana:8.13.0",
                Status = ContainerStatus.Running,
                Ports = "5601\u21925601",
                Cpu = "5%",
                Memory = "420MB",
                Uptime = "6h 40m"
            },
            new ContainerItem
            {
                Name = "grafana-dash",
                Id = "5f4e3d2c1b0a",
                Image = "grafana/grafana:10.4.0",
                Status = ContainerStatus.Running,
                Ports = "3000\u21923000",
                Cpu = "2%",
                Memory = "192MB",
                Uptime = "12h 08m"
            },
            new ContainerItem
            {
                Name = "prometheus",
                Id = "abcabc123123",
                Image = "prom/prometheus:v2.51.0",
                Status = ContainerStatus.Running,
                Ports = "9090\u21929090",
                Cpu = "7%",
                Memory = "310MB",
                Uptime = "12h 09m"
            },
            new ContainerItem
            {
                Name = "minio-storage",
                Id = "321321cbacba",
                Image = "minio/minio:latest",
                Status = ContainerStatus.Running,
                Ports = "9000\u21929000",
                Cpu = "3%",
                Memory = "148MB",
                Uptime = "3h 55m"
            },
            new ContainerItem
            {
                Name = "mysql-db",
                Id = "9f8e7d6c5b4a",
                Image = "mysql:8.4",
                Status = ContainerStatus.Running,
                Ports = "3306\u21923306",
                Cpu = "8%",
                Memory = "540MB",
                Uptime = "1d 6h"
            },
            new ContainerItem
            {
                Name = "keycloak-auth",
                Id = "4a5b6c7d8e9f",
                Image = "quay.io/keycloak/keycloak:24.0",
                Status = ContainerStatus.Exited
            },
            new ContainerItem
            {
                Name = "vault-secrets",
                Id = "beadfeedbead",
                Image = "hashicorp/vault:1.16",
                Status = ContainerStatus.Running,
                Ports = "8200\u21928200",
                Cpu = "1%",
                Memory = "96MB",
                Uptime = "8h 22m"
            },
            new ContainerItem
            {
                Name = "nats-streaming",
                Id = "feedfacefeed",
                Image = "nats:2.10-alpine",
                Status = ContainerStatus.Running,
                Ports = "4222\u21924222",
                Cpu = "2%",
                Memory = "72MB",
                Uptime = "9h 17m"
            },
            new ContainerItem
            {
                Name = "mailhog-smtp",
                Id = "cafebabecafe",
                Image = "mailhog/mailhog:v1.0.1",
                Status = ContainerStatus.Running,
                Ports = "8025\u21928025",
                Cpu = "1%",
                Memory = "40MB",
                Uptime = "2h 03m"
            },
            new ContainerItem
            {
                Name = "traefik-proxy",
                Id = "10ff20ee30dd",
                Image = "traefik:v3.0",
                Status = ContainerStatus.Running,
                Ports = "443\u2192443",
                Cpu = "4%",
                Memory = "88MB",
                Uptime = "1d 1h"
            },
            new ContainerItem
            {
                Name = "jaeger-tracing",
                Id = "40cc50bb60aa",
                Image = "jaegertracing/all-in-one:1.56",
                Status = ContainerStatus.Exited
            },
            new ContainerItem
            {
                Name = "adminer-ui",
                Id = "70990088aa11",
                Image = "adminer:4.8.1",
                Status = ContainerStatus.Paused,
                Ports = "8081\u21928080",
                Cpu = "0%",
                Memory = "36MB",
                Uptime = "4h 30m"
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
