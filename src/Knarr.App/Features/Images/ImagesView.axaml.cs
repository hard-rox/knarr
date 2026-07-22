using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Knarr.App.Models;

namespace Knarr.App.Features.Images;

public partial class ImagesView : UserControl
{
    public ImagesView()
    {
        InitializeComponent();
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
