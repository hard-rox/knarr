using System.Collections.Specialized;
using Knarr.App.Controls;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.RunContainer;

/// <summary>
/// View model for the modal "run container" dialog. Collects run options (image, <c>--detach</c>,
/// <c>--rm</c>, environment variables, volume mounts, optional name), surfaces a live read-only
/// preview of the exact command that will run, and — when not detached — streams the container's
/// interactive logs into a terminal panel. Each open starts a fresh session via <see cref="Reset"/>.
/// </summary>
public partial class RunContainerDialogViewModel : ViewModelBase
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
        OutputLines = [];
        EnvironmentVariables.CollectionChanged += OnEntriesChanged;
        Volumes.CollectionChanged += OnEntriesChanged;
        UpdateCommandPreview();
    }

    /// <summary>Design-time constructor; renders the dialog without a container CLI.</summary>
    public RunContainerDialogViewModel()
        : this(null!, NullLogger<RunContainerDialogViewModel>.Instance)
    {
    }

    /// <summary>Raised when the dialog should close.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Raised after a run starts successfully so the host can refresh its lists.</summary>
    public event EventHandler? ContainerStarted;

    /// <summary>The image reference to run (e.g. <c>docker.io/library/alpine:3.20</c>).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    public partial string ImageReference { get; set; } = string.Empty;

    /// <summary>Whether the container runs in the background (<c>--detach</c>). Defaults to true.</summary>
    [ObservableProperty]
    public partial bool Detach { get; set; } = true;

    /// <summary>Whether the container is removed automatically after it exits (<c>--rm</c>).</summary>
    [ObservableProperty]
    public partial bool RemoveOnExit { get; set; }

    /// <summary>Optional container name (<c>--name</c>).</summary>
    [ObservableProperty]
    public partial string ContainerName { get; set; } = string.Empty;

    /// <summary>True while a run is in flight.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    /// <summary>Human-readable status shown to the user (success / error / canceled).</summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>The exact command that will run, recomputed live as inputs change.</summary>
    [ObservableProperty]
    public partial string CommandPreview { get; set; } = string.Empty;

    /// <summary>Drives the terminal panel accent while foreground logs stream.</summary>
    [ObservableProperty]
    public partial TerminalState TerminalState { get; set; } = TerminalState.Idle;

    /// <summary>Editable environment-variable rows; starts empty, grown via <see cref="AddEnvironmentVariableCommand"/>.</summary>
    public ObservableCollection<EnvironmentVariableEntry> EnvironmentVariables { get; }

    /// <summary>Editable volume-mount rows; starts empty, grown via <see cref="AddVolumeCommand"/>.</summary>
    public ObservableCollection<VolumeMountEntry> Volumes { get; }

    /// <summary>Live transcript of the streamed foreground run (command / stdout / stderr / exit).</summary>
    public ObservableCollection<CliOutputLine> OutputLines { get; }

    private bool CanRun => !IsRunning && !string.IsNullOrWhiteSpace(ImageReference);

    /// <summary>Resets the dialog to a fresh session, seeding the image reference.</summary>
    public void Reset(string? initialImage)
    {
        _cts?.Cancel();
        _cts?.Dispose();
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
        OutputLines.Clear();

        ImageReference = initialImage?.Trim() ?? string.Empty;
        Detach = true;
        RemoveOnExit = false;
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

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void Close()
    {
        _cts?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task RunDetachedAsync(RunContainerOptions options)
    {
        var started = false;
        try
        {
            var output = await _cliProvider!.RunContainerAsync(options, _cts!.Token).ConfigureAwait(true);
            started = true;
            var id = output.Trim();
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
        }
    }

    private async Task RunForegroundAsync(RunContainerOptions options)
    {
        OutputLines.Clear();
        TerminalState = TerminalState.Running;

        var started = false;
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
    };

    private void UpdateCommandPreview()
        => CommandPreview = _cliProvider?.BuildRunContainerCommand(BuildOptions()) ?? string.Empty;

    partial void OnImageReferenceChanged(string value) => UpdateCommandPreview();

    partial void OnDetachChanged(bool value) => UpdateCommandPreview();

    partial void OnRemoveOnExitChanged(bool value) => UpdateCommandPreview();

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
