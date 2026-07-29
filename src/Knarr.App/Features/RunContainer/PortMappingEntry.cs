namespace Knarr.App.Features.RunContainer;

public partial class PortMappingEntry : ObservableObject
{
    [ObservableProperty]
    public partial string HostPort { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContainerPort { get; set; } = string.Empty;
}
