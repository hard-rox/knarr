using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Images;

/// <summary>
/// View model for the modal image-pull dialog. Runs a single buffered pull command (the CLI does
/// not stream output incrementally), gates the Pull action behind strict OCI-style reference
/// validation, and supports cancellation. Stays open showing a status message after the command
/// completes. Each dialog open starts a fresh session via <see cref="Reset"/>.
/// </summary>
public partial class PullImageDialogViewModel(
    IContainerCliProvider cliProvider,
    ILogger<PullImageDialogViewModel> logger)
    : ViewModelBase
{
    // Pragmatic OCI/distribution reference grammar: optional registry host[:port]/, path, optional
    // :tag, optional @digest. Anchored and compiled for fast, allocation-light validation on input.
    [GeneratedRegex(
        @"^(?<domain>(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?)(?:\.(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?))*(?::[0-9]+)?/)?" +
        @"(?<name>[a-z0-9]+(?:(?:[._]|__|[-]+)[a-z0-9]+)*(?:/[a-z0-9]+(?:(?:[._]|__|[-]+)[a-z0-9]+)*)*)" +
        @"(?::(?<tag>[a-zA-Z0-9_][a-zA-Z0-9._-]{0,127}))?" +
        @"(?:@(?<digest>[A-Za-z][A-Za-z0-9]*(?:[-_+.][A-Za-z][A-Za-z0-9]*)*:[0-9a-fA-F]{32,}))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceRegex();

    private CancellationTokenSource? _cts;

    /// <summary>Design-time constructor.</summary>
    public PullImageDialogViewModel() : this(null!, NullLogger<PullImageDialogViewModel>.Instance)
    {
    }

    /// <summary>Raised after a pull completes successfully so the host can refresh its image list.</summary>
    public event EventHandler? PullSucceeded;

    /// <summary>Raised when the dialog should close.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The image reference the user intends to pull (e.g. <c>docker.io/library/alpine:3.20</c>).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    public partial string ImageReference { get; set; } = string.Empty;

    /// <summary>True while a pull is in flight.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    /// <summary>Human-readable status shown to the user (in progress / success / error / canceled).</summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>True when the current <see cref="ImageReference"/> is a syntactically valid image reference.</summary>
    private bool CanPull =>
        !IsRunning
        && !string.IsNullOrWhiteSpace(ImageReference)
        && ReferenceRegex().IsMatch(ImageReference.Trim());

    /// <summary>Resets the dialog to a fresh session, optionally seeding the reference input.</summary>
    public void Reset(string? initialReference)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        ImageReference = initialReference?.Trim() ?? string.Empty;
        IsRunning = false;
        StatusMessage = null;
    }

    [RelayCommand(CanExecute = nameof(CanPull))]
    private async Task Pull()
    {
        var reference = ImageReference.Trim();

        IsRunning = true;
        StatusMessage = $"Pulling {reference}\u2026";
        logger.LogInformation("Pulling image {Reference}", reference);

        _cts = new CancellationTokenSource();
        var succeeded = false;
        try
        {
            await cliProvider.PullImageAsync(reference, _cts.Token).ConfigureAwait(true);

            succeeded = true;
            StatusMessage = $"Pulled {reference}.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Pull canceled.";
            logger.LogInformation("Pull canceled for {Reference}", reference);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            logger.LogError(ex, "Pull failed for {Reference}", reference);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }

        if (succeeded)
        {
            PullSucceeded?.Invoke(this, EventArgs.Empty);
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
}
