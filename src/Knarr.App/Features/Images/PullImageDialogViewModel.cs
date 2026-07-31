using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Images;

public partial class PullImageDialogViewModel(
    IContainerCliProvider cliProvider,
    ILogger<PullImageDialogViewModel> logger)
    : ViewModelBase, IDialogViewModel
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

    public PullImageDialogViewModel() : this(null!, NullLogger<PullImageDialogViewModel>.Instance)
    {
    }

    public event EventHandler? PullSucceeded;

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    public partial string ImageReference { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    [ObservableProperty] public partial string? StatusMessage { get; set; }

    private bool CanPull =>
        !IsRunning
        && !string.IsNullOrWhiteSpace(ImageReference)
        && ReferenceRegex().IsMatch(ImageReference.Trim());

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
        string reference = ImageReference.Trim();

        IsRunning = true;
        StatusMessage = $"Pulling {reference}\u2026";
        logger.LogInformation("Pulling image {Reference}", reference);

        _cts = new CancellationTokenSource();
        bool succeeded = false;
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
