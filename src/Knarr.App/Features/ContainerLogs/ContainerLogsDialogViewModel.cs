using Knarr.App.Controls;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.ContainerLogs;

/// <summary>
/// View model for the modal "container logs" dialog. Streams a single container's logs
/// (<c>logs</c>) into a terminal panel, letting the user toggle <c>--follow</c>/<c>--timestamps</c>
/// and limit output with <c>--tail</c> (defaults to the last 200 lines). Any option change cancels
/// the in-flight stream and starts a fresh one with the updated options; a follow stream only ever
/// stops via cancellation (the <see cref="StopCommand"/> or closing the dialog).
/// </summary>
public partial class ContainerLogsDialogViewModel : ViewModelBase
{
    private readonly IContainerCliProvider? _cliProvider;
    private readonly ILogger<ContainerLogsDialogViewModel> _logger;
    private CancellationTokenSource? _cts;
    private bool _suppressAutoRestart;

    public ContainerLogsDialogViewModel(
        IContainerCliProvider cliProvider,
        ILogger<ContainerLogsDialogViewModel> logger)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        OutputLines = [];
    }

    /// <summary>Design-time constructor; renders the dialog without a container CLI.</summary>
    public ContainerLogsDialogViewModel()
        : this(null!, NullLogger<ContainerLogsDialogViewModel>.Instance)
    {
    }

    /// <summary>Raised when the dialog should close.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The id of the container whose logs are streamed.</summary>
    [ObservableProperty]
    public partial string ContainerId { get; set; } = string.Empty;

    /// <summary>The display name of the container, shown in the dialog title.</summary>
    [ObservableProperty]
    public partial string ContainerName { get; set; } = string.Empty;

    /// <summary>Whether to keep streaming new output as it is produced (<c>--follow</c>).</summary>
    [ObservableProperty]
    public partial bool Follow { get; set; }

    /// <summary>Whether to prefix each log line with its timestamp (<c>--timestamps</c>).</summary>
    [ObservableProperty]
    public partial bool Timestamps { get; set; }

    /// <summary>Whether output is limited to the last <see cref="TailLines"/> lines (<c>--tail</c>).</summary>
    [ObservableProperty]
    public partial bool LimitTail { get; set; }

    /// <summary>The tail line count applied when <see cref="LimitTail"/> is checked.</summary>
    [ObservableProperty]
    public partial int TailLines { get; set; } = 200;

    /// <summary>Drives the terminal panel accent while logs stream.</summary>
    [ObservableProperty]
    public partial TerminalState TerminalState { get; set; } = TerminalState.Idle;

    /// <summary>True while a log stream is in flight (follow or a one-shot fetch).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    public partial bool IsStreaming { get; set; }

    /// <summary>Human-readable status shown to the user (stopped / error).</summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Live transcript of the streamed logs (command / stdout / stderr / exit).</summary>
    public ObservableCollection<CliOutputLine> OutputLines { get; }

    /// <summary>Short 12-character id, matching the CLI's abbreviated form.</summary>
    public string ShortId => ContainerId.Length > 12 ? ContainerId[..12] : ContainerId;

    /// <summary>
    /// Resets the dialog to a fresh session for the given container and immediately starts
    /// streaming its logs with default options (no follow, limited to the last 200 lines).
    /// </summary>
    public void Reset(string containerId, string containerName)
    {
        RequestCancellation();
        _cts = null;
        OutputLines.Clear();

        _suppressAutoRestart = true;
        ContainerId = containerId;
        ContainerName = containerName;
        Follow = false;
        Timestamps = false;
        LimitTail = true;
        TailLines = 200;
        StatusMessage = null;
        TerminalState = TerminalState.Idle;
        _suppressAutoRestart = false;

        RestartStream();
    }

    private bool CanStop => IsStreaming;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => RequestCancellation();

    [RelayCommand]
    private void Close()
    {
        RequestCancellation();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnFollowChanged(bool value) => RestartStreamIfNotSuppressed();

    partial void OnTimestampsChanged(bool value) => RestartStreamIfNotSuppressed();

    partial void OnLimitTailChanged(bool value) => RestartStreamIfNotSuppressed();

    partial void OnTailLinesChanged(int value) => RestartStreamIfNotSuppressed();

    private void RestartStreamIfNotSuppressed()
    {
        if (!_suppressAutoRestart)
        {
            RestartStream();
        }
    }

    /// <summary>
    /// Requests cancellation of the in-flight stream on a background thread (see
    /// <see cref="RunContainer.RunContainerDialogViewModel"/> for why this must not run on the UI
    /// thread), then starts a fresh stream with the current options.
    /// </summary>
    private void RestartStream()
    {
        if (_cliProvider is null || string.IsNullOrWhiteSpace(ContainerId))
        {
            return;
        }

        RequestCancellation();
        _ = StreamAsync();
    }

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
                // The stream already completed and disposed the source; nothing left to cancel.
            }
        });
    }

    private async Task StreamAsync()
    {
        ContainerLogsOptions options = BuildOptions();
        CancellationTokenSource cts = new();
        _cts = cts;

        OutputLines.Clear();
        TerminalState = TerminalState.Running;
        IsStreaming = true;
        StatusMessage = null;

        var completed = false;
        try
        {
            await foreach (CliOutputLine line in _cliProvider!
                .StreamContainerLogsAsync(options, cts.Token)
                .ConfigureAwait(true))
            {
                OutputLines.Add(line);
            }

            completed = true;
        }
        catch (OperationCanceledException)
        {
            TerminalState = TerminalState.Canceled;
            StatusMessage = "Log stream stopped.";
        }
        catch (Exception ex)
        {
            TerminalState = TerminalState.Error;
            StatusMessage = ex.Message;
            _logger.LogError(ex, "Failed to stream logs for container {ContainerId}", options.ContainerId);
        }
        finally
        {
            IsStreaming = false;
            if (ReferenceEquals(_cts, cts))
            {
                _cts = null;
            }

            cts.Dispose();
        }

        if (completed)
        {
            TerminalState = TerminalState.Success;
        }
    }

    // Since/Until filtering is not exposed in the UI for now; ContainerLogsOptions still carries
    // the Since/Until DTO properties for when this is revisited.
    private ContainerLogsOptions BuildOptions() => new()
    {
        ContainerId = ContainerId,
        Follow = Follow,
        Timestamps = Timestamps,
        TailLines = LimitTail ? TailLines : null,
    };
}
