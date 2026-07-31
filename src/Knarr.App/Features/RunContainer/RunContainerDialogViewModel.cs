using System.Collections.Specialized;
using Knarr.App.Controls;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.RunContainer;

public partial class RunContainerDialogViewModel : ViewModelBase, IDialogViewModel
{
    private readonly IContainerCliProvider? _cliProvider;
    private readonly ILogger<RunContainerDialogViewModel> _logger;
    private CancellationTokenSource? _cts;

    public RunContainerDialogViewModel(
        IContainerCliProvider cliProvider,
        ILogger<RunContainerDialogViewModel> logger)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        EnvironmentVariables = [];
        Volumes = [];
        Ports = [];
        OutputLines = [];
        EnvironmentVariables.CollectionChanged += OnEntriesChanged;
        Volumes.CollectionChanged += OnEntriesChanged;
        Ports.CollectionChanged += OnEntriesChanged;
        UpdateCommandPreview();
    }

    public RunContainerDialogViewModel()
        : this(null!, NullLogger<RunContainerDialogViewModel>.Instance)
    {
    }

    public event EventHandler? CloseRequested;

    public event EventHandler? ContainerStarted;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    public partial string ImageReference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Detach { get; set; } = true;

    [ObservableProperty]
    public partial bool RemoveOnExit { get; set; }

    [ObservableProperty]
    public partial string ContainerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsImageEditable { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    [NotifyPropertyChangedFor(nameof(CanAddPort))]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string CommandPreview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TerminalState TerminalState { get; set; } = TerminalState.Idle;

    public ObservableCollection<EnvironmentVariableEntry> EnvironmentVariables { get; }

    public ObservableCollection<VolumeMountEntry> Volumes { get; }

    public ObservableCollection<PortMappingEntry> Ports { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddPort))]
    public partial bool PublishAllPorts { get; set; }

    public bool SupportsPublishAllPorts => _cliProvider?.SupportsPublishAllPorts ?? true;

    public bool CanAddPort => !IsRunning && (!PublishAllPorts || !SupportsPublishAllPorts);

    public ObservableCollection<CliOutputLine> OutputLines { get; }

    private bool CanRun => !IsRunning && !string.IsNullOrWhiteSpace(ImageReference);

    public void Reset(string? initialImage, bool imageEditable = true)
    {
        RequestCancellation();
        _cts = null;

        foreach (EnvironmentVariableEntry entry in EnvironmentVariables)
        {
            entry.PropertyChanged -= OnEntryPropertyChanged;
        }

        EnvironmentVariables.Clear();

        foreach (VolumeMountEntry entry in Volumes)
        {
            entry.PropertyChanged -= OnEntryPropertyChanged;
        }

        Volumes.Clear();

        foreach (PortMappingEntry entry in Ports)
        {
            entry.PropertyChanged -= OnEntryPropertyChanged;
        }

        Ports.Clear();
        OutputLines.Clear();

        ImageReference = initialImage?.Trim() ?? string.Empty;
        IsImageEditable = imageEditable;
        Detach = true;
        RemoveOnExit = false;
        PublishAllPorts = false;
        ContainerName = string.Empty;
        IsRunning = false;
        StatusMessage = null;
        TerminalState = TerminalState.Idle;

        UpdateCommandPreview();
    }

    [RelayCommand]
    private void AddEnvironmentVariable() => EnvironmentVariables.Add(new EnvironmentVariableEntry());

    [RelayCommand]
    private void RemoveEnvironmentVariable(EnvironmentVariableEntry entry) => EnvironmentVariables.Remove(entry);

    [RelayCommand]
    private void AddVolume() => Volumes.Add(new VolumeMountEntry());

    [RelayCommand]
    private void RemoveVolume(VolumeMountEntry entry) => Volumes.Remove(entry);

    [RelayCommand]
    private void AddPort() => Ports.Add(new PortMappingEntry());

    [RelayCommand]
    private void RemovePort(PortMappingEntry entry) => Ports.Remove(entry);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task Run()
    {
        if (_cliProvider is null)
        {
            return;
        }

        RunContainerOptions options = BuildOptions();

        IsRunning = true;
        StatusMessage = null;
        _cts = new CancellationTokenSource();

        if (options.Detach)
        {
            await RunDetachedAsync(options).ConfigureAwait(true);
        }
        else
        {
            await RunForegroundAsync(options).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestCancellation();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // CliWrap runs its graceful (Ctrl+C) cancellation handler synchronously inside
    // CancellationTokenSource.Cancel(); on Windows that console signalling is blocking, so cancelling
    // on the UI thread would freeze the app. Offload to a background thread instead.
    private void RequestCancellation()
    {
        CancellationTokenSource? cts = _cts;
        if (cts is null)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The run already completed and disposed the source; nothing left to cancel.
            }
        });
    }

    private async Task RunDetachedAsync(RunContainerOptions options)
    {
        bool started = false;
        try
        {
            string output = await _cliProvider!.RunContainerAsync(options, _cts!.Token).ConfigureAwait(true);
            started = true;
            string id = output.Trim();
            StatusMessage = string.IsNullOrEmpty(id) ? "Container started." : $"Started container {id}.";
            _logger.LogInformation("Ran container from {Image} (detached)", options.ImageReference);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Run canceled.";
            _logger.LogInformation("Run canceled for {Image}", options.ImageReference);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            _logger.LogError(ex, "Run failed for {Image}", options.ImageReference);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }

        if (started)
        {
            ContainerStarted?.Invoke(this, EventArgs.Empty);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task RunForegroundAsync(RunContainerOptions options)
    {
        OutputLines.Clear();
        TerminalState = TerminalState.Running;

        bool started = false;
        int? exitCode = null;
        try
        {
            await foreach (CliOutputLine line in _cliProvider!
                .RunContainerStreamingAsync(options, _cts!.Token)
                .ConfigureAwait(true))
            {
                OutputLines.Add(line);
                if (line.Kind == CliOutputKind.Exit)
                {
                    exitCode = line.ExitCode;
                }
            }

            started = true;
        }
        catch (OperationCanceledException)
        {
            TerminalState = TerminalState.Canceled;
            StatusMessage = "Run canceled.";
            _logger.LogInformation("Run canceled for {Image}", options.ImageReference);
        }
        catch (Exception ex)
        {
            TerminalState = TerminalState.Error;
            StatusMessage = ex.Message;
            _logger.LogError(ex, "Run failed for {Image}", options.ImageReference);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }

        if (started)
        {
            TerminalState = exitCode is null or 0 ? TerminalState.Success : TerminalState.Error;
            ContainerStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    private RunContainerOptions BuildOptions() => new()
    {
        ImageReference = ImageReference.Trim(),
        Detach = Detach,
        RemoveOnExit = RemoveOnExit,
        Name = string.IsNullOrWhiteSpace(ContainerName) ? null : ContainerName.Trim(),
        EnvironmentVariables =
        [
            .. EnvironmentVariables
                .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                .Select(e => new RunEnvironmentVariable(e.Key.Trim(), e.Value)),
        ],
        Volumes =
        [
            .. Volumes
                .Where(v => !string.IsNullOrWhiteSpace(v.Source) && !string.IsNullOrWhiteSpace(v.Target))
                .Select(v => new RunVolumeMount(v.Source.Trim(), v.Target.Trim())),
        ],
        Ports =
        [
            .. Ports
                .Where(p => !string.IsNullOrWhiteSpace(p.HostPort) && !string.IsNullOrWhiteSpace(p.ContainerPort))
                .Select(p => new RunPortMapping(p.HostPort.Trim(), p.ContainerPort.Trim())),
        ],
        PublishAllPorts = SupportsPublishAllPorts && PublishAllPorts,
    };

    private void UpdateCommandPreview()
        => CommandPreview = _cliProvider?.BuildRunContainerCommand(BuildOptions()) ?? string.Empty;

    partial void OnImageReferenceChanged(string value) => UpdateCommandPreview();

    partial void OnDetachChanged(bool value) => UpdateCommandPreview();

    partial void OnRemoveOnExitChanged(bool value) => UpdateCommandPreview();

    partial void OnPublishAllPortsChanged(bool value) => UpdateCommandPreview();

    partial void OnContainerNameChanged(string value) => UpdateCommandPreview();

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= OnEntryPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged += OnEntryPropertyChanged;
            }
        }

        UpdateCommandPreview();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateCommandPreview();
}
