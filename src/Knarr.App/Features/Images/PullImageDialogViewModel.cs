using System.Text;
using System.Text.RegularExpressions;
using Knarr.App.Controls;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Images;

/// <summary>
/// View model for the modal image-pull dialog. Streams the pull command transcript into a
/// <see cref="TerminalOutputView"/>-friendly collection, gates the Pull action behind strict
/// OCI-style reference validation, supports cancellation and copy-to-clipboard, and stays open in a
/// terminal (success/error/canceled) state after the command completes. Each dialog open starts a
/// fresh session via <see cref="Reset"/>.
/// </summary>
public partial class PullImageDialogViewModel : ViewModelBase
{
    /// <summary>Maximum number of transcript lines retained; older lines are dropped when exceeded.</summary>
    private const int MaxLines = 5000;

    // Pragmatic OCI/distribution reference grammar: optional registry host[:port]/, path, optional
    // :tag, optional @digest. Anchored and compiled for fast, allocation-light validation on input.
    [GeneratedRegex(
        @"^(?<domain>(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?)(?:\.(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?))*(?::[0-9]+)?/)?" +
        @"(?<name>[a-z0-9]+(?:(?:[._]|__|[-]+)[a-z0-9]+)*(?:/[a-z0-9]+(?:(?:[._]|__|[-]+)[a-z0-9]+)*)*)" +
        @"(?::(?<tag>[a-zA-Z0-9_][a-zA-Z0-9._-]{0,127}))?" +
        @"(?:@(?<digest>[A-Za-z][A-Za-z0-9]*(?:[-_+.][A-Za-z][A-Za-z0-9]*)*:[0-9a-fA-F]{32,}))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceRegex();

    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<PullImageDialogViewModel> _logger;

    private CancellationTokenSource? _cts;

    public PullImageDialogViewModel(IContainerCliProvider cliProvider, ILogger<PullImageDialogViewModel> logger)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        Output = [];
    }

    /// <summary>Design-time constructor.</summary>
    public PullImageDialogViewModel()
    {
        _cliProvider = null!;
        _logger = NullLogger<PullImageDialogViewModel>.Instance;
        Output = [];
    }

    /// <summary>Raised after a pull completes successfully so the host can refresh its image list.</summary>
    public event EventHandler? PullSucceeded;

    /// <summary>Raised when the user copies the transcript; carries the full text for the view to place on the clipboard.</summary>
    public event EventHandler<string>? CopyRequested;

    /// <summary>Raised when the dialog should close.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The streamed command transcript bound to the reusable terminal panel.</summary>
    public ObservableCollection<CliOutputLine> Output { get; }

    /// <summary>The image reference the user intends to pull (e.g. <c>docker.io/library/alpine:3.20</c>).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    public partial string ImageReference { get; set; } = string.Empty;

    /// <summary>True while a pull is in flight.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    /// <summary>Lifecycle state driving the terminal panel accent and the status label.</summary>
    [ObservableProperty]
    public partial TerminalState State { get; set; } = TerminalState.Idle;

    /// <summary>Human-readable status shown alongside the terminal panel.</summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>True when the transcript was capped and older lines were dropped.</summary>
    [ObservableProperty]
    public partial bool IsTruncated { get; set; }

    /// <summary>Note describing the truncation, shown by the terminal panel when <see cref="IsTruncated"/> is true.</summary>
    [ObservableProperty]
    public partial string? TruncationNote { get; set; }

    /// <summary>True when the current <see cref="ImageReference"/> is a syntactically valid image reference.</summary>
    private bool CanPull =>
        !IsRunning
        && !string.IsNullOrWhiteSpace(ImageReference)
        && ReferenceRegex().IsMatch(ImageReference.Trim());

    private bool HasOutput => Output.Count > 0;

    /// <summary>Resets the dialog to a fresh session, optionally seeding the reference input.</summary>
    public void Reset(string? initialReference)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        ImageReference = initialReference?.Trim() ?? string.Empty;
        Output.Clear();
        IsTruncated = false;
        TruncationNote = null;
        IsRunning = false;
        State = TerminalState.Idle;
        StatusMessage = null;
        CopyOutputCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanPull))]
    private async Task Pull()
    {
        var reference = ImageReference.Trim();

        Output.Clear();
        IsTruncated = false;
        TruncationNote = null;
        CopyOutputCommand.NotifyCanExecuteChanged();

        IsRunning = true;
        State = TerminalState.Running;
        StatusMessage = $"Pulling {reference}\u2026";
        _logger.LogInformation("Pulling image {Reference}", reference);

        _cts = new CancellationTokenSource();
        try
        {
            await foreach (CliOutputLine line in _cliProvider
                .PullImageStreamingAsync(reference, _cts.Token)
                .ConfigureAwait(true))
            {
                AddLine(line);

                if (line.Kind == CliOutputKind.Exit)
                {
                    if (line.ExitCode == 0)
                    {
                        State = TerminalState.Success;
                        StatusMessage = $"Pulled {reference}.";
                    }
                    else
                    {
                        State = TerminalState.Error;
                        StatusMessage = $"Pull failed (exit code {line.ExitCode}).";
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            State = TerminalState.Canceled;
            StatusMessage = "Pull canceled.";
            AddLine(CliOutputLine.ForStandardError("Pull canceled by user."));
            _logger.LogInformation("Pull canceled for {Reference}", reference);
        }
        catch (Exception ex)
        {
            State = TerminalState.Error;
            StatusMessage = ex.Message;
            AddLine(CliOutputLine.ForStandardError(ex.Message));
            _logger.LogError(ex, "Pull failed for {Reference}", reference);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }

        if (State == TerminalState.Success)
        {
            PullSucceeded?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(HasOutput))]
    private void CopyOutput()
    {
        var builder = new StringBuilder();
        foreach (CliOutputLine line in Output)
        {
            builder.AppendLine(line.Text);
        }

        CopyRequested?.Invoke(this, builder.ToString());
    }

    [RelayCommand]
    private void Close()
    {
        _cts?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddLine(CliOutputLine line)
    {
        Output.Add(line);

        if (Output.Count > MaxLines)
        {
            var removeCount = Output.Count - MaxLines;
            for (var i = 0; i < removeCount; i++)
            {
                Output.RemoveAt(0);
            }

            IsTruncated = true;
            TruncationNote = $"Output truncated to the most recent {MaxLines:N0} lines.";
        }

        CopyOutputCommand.NotifyCanExecuteChanged();
    }
}
