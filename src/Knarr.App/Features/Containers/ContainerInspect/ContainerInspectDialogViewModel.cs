using System.Globalization;
using Knarr.App.Controls;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Containers.ContainerInspect;

public partial class ContainerInspectDialogViewModel(
    IContainerCliProvider cliProvider,
    ILogger<ContainerInspectDialogViewModel> logger)
    : ViewModelBase, IDialogViewModel
{
    private const string Dash = "\u2014";

    private readonly IContainerCliProvider? _cliProvider = cliProvider;
    private string _containerId = string.Empty;
    private CancellationTokenSource? _cts;

    public ContainerInspectDialogViewModel()
        : this(null!, NullLogger<ContainerInspectDialogViewModel>.Instance)
    {
    }

    public event EventHandler? CloseRequested;

    [ObservableProperty] public partial string ContainerName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInspection))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasInspection))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInspection))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusPillStatus))]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(FullId))]
    [NotifyPropertyChangedFor(nameof(ImageText))]
    [NotifyPropertyChangedFor(nameof(CreatedText))]
    [NotifyPropertyChangedFor(nameof(StartedText))]
    [NotifyPropertyChangedFor(nameof(FinishedText))]
    [NotifyPropertyChangedFor(nameof(ExitCodeText))]
    [NotifyPropertyChangedFor(nameof(EntrypointText))]
    [NotifyPropertyChangedFor(nameof(CommandText))]
    [NotifyPropertyChangedFor(nameof(WorkingDirectoryText))]
    [NotifyPropertyChangedFor(nameof(UserText))]
    [NotifyPropertyChangedFor(nameof(NetworkModeText))]
    [NotifyPropertyChangedFor(nameof(MemoryText))]
    [NotifyPropertyChangedFor(nameof(NanoCpusText))]
    [NotifyPropertyChangedFor(nameof(Ports))]
    [NotifyPropertyChangedFor(nameof(Networks))]
    [NotifyPropertyChangedFor(nameof(EnvironmentVariables))]
    [NotifyPropertyChangedFor(nameof(Labels))]
    [NotifyPropertyChangedFor(nameof(HasPorts))]
    [NotifyPropertyChangedFor(nameof(HasNetworks))]
    [NotifyPropertyChangedFor(nameof(HasEnvironmentVariables))]
    [NotifyPropertyChangedFor(nameof(HasLabels))]
    [NotifyPropertyChangedFor(nameof(RawJson))]
    public partial ContainerInspection? Inspection { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasInspection => !IsLoading && !HasError && Inspection is not null;

    public string StatusText => Text(Inspection?.Status);

    public PillStatus StatusPillStatus => Inspection?.Status?.ToLowerInvariant() switch
    {
        "running" => PillStatus.Running,
        "exited" or "stopped" => PillStatus.Stopped,
        _ => PillStatus.Neutral,
    };

    public bool IsRunning => Inspection?.IsRunning ?? false;

    public string FullId => Text(Inspection?.Id);
    public string ImageText => Text(Inspection?.Image);
    public string EntrypointText => JoinArgs(Inspection?.Entrypoint);
    public string CommandText => JoinArgs(Inspection?.Command);
    public string WorkingDirectoryText => Text(Inspection?.WorkingDirectory);
    public string UserText => Text(Inspection?.User);
    public string NetworkModeText => Text(Inspection?.NetworkMode);

    public string ExitCodeText => Inspection is { } i
        ? i.ExitCode.ToString(CultureInfo.InvariantCulture)
        : Dash;

    public string MemoryText => Inspection is { MemoryBytes: > 0 } i
        ? FormatBytes(i.MemoryBytes)
        : Dash;

    public string NanoCpusText => Inspection is { NanoCpus: > 0 } i
        ? $"{i.NanoCpus / 1_000_000_000.0:0.##} vCPU"
        : Dash;

    public string CreatedText => FormatDate(Inspection?.Created);
    public string StartedText => FormatDate(Inspection?.StartedAt);
    public string FinishedText => Inspection?.IsRunning == false ? FormatDate(Inspection?.FinishedAt) : Dash;

    public IReadOnlyList<string> Ports => Inspection?.Ports ?? [];
    public IReadOnlyList<ContainerNetworkInfo> Networks => Inspection?.Networks ?? [];
    public IReadOnlyList<InspectionEntry> EnvironmentVariables => Inspection?.EnvironmentVariables ?? [];
    public IReadOnlyList<InspectionEntry> Labels => Inspection?.Labels ?? [];

    public bool HasPorts => Ports.Count > 0;
    public bool HasNetworks => Networks.Count > 0;
    public bool HasEnvironmentVariables => EnvironmentVariables.Count > 0;
    public bool HasLabels => Labels.Count > 0;

    public string RawJson => Inspection?.RawJson ?? string.Empty;

    public void Reset(string containerId, string containerName)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _containerId = containerId;
        ContainerName = containerName;
        Inspection = null;
        ErrorMessage = null;
        _ = LoadAsync();
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private void Close()
    {
        _cts?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadAsync()
    {
        if (_cliProvider is null || string.IsNullOrWhiteSpace(_containerId))
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Inspection = await _cliProvider.InspectContainerAsync(_containerId, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Container inspect canceled for {Id}", _containerId);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            logger.LogError(ex, "Container inspect failed for {Id}", _containerId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string Text(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Dash : value;

    private static string JoinArgs(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? string.Join(' ', values) : Dash;

    private static string FormatDate(DateTimeOffset? dt) =>
        dt is { } d ? d.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture) : Dash;

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }
}
