using Knarr.App.Features.Settings;

namespace Knarr.App.Tests.Features.Settings;

public class SettingsViewModelTests
{
    [Fact]
    public void DefaultState_IsValid()
    {
        SettingsViewModel vm = new();

        Assert.NotNull(vm);
    }
}
