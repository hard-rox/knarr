using Avalonia.Controls;

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

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel?.CloseRequested -= OnCloseRequested;
        _viewModel = null;

        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }
}
