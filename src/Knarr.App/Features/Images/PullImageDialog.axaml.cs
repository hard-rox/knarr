using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace Knarr.App.Features.Images;

public partial class PullImageDialog : Window
{
    private PullImageDialogViewModel? _viewModel;

    public PullImageDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CopyRequested -= OnCopyRequested;
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as PullImageDialogViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CopyRequested += OnCopyRequested;
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private async void OnCopyRequested(object? sender, string text)
    {
        if (Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CopyRequested -= OnCopyRequested;
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel = null;
        }

        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }
}
