using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Models;

namespace Knarr.App.Services;

/// <summary>
/// An in-memory <see cref="IContainerCliProvider"/> that serves fixed sample data and treats every
/// mutation as a no-op success. Used by the Avalonia previewer (and as a convenient default in
/// tests) so the UI renders without a real container CLI on the host.
/// </summary>
public sealed class DesignTimeContainerCliProvider : IContainerCliProvider
{
    private static readonly IReadOnlyList<ContainerSummary> SampleContainers =
    [
        new() { Name = "web-api", Id = "a1b2c3d4e5f6", Image = "nginx:latest", Status = ContainerStatus.Running, Ports = "8080\u219280", Cpu = "6%", Memory = "128MB", Uptime = "2h 14m" },
        new() { Name = "postgres-db", Id = "f6e5d4c3b2a1", Image = "postgres:16", Status = ContainerStatus.Running, Ports = "5432\u21925432", Cpu = "9%", Memory = "512MB", Uptime = "2h 14m" },
        new() { Name = "redis-cache", Id = "99aa88bb77cc", Image = "redis:7-alpine", Status = ContainerStatus.Running, Ports = "6379\u21926379", Cpu = "3%", Memory = "64MB", Uptime = "48m" },
        new() { Name = "batch-worker", Id = "1234abcd5678", Image = "worker:dev", Status = ContainerStatus.Exited },
        new() { Name = "rabbitmq-broker", Id = "aa11bb22cc33", Image = "rabbitmq:3-management", Status = ContainerStatus.Running, Ports = "5672\u21925672", Cpu = "4%", Memory = "256MB", Uptime = "5h 02m" },
        new() { Name = "mongo-primary", Id = "dd44ee55ff66", Image = "mongo:7", Status = ContainerStatus.Running, Ports = "27017\u219227017", Cpu = "11%", Memory = "768MB", Uptime = "1d 3h" },
        new() { Name = "elasticsearch", Id = "77gg88hh99ii", Image = "elasticsearch:8.13.0", Status = ContainerStatus.Running, Ports = "9200\u21929200", Cpu = "18%", Memory = "1.5GB", Uptime = "6h 41m" },
        new() { Name = "kibana-ui", Id = "0a1b2c3d4e5f", Image = "kibana:8.13.0", Status = ContainerStatus.Running, Ports = "5601\u21925601", Cpu = "5%", Memory = "420MB", Uptime = "6h 40m" },
        new() { Name = "grafana-dash", Id = "5f4e3d2c1b0a", Image = "grafana/grafana:10.4.0", Status = ContainerStatus.Running, Ports = "3000\u21923000", Cpu = "2%", Memory = "192MB", Uptime = "12h 08m" },
        new() { Name = "prometheus", Id = "abcabc123123", Image = "prom/prometheus:v2.51.0", Status = ContainerStatus.Running, Ports = "9090\u21929090", Cpu = "7%", Memory = "310MB", Uptime = "12h 09m" },
        new() { Name = "minio-storage", Id = "321321cbacba", Image = "minio/minio:latest", Status = ContainerStatus.Running, Ports = "9000\u21929000", Cpu = "3%", Memory = "148MB", Uptime = "3h 55m" },
        new() { Name = "mysql-db", Id = "9f8e7d6c5b4a", Image = "mysql:8.4", Status = ContainerStatus.Running, Ports = "3306\u21923306", Cpu = "8%", Memory = "540MB", Uptime = "1d 6h" },
        new() { Name = "keycloak-auth", Id = "4a5b6c7d8e9f", Image = "quay.io/keycloak/keycloak:24.0", Status = ContainerStatus.Exited },
        new() { Name = "vault-secrets", Id = "beadfeedbead", Image = "hashicorp/vault:1.16", Status = ContainerStatus.Running, Ports = "8200\u21928200", Cpu = "1%", Memory = "96MB", Uptime = "8h 22m" },
        new() { Name = "nats-streaming", Id = "feedfacefeed", Image = "nats:2.10-alpine", Status = ContainerStatus.Running, Ports = "4222\u21924222", Cpu = "2%", Memory = "72MB", Uptime = "9h 17m" },
        new() { Name = "mailhog-smtp", Id = "cafebabecafe", Image = "mailhog/mailhog:v1.0.1", Status = ContainerStatus.Running, Ports = "8025\u21928025", Cpu = "1%", Memory = "40MB", Uptime = "2h 03m" },
        new() { Name = "traefik-proxy", Id = "10ff20ee30dd", Image = "traefik:v3.0", Status = ContainerStatus.Running, Ports = "443\u2192443", Cpu = "4%", Memory = "88MB", Uptime = "1d 1h" },
        new() { Name = "jaeger-tracing", Id = "40cc50bb60aa", Image = "jaegertracing/all-in-one:1.56", Status = ContainerStatus.Exited },
        new() { Name = "adminer-ui", Id = "70990088aa11", Image = "adminer:4.8.1", Status = ContainerStatus.Paused, Ports = "8081\u21928080", Cpu = "0%", Memory = "36MB", Uptime = "4h 30m" },
    ];

    private static readonly IReadOnlyList<ImageSummary> SampleImages =
    [
        new() { Repository = "nginx", Tag = "latest", Id = "sha256:9c7a54a9a1b2", Created = "3 days ago", Size = "187 MB" },
        new() { Repository = "postgres", Tag = "16", Id = "sha256:b1f2e3c4d5e6", Created = "1 week ago", Size = "438 MB" },
        new() { Repository = "redis", Tag = "7-alpine", Id = "sha256:44de9a0f1e2d", Created = "2 weeks ago", Size = "41 MB" },
        new() { Repository = "worker", Tag = "dev", Id = "sha256:0a11cc22bb33", Created = "1 hour ago", Size = "312 MB" },
        new() { Repository = "ubuntu", Tag = "24.04", Id = "sha256:c2d3e4f5a6b7", Created = "1 month ago", Size = "78 MB" },
    ];

    public Task<IReadOnlyList<ContainerSummary>> ListContainersAsync(
        bool includeAll = true,
        CancellationToken cancellationToken = default)
        => Task.FromResult(SampleContainers);

    public Task<IReadOnlyList<ImageSummary>> ListImagesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(SampleImages);

    public Task StartContainerAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopContainerAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RestartContainerAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task KillContainerAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string> InspectContainerAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult("{}");

    public Task PullImageAsync(string reference, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PushImageAsync(string reference, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TagImageAsync(string source, string target, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PruneImagesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<string> InspectImageAsync(string reference, CancellationToken cancellationToken = default)
        => Task.FromResult("{}");
}
