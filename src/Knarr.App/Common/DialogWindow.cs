using Avalonia.Controls;

namespace Knarr.App.Common;

/// <summary>
/// Base window for dialogs. Closes itself when the hosted <see cref="IDialogViewModel"/>
/// requests it, so dialog code-behind stays limited to <c>InitializeComponent()</c>.
/// </summary>
public class DialogWindow : Window
{
    private IDialogViewModel? _viewModel;

    protected DialogWindow()
    {
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachViewModel();

        _viewModel = DataContext as IDialogViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private void OnClosed(object? sender, EventArgs e)
    {
        DetachViewModel();

        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel = null;
        }
    }
}
