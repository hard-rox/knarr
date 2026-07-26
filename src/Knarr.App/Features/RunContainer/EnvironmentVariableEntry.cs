namespace Knarr.App.Features.RunContainer;

/// <summary>An editable environment-variable row (<c>KEY=VALUE</c>) in the run-container dialog.</summary>
public partial class EnvironmentVariableEntry : ObservableObject
{
    [ObservableProperty]
    public partial string Key { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
