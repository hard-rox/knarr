namespace Knarr.App.Features.RunContainer;

public partial class VolumeMountEntry : ObservableObject
{
    [ObservableProperty] public partial string Source { get; set; } = string.Empty;

    [ObservableProperty] public partial string Target { get; set; } = string.Empty;
}
