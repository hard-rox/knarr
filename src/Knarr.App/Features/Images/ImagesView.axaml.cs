using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Knarr.App.Features.RunContainer;

namespace Knarr.App.Features.Images;

public partial class ImagesView : UserControl
{
    private ImagesViewModel? _viewModel;

    public ImagesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PullDialogRequested -= OnPullDialogRequested;
            _viewModel.RunDialogRequested -= OnRunDialogRequested;
        }

        _viewModel = DataContext as ImagesViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PullDialogRequested += OnPullDialogRequested;
            _viewModel.RunDialogRequested += OnRunDialogRequested;
        }
    }

    private async void OnPullDialogRequested(object? sender, PullImageDialogViewModel dialogViewModel)
    {
        PullImageDialog dialog = new() { DataContext = dialogViewModel };

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    private async void OnRunDialogRequested(object? sender, RunContainerDialogViewModel dialogViewModel)
    {
        RunContainerDialog dialog = new() { DataContext = dialogViewModel };

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    private async void OnCopyImageName(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ImageItem image } &&
            TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(image.RepoTag);
        }
    }
}
