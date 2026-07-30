namespace Knarr.App.Features.RunContainer;

/// <summary>An editable port-mapping row (host port to container port) in the run-container dialog.</summary>
public partial class PortMappingEntry : ObservableObject
{
    [ObservableProperty]
    public partial string HostPort { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContainerPort { get; set; } = string.Empty;
}
