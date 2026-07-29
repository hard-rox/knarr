using Avalonia.Controls;

namespace Knarr.App.Features.Containers.ContainerLogs;

public partial class ContainerLogsDialog : Window
{
    private ContainerLogsDialogViewModel? _viewModel;

    public ContainerLogsDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel?.CloseRequested -= OnCloseRequested;

        _viewModel = DataContext as ContainerLogsDialogViewModel;

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
