using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knarr.App.Common;
using Knarr.App.Models;

namespace Knarr.App.Features.Images;

/// <summary>
/// View model for the Images feature. Presents the list of images and exposes the actions that
/// (in a later milestone) will map 1:1 onto CLI commands. For now the commands are stubs and the
/// data is sample/design data.
/// </summary>
public partial class ImagesViewModel : ViewModelBase
{
    private readonly List<ImageItem> _allImages;

    public ImagesViewModel()
    {
        _allImages =
        [
            new ImageItem
            {
                Repository = "nginx",
                Tag = "latest",
                Id = "sha256:9c7a54a9a1b2",
                Created = "3 days ago",
                Size = "187 MB"
            },
            new ImageItem
            {
                Repository = "postgres",
                Tag = "16",
                Id = "sha256:b1f2e3c4d5e6",
                Created = "1 week ago",
                Size = "438 MB"
            },
            new ImageItem
            {
                Repository = "redis",
                Tag = "7-alpine",
                Id = "sha256:44de9a0f1e2d",
                Created = "2 weeks ago",
                Size = "41 MB"
            },
            new ImageItem
            {
                Repository = "worker",
                Tag = "dev",
                Id = "sha256:0a11cc22bb33",
                Created = "1 hour ago",
                Size = "312 MB"
            },
            new ImageItem
            {
                Repository = "ubuntu",
                Tag = "24.04",
                Id = "sha256:c2d3e4f5a6b7",
                Created = "1 month ago",
                Size = "78 MB"
            }
        ];

        Images = new ObservableCollection<ImageItem>(_allImages);
        foreach (var image in _allImages)
        {
            image.PropertyChanged += OnImagePropertyChanged;
        }
    }

    public ObservableCollection<ImageItem> Images { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ImageItem? _selectedImage;

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
            // A null assignment comes from the indeterminate state; treat it as "select all".
            var target = value ?? true;
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

    // Toolbar commands — stubs for the design milestone; real CLI wiring lands later.
    [RelayCommand]
    private void Refresh()
    {
    }

    [RelayCommand]
    private void Build()
    {
    }

    [RelayCommand]
    private void Pull()
    {
    }

    [RelayCommand]
    private void Push()
    {
    }

    [RelayCommand]
    private void Import()
    {
    }

    [RelayCommand]
    private void Prune()
    {
    }

    // Bulk (multiselect) commands — operate on every ticked row. Stubs for the design milestone.
    [RelayCommand]
    private void PushSelected()
    {
        foreach (var image in SelectedImages)
        {
            PushImage(image);
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        foreach (var image in SelectedImages.ToList())
        {
            Remove(image);
        }
    }

    // Row commands — stubs for the design milestone.
    [RelayCommand]
    private void Run(ImageItem image)
    {
    }

    [RelayCommand]
    private void Tag(ImageItem image)
    {
    }

    [RelayCommand]
    private void PushImage(ImageItem image)
    {
    }

    [RelayCommand]
    private void Inspect(ImageItem image)
    {
    }

    [RelayCommand]
    private void Remove(ImageItem image)
    {
    }
}
