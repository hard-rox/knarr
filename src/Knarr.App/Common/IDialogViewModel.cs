namespace Knarr.App.Common;

/// <summary>
/// Implemented by view models hosted in a <see cref="DialogWindow"/>. Raising
/// <see cref="CloseRequested"/> closes the hosting window without the view model
/// referencing any UI type.
/// </summary>
public interface IDialogViewModel
{
    public event EventHandler? CloseRequested;
}
