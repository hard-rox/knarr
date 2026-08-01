using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Knarr.App.Features.RunContainer;

public partial class RunContainerDialog : DialogWindow
{
    public RunContainerDialog() => InitializeComponent();

    private async void OnCopyCommand(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RunContainerDialogViewModel { CommandPreview: { Length: > 0 } command } &&
            GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(command);
        }
    }
}
