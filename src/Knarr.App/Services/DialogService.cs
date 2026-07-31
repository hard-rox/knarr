using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;

namespace Knarr.App.Services;

public sealed class DialogService(IServiceProvider services) : IDialogService
{
    public void Show<TViewModel>(Action<TViewModel> configure)
        where TViewModel : class, IDialogViewModel
    {
        TViewModel viewModel = services.GetRequiredService<TViewModel>();
        configure(viewModel);

        Window dialog = CreateWindow(typeof(TViewModel));
        dialog.DataContext = viewModel;

        if (ResolveOwner() is not { } owner)
        {
            dialog.Show();
            return;
        }

        // Modality is emulated rather than using ShowDialog: on macOS the native backend polls an
        // NSModalSession on the main queue, which starves input dispatch and makes typing lag.
        owner.IsEnabled = false;
        dialog.Closed += (_, _) =>
        {
            owner.IsEnabled = true;
            owner.Activate();
        };

        dialog.Show(owner);
    }

    private static Window? ResolveOwner() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow
            : null;

    // Convention: Features/X/FooDialogViewModel is hosted by Features/X/FooDialog.
    private static Window CreateWindow(Type viewModelType)
    {
        string windowTypeName = viewModelType.FullName!
            .Replace("DialogViewModel", "Dialog", StringComparison.Ordinal);

        Type windowType = viewModelType.Assembly.GetType(windowTypeName)
                          ?? throw new InvalidOperationException(
                              $"No dialog window '{windowTypeName}' found for {viewModelType.Name}.");

        return (Window)Activator.CreateInstance(windowType)!;
    }
}
