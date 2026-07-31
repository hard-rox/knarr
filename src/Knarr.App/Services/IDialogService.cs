namespace Knarr.App.Services;

/// <summary>
/// Opens dialog windows on behalf of view models, keeping them free of Avalonia types.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Resolves <typeparamref name="TViewModel"/> from the container, applies
    /// <paramref name="configure"/> to it, then shows the matching dialog window.
    /// </summary>
    void Show<TViewModel>(Action<TViewModel> configure)
        where TViewModel : class, IDialogViewModel;
}
