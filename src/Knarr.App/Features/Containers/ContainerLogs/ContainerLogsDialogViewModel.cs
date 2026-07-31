using Knarr.App.Controls;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Containers.ContainerLogs;

public partial class ContainerLogsDialogViewModel : ViewModelBase, IDialogViewModel
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

    public ContainerLogsDialogViewModel()
        : this(null!, NullLogger<ContainerLogsDialogViewModel>.Instance)
    {
    }

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    public partial string ContainerId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContainerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Follow { get; set; }

    [ObservableProperty]
    public partial bool Timestamps { get; set; }

    [ObservableProperty]
    public partial bool LimitTail { get; set; }

    [ObservableProperty]
    public partial int TailLines { get; set; } = 200;

    [ObservableProperty]
    public partial TerminalState TerminalState { get; set; } = TerminalState.Idle;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    public partial bool IsStreaming { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public ObservableCollection<CliOutputLine> OutputLines { get; }

    public string ShortId => ContainerId.Length > 12 ? ContainerId[..12] : ContainerId;

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

    // Cancellation must happen off the UI thread (see RunContainerDialogViewModel) before starting a fresh stream.
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

        bool completed = false;
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
