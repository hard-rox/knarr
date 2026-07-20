using Knarr.App.Features.Dashboard;

namespace Knarr.App.Tests.Features.Dashboard;

public class DashboardViewModelTests
{
    [Fact]
    public void DefaultState_IsValid()
    {
        var vm = new DashboardViewModel();

        Assert.NotNull(vm);
    }
}
