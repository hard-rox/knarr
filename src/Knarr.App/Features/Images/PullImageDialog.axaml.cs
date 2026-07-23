using Avalonia.Controls;

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
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as PullImageDialogViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel = null;
        }

        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }
}
