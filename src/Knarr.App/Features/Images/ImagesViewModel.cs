namespace Knarr.App.Features.Images;

public partial class ImagesViewModel : ViewModelBase
{
    private readonly IContainerCliProvider _cliProvider;
    private readonly List<ImageItem> _allImages = [];

    public ImagesViewModel(IContainerCliProvider cliProvider)
    {
        _cliProvider = cliProvider;
        Images = [];
        _ = LoadAsync();
    }

    /// <summary>Design-time constructor; serves sample data via the in-memory provider.</summary>
    public ImagesViewModel()
        : this(new DesignTimeContainerCliProvider())
    {
    }

    public ObservableCollection<ImageItem> Images { get; }

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
            foreach (var image in Images)
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
        foreach (var image in filtered)
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
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private void Build()
    {
        // Build dialog is a later milestone.
    }

    [RelayCommand]
    private void Pull()
    {
        // Pull dialog (registry reference input) is a later milestone.
    }

    [RelayCommand]
    private void Import()
    {
        // Import file picker is a later milestone.
    }

    // Bulk (multiselect) commands operate on every ticked row.
    [RelayCommand]
    private async Task DeleteSelected()
    {
        foreach (var image in SelectedImages.ToList())
        {
            await Remove(image).ConfigureAwait(true);
        }
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
        => ExecuteAndReloadAsync(ct => _cliProvider.RemoveImageAsync(image.RepoTag, force: true, ct));

    /// <summary>
    /// Loads (or reloads) the image list from the CLI. Safe to call repeatedly; concurrent calls
    /// are coalesced. Failures are surfaced via <see cref="ErrorMessage"/> and never throw.
    /// </summary>
    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var summaries = await _cliProvider.ListImagesAsync(cancellationToken).ConfigureAwait(true);

            foreach (var existing in _allImages)
            {
                existing.PropertyChanged -= OnImagePropertyChanged;
            }

            _allImages.Clear();
            foreach (var summary in summaries)
            {
                var item = new ImageItem
                {
                    Repository = summary.Repository,
                    Tag = summary.Tag,
                    Created = summary.Created,
                    Size = summary.Size,
                };
                item.PropertyChanged += OnImagePropertyChanged;
                _allImages.Add(item);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
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
        }

        await LoadAsync().ConfigureAwait(true);
    }
}
