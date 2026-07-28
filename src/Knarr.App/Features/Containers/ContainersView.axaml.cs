using Avalonia.Controls;
using Knarr.App.Features.ContainerLogs;
using Knarr.App.Features.RunContainer;

namespace Knarr.App.Features.Containers;

public partial class ContainersView : UserControl
{
    private ContainersViewModel? _viewModel;

    public ContainersView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.RunDialogRequested -= OnRunDialogRequested;
            _viewModel.LogsDialogRequested -= OnLogsDialogRequested;
        }

        _viewModel = DataContext as ContainersViewModel;

        if (_viewModel is not null)
        {
            _viewModel.RunDialogRequested += OnRunDialogRequested;
            _viewModel.LogsDialogRequested += OnLogsDialogRequested;
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

    private async void OnLogsDialogRequested(object? sender, ContainerLogsDialogViewModel dialogViewModel)
    {
        ContainerLogsDialog dialog = new() { DataContext = dialogViewModel };

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }
}

