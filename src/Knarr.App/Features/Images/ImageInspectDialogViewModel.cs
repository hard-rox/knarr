using System.Globalization;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Images;

public partial class ImageInspectDialogViewModel(
    IContainerCliProvider cliProvider,
    ILogger<ImageInspectDialogViewModel> logger)
    : ViewModelBase, IDialogViewModel
{
    private const string Dash = "\u2014";

    private readonly IContainerCliProvider? _cliProvider = cliProvider;
    private CancellationTokenSource? _cts;

    public ImageInspectDialogViewModel()
        : this(null!, NullLogger<ImageInspectDialogViewModel>.Instance)
    {
    }

    public event EventHandler? CloseRequested;

    [ObservableProperty] public partial string ImageReference { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInspection))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasInspection))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInspection))]
    [NotifyPropertyChangedFor(nameof(ShortId))]
    [NotifyPropertyChangedFor(nameof(FullId))]
    [NotifyPropertyChangedFor(nameof(Architecture))]
    [NotifyPropertyChangedFor(nameof(OperatingSystem))]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    [NotifyPropertyChangedFor(nameof(CreatedText))]
    [NotifyPropertyChangedFor(nameof(AuthorText))]
    [NotifyPropertyChangedFor(nameof(CommentText))]
    [NotifyPropertyChangedFor(nameof(RepoTagsText))]
    [NotifyPropertyChangedFor(nameof(RepoDigestsText))]
    [NotifyPropertyChangedFor(nameof(EntrypointText))]
    [NotifyPropertyChangedFor(nameof(CommandText))]
    [NotifyPropertyChangedFor(nameof(WorkingDirectoryText))]
    [NotifyPropertyChangedFor(nameof(UserText))]
    [NotifyPropertyChangedFor(nameof(StopSignalText))]
    [NotifyPropertyChangedFor(nameof(ExposedPorts))]
    [NotifyPropertyChangedFor(nameof(Volumes))]
    [NotifyPropertyChangedFor(nameof(EnvironmentVariables))]
    [NotifyPropertyChangedFor(nameof(Labels))]
    [NotifyPropertyChangedFor(nameof(HasExposedPorts))]
    [NotifyPropertyChangedFor(nameof(HasVolumes))]
    [NotifyPropertyChangedFor(nameof(HasEnvironmentVariables))]
    [NotifyPropertyChangedFor(nameof(HasLabels))]
    [NotifyPropertyChangedFor(nameof(RawJson))]
    public partial ImageInspection? Inspection { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasInspection => !IsLoading && !HasError && Inspection is not null;

    public string ShortId => Text(Inspection?.ShortId);
    public string FullId => Text(Inspection?.Id);
    public string Architecture => Text(Inspection?.Architecture);
    public string OperatingSystem => Text(Inspection?.Os);
    public string SizeText => Text(Inspection?.Size);
    public string AuthorText => Text(Inspection?.Author);
    public string CommentText => Text(Inspection?.Comment);
    public string RepoTagsText => Join(Inspection?.RepoTags);
    public string RepoDigestsText => Join(Inspection?.RepoDigests);
    public string EntrypointText => JoinArgs(Inspection?.Entrypoint);
    public string CommandText => JoinArgs(Inspection?.Command);
    public string WorkingDirectoryText => Text(Inspection?.WorkingDirectory);
    public string UserText => Text(Inspection?.User);
    public string StopSignalText => Text(Inspection?.StopSignal);

    public string CreatedText => Inspection?.Created is { } dt
        ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
        : Dash;

    public IReadOnlyList<string> ExposedPorts => Inspection?.ExposedPorts ?? [];
    public IReadOnlyList<string> Volumes => Inspection?.Volumes ?? [];
    public IReadOnlyList<InspectionEntry> EnvironmentVariables => Inspection?.EnvironmentVariables ?? [];
    public IReadOnlyList<InspectionEntry> Labels => Inspection?.Labels ?? [];

    public bool HasExposedPorts => ExposedPorts.Count > 0;
    public bool HasVolumes => Volumes.Count > 0;
    public bool HasEnvironmentVariables => EnvironmentVariables.Count > 0;
    public bool HasLabels => Labels.Count > 0;

    public string RawJson => Inspection?.RawJson ?? string.Empty;

    public void Reset(string imageReference)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        ImageReference = imageReference;
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
        if (_cliProvider is null || string.IsNullOrWhiteSpace(ImageReference))
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
            Inspection = await _cliProvider.InspectImageAsync(ImageReference, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Image inspect canceled for {Reference}", ImageReference);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            logger.LogError(ex, "Image inspect failed for {Reference}", ImageReference);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string Text(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Dash : value;

    private static string Join(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? string.Join('\n', values) : Dash;

    private static string JoinArgs(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? string.Join(' ', values) : Dash;
}
