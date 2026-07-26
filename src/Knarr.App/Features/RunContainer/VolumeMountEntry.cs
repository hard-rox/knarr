namespace Knarr.App.Features.RunContainer;

/// <summary>An editable bind-mount row (host <c>Source</c> to container <c>Target</c>) in the run-container dialog.</summary>
public partial class VolumeMountEntry : ObservableObject
{
    [ObservableProperty]
    public partial string Source { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Target { get; set; } = string.Empty;
}
