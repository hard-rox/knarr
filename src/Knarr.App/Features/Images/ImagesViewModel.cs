using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Common;
using Knarr.App.Models;
using Knarr.App.Services;

namespace Knarr.App.Features.Images;

/// <summary>
/// View model for the Images feature. Presents the list of images and exposes the actions, each of
/// which maps 1:1 onto a single CLI command via <see cref="IContainerCliProvider"/>. Data is loaded
/// from the host container CLI.
/// </summary>
public partial class ImagesViewModel : ViewModelBase
{
    private readonly IContainerCliProvider _cliProvider;
    private readonly List<ImageItem> _allImages = [];

    public ImagesViewModel(IContainerCliProvider cliProvider)
    {
        _cliProvider = cliProvider;
        Images = new ObservableCollection<ImageItem>();

        // Kick off the initial load; property updates marshal back to the UI thread.
        _ = LoadAsync();
    }

    /// <summary>Design-time constructor; serves sample data via the in-memory provider.</summary>
    public ImagesViewModel()
        : this(new DesignTimeContainerCliProvider())
    {
    }

    public ObservableCollection<ImageItem> Images { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ImageItem? _selectedImage;

    /// <summary>True while a CLI list/refresh is in flight.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Message from the most recent failed CLI action, or null when the last action succeeded.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Rows currently ticked for a bulk action.</summary>
    public IReadOnlyList<ImageItem> SelectedImages =>
        Images.Where(i => i.IsSelected).ToList();

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
                i.Repository.Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                i.Tag.Contains(term, System.StringComparison.OrdinalIgnoreCase));
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
    private void Push()
    {
        // Toolbar push (reference input) is a later milestone; use the per-row push meanwhile.
    }

    [RelayCommand]
    private void Import()
    {
        // Import file picker is a later milestone.
    }

    [RelayCommand]
    private Task Prune() => ExecuteAndReloadAsync(ct => _cliProvider.PruneImagesAsync(ct));

    // Bulk (multiselect) commands — operate on every ticked row.
    [RelayCommand]
    private async Task PushSelected()
    {
        foreach (var image in SelectedImages.ToList())
        {
            await PushImage(image).ConfigureAwait(true);
        }
    }

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
    private Task PushImage(ImageItem image)
        => ExecuteAndReloadAsync(ct => _cliProvider.PushImageAsync(image.RepoTag, ct));

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
    public async Task LoadAsync(CancellationToken cancellationToken = default)
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
                    Id = summary.Id,
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
