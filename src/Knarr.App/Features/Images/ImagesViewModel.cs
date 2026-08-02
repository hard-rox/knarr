using Knarr.App.Features.RunContainer;
using Knarr.App.Services;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Features.Images;

public partial class ImagesViewModel : ViewModelBase, IDisposable
{
    private readonly IContainerCliProvider _cliProvider;
    private readonly ILogger<ImagesViewModel> _logger;
    private readonly IDialogService? _dialogService;
    private readonly List<ImageItem> _allImages = [];
    private readonly IDisposable? _refreshSubscription;

    private bool _loadInFlight;

    public ImagesViewModel(
        IContainerCliProvider cliProvider,
        ILogger<ImagesViewModel> logger,
        IDialogService? dialogService = null,
        IAutoRefreshService? autoRefresh = null)
    {
        _cliProvider = cliProvider;
        _logger = logger;
        _dialogService = dialogService;
        Images = [];
        _ = LoadAsync();
        _refreshSubscription = autoRefresh?.Subscribe(ct => LoadAsync(showLoading: false, ct));
    }

    public ImagesViewModel()
    {
        _cliProvider = null!;
        _logger = NullLogger<ImagesViewModel>.Instance;
        Images = [];
    }

    public ObservableCollection<ImageItem> Images { get; }

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasItems => !IsLoading && !HasError && Images.Count > 0;

    public bool IsEmpty => !IsLoading && !HasError && _allImages.Count == 0;

    public bool HasNoResults => !IsLoading && !HasError && _allImages.Count > 0 && Images.Count == 0;

    public IReadOnlyList<ImageItem> SelectedImages =>
        [.. Images.Where(i => i.IsSelected)];

    public int SelectedCount => Images.Count(i => i.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    public bool? AllSelected
    {
        get
        {
            if (Images.Count == 0)
            {
                return false;
            }

            int selected = SelectedCount;
            if (selected == 0)
            {
                return false;
            }

            return selected == Images.Count ? true : null;
        }
        set
        {
            // When the user clicks a checked box, Avalonia cycles it to null. Treat this as deselect all.
            bool target = value ?? false;
            foreach (ImageItem image in Images)
            {
                image.IsSelected = target;
            }
        }
    }

    private void OnImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ImageItem.IsSelected)) return;
        OnPropertyChanged(nameof(SelectedImages));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(AllSelected));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ImageItem> filtered = _allImages;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string term = SearchText.Trim();
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
        _dialogService?.Show<PullImageDialogViewModel>(dialogViewModel =>
        {
            dialogViewModel.Reset(initialReference);
            dialogViewModel.PullSucceeded += OnPullSucceeded;
        });
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
        _dialogService?.Show<RunContainerDialogViewModel>(dialogViewModel =>
        {
            dialogViewModel.Reset(image.RepoTag, imageEditable: false);
            dialogViewModel.ContainerStarted += OnContainerStarted;
        });
    }

    private void OnContainerStarted(object? sender, EventArgs e) => _ = LoadAsync();

    [RelayCommand]
    private void Tag(ImageItem image)
    {
        // Tag dialog (target reference input) is a later milestone.
    }

    [RelayCommand]
    private void Inspect(ImageItem image)
    {
        _dialogService?.Show<ImageInspectDialogViewModel>(vm => vm.Reset(ResolveImageReference(image)));
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

        return !string.IsNullOrWhiteSpace(image.Id) ? image.Id : image.RepoTag;
    }

    // Concurrent calls are coalesced; showLoading=false (background auto-refresh) skips the loading
    // flag so the table stays visible without flicker.
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
            IReadOnlyList<ContainerImage> summaries =
                await _cliProvider.ListImagesAsync(cancellationToken).ConfigureAwait(true);

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

    public void Dispose()
    {
        _refreshSubscription?.Dispose();

        foreach (ImageItem item in _allImages)
        {
            item.PropertyChanged -= OnImagePropertyChanged;
        }

        GC.SuppressFinalize(this);
    }
}
