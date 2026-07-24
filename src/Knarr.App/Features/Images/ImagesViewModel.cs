using Avalonia.Threading;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Images;

public partial class ImagesViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(5);

    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<ImagesViewModel> _logger;
    private readonly Func<PullImageDialogViewModel>? _pullDialogFactory;
    private readonly List<ImageItem> _allImages = [];

    private DispatcherTimer? _refreshTimer;
    private bool _loadInFlight;

    public ImagesViewModel(
        IContainerCliProvider cliProvider,
        ILogger<ImagesViewModel> logger,
        Func<PullImageDialogViewModel>? pullDialogFactory = null)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        _pullDialogFactory = pullDialogFactory;
        Images = [];
        _ = LoadAsync();
        StartAutoRefresh();
    }

    /// <summary>Design-time constructor; renders an empty list without a container CLI.</summary>
    public ImagesViewModel()
    {
        _cliProvider = null!;
        _logger = NullLogger<ImagesViewModel>.Instance;
        Images = [];
    }

    public ObservableCollection<ImageItem> Images { get; }

    /// <summary>
    /// Raised when a pull dialog should be shown. The view resolves the owner window and displays
    /// the supplied, already-initialised dialog view model modally.
    /// </summary>
    public event EventHandler<PullImageDialogViewModel>? PullDialogRequested;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>True while a CLI list/refresh is in flight.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    public partial bool IsLoading { get; set; }

    /// <summary>Message from the most recent failed CLI action, or null when the last action succeeded.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    public partial string? ErrorMessage { get; set; }

    /// <summary>True when the last CLI action failed and <see cref="ErrorMessage"/> is set.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>True when there are rows to display in the table.</summary>
    public bool HasItems => !IsLoading && !HasError && Images.Count > 0;

    /// <summary>True when the CLI returned no images at all (not merely filtered out).</summary>
    public bool IsEmpty => !IsLoading && !HasError && _allImages.Count == 0;

    /// <summary>True when images exist, but the current search filter matches none.</summary>
    public bool HasNoResults => !IsLoading && !HasError && _allImages.Count > 0 && Images.Count == 0;

    /// <summary>Rows currently ticked for a bulk action.</summary>
    public IReadOnlyList<ImageItem> SelectedImages =>
        [.. Images.Where(i => i.IsSelected)];

    public int SelectedCount => Images.Count(i => i.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    /// <summary>
    /// Header "select all" checkbox state: true/false when uniform, null (indeterminate) when mixed.
    /// </summary>
    public bool? AllSelected
    {
        get
        {
            if (Images.Count == 0)
            {
                return false;
            }

            var selected = SelectedCount;
            if (selected == 0)
            {
                return false;
            }

            return selected == Images.Count ? true : null;
        }
        set
        {
            // When the user clicks a checked box, Avalonia cycles it to null. Treat this as deselect all.
            var target = value ?? false;
            foreach (ImageItem image in Images)
            {
                image.IsSelected = target;
            }
        }
    }

    private void OnImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageItem.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedImages));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(AllSelected));
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ImageItem> filtered = _allImages;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = _allImages.Where(i =>
                i.Repository.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Tag.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Images.Clear();
        foreach (ImageItem image in filtered)
        {
            Images.Add(image);
        }

        OnPropertyChanged(nameof(SelectedImages));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(AllSelected));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoResults));
    }

    // Toolbar commands — each maps 1:1 onto a CLI invocation via the provider, then reloads.
    [RelayCommand]
    private Task Refresh()
    {
        _logger.LogInformation("Manual images refresh requested");
        return LoadAsync();
    }

    [RelayCommand]
    private void Build()
    {
        // Build dialog is a later milestone.
    }

    [RelayCommand]
    private void Pull(string? initialReference)
    {
        if (_pullDialogFactory is null)
        {
            return;
        }

        PullImageDialogViewModel dialogViewModel = _pullDialogFactory();
        dialogViewModel.Reset(initialReference);
        dialogViewModel.PullSucceeded += OnPullSucceeded;
        PullDialogRequested?.Invoke(this, dialogViewModel);
    }

    private void OnPullSucceeded(object? sender, EventArgs e) => _ = LoadAsync();

    [RelayCommand]
    private void Import()
    {
        // Import file picker is a later milestone.
    }

    // Bulk (multiselect) commands — the provider runs each batch as a single command session.
    [RelayCommand]
    private Task DeleteSelected()
    {
        List<string> references = SelectedImages.Select(ResolveImageReference).ToList();
        return references.Count == 0
            ? Task.CompletedTask
            : ExecuteAndReloadAsync(ct => _cliProvider.RemoveImagesAsync(references, force: true, ct));
    }

    // Row commands.
    [RelayCommand]
    private void Run(ImageItem image)
    {
        // Run wizard is a later milestone.
    }

    [RelayCommand]
    private void Tag(ImageItem image)
    {
        // Tag dialog (target reference input) is a later milestone.
    }

    [RelayCommand]
    private void Inspect(ImageItem image)
    {
        // Inspect viewer is a later milestone.
    }

    [RelayCommand]
    private Task Remove(ImageItem image)
        => ExecuteAndReloadAsync(ct => _cliProvider.RemoveImageAsync(ResolveImageReference(image), force: true, ct));

    private static string ResolveImageReference(ImageItem image)
    {
        if (!string.IsNullOrWhiteSpace(image.Repository) && !string.IsNullOrWhiteSpace(image.Tag))
        {
            return image.RepoTag;
        }

        if (!string.IsNullOrWhiteSpace(image.Id))
        {
            return image.Id;
        }

        return image.RepoTag;
    }

    /// <summary>
    /// Loads (or reloads) the image list from the CLI. Safe to call repeatedly; concurrent calls
    /// are coalesced. When <paramref name="showLoading"/> is false (background auto-refresh) the
    /// loading indicator is not toggled, so the table stays visible without flicker. Failures are
    /// surfaced via <see cref="ErrorMessage"/> and never throw.
    /// </summary>
    private async Task LoadAsync(bool showLoading = true, CancellationToken cancellationToken = default)
    {
        if (_loadInFlight)
        {
            return;
        }

        _loadInFlight = true;
        if (showLoading)
        {
            IsLoading = true;
        }

        ErrorMessage = null;
        try
        {
            IReadOnlyList<ContainerImage> summaries = await _cliProvider.ListImagesAsync(cancellationToken).ConfigureAwait(true);

            foreach (ImageItem existing in _allImages)
            {
                existing.PropertyChanged -= OnImagePropertyChanged;
            }

            _allImages.Clear();
            foreach (ContainerImage summary in summaries)
            {
                ImageItem item = new(summary);
                item.PropertyChanged += OnImagePropertyChanged;
                _allImages.Add(item);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to load images");
        }
        finally
        {
            if (showLoading)
            {
                IsLoading = false;
            }

            _loadInFlight = false;
        }
    }

    /// <summary>Runs a mutating CLI action, surfacing failures via <see cref="ErrorMessage"/>, then reloads.</summary>
    private async Task ExecuteAndReloadAsync(Func<CancellationToken, Task> action)
    {
        try
        {
            await action(CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Image action failed");
        }

        await LoadAsync().ConfigureAwait(true);
    }

    /// <summary>Starts the periodic background refresh of the image list.</summary>
    private void StartAutoRefresh()
    {
        if (_refreshTimer is not null)
        {
            return;
        }

        _refreshTimer = new DispatcherTimer { Interval = _refreshInterval };
        _refreshTimer.Tick += async (_, _) => await LoadAsync(showLoading: false).ConfigureAwait(true);
        _refreshTimer.Start();
        _logger.LogDebug("Images auto-refresh started ({Interval}s)", _refreshInterval.TotalSeconds);
    }

    public void Dispose()
    {
        if (_refreshTimer is not null)
        {
            _refreshTimer.Stop();
            _refreshTimer = null;
            _logger.LogDebug("Images auto-refresh stopped");
        }

        foreach (ImageItem item in _allImages)
        {
            item.PropertyChanged -= OnImagePropertyChanged;
        }

        GC.SuppressFinalize(this);
    }
}
