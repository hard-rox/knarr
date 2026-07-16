using Knarr.App.Features.Settings;
using Xunit;

namespace Knarr.App.Tests.Features.Settings;

public class SettingsViewModelTests
{
    [Fact]
    public void DefaultState_IsValid()
    {
        var vm = new SettingsViewModel();

        Assert.NotNull(vm);
    }
}
