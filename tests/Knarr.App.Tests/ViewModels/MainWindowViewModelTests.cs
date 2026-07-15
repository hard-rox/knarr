using Knarr.App.ViewModels;
using Xunit;

namespace Knarr.App.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Greeting_ReturnsExpectedText()
    {
        var vm = new MainWindowViewModel();

        Assert.Equal("Welcome to Avalonia!", vm.Greeting);
    }
}
