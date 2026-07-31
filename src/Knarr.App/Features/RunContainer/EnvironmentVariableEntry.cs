namespace Knarr.App.Features.RunContainer;

public partial class EnvironmentVariableEntry : ObservableObject
{
    [ObservableProperty] public partial string Key { get; set; } = string.Empty;

    [ObservableProperty] public partial string Value { get; set; } = string.Empty;
}
