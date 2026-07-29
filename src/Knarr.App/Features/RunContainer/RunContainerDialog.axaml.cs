using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Knarr.App.Features.RunContainer;

public partial class RunContainerDialog : Window
{
    private RunContainerDialogViewModel? _viewModel;

    public RunContainerDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel?.CloseRequested -= OnCloseRequested;

        _viewModel = DataContext as RunContainerDialogViewModel;

        _viewModel?.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private async void OnCopyCommand(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is { CommandPreview: { Length: > 0 } command } &&
            GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(command);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel?.CloseRequested -= OnCloseRequested;
        _viewModel = null;

        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }
}
