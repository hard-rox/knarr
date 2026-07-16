using System.Linq;
using Knarr.App.Features.Containers;
using Knarr.App.Models;
using Xunit;

namespace Knarr.App.Tests.Features.Containers;

public class ContainersViewModelTests
{
    [Fact]
    public void DefaultState_SeedsContainers()
    {
        var vm = new ContainersViewModel();

        Assert.Equal(4, vm.Containers.Count);
        Assert.Contains(vm.Containers, c => c.Name == "web-api");
        Assert.Contains(vm.Containers, c => c.Status == ContainerStatus.Exited);
    }

    [Fact]
    public void SearchText_FiltersByName()
    {
        var vm = new ContainersViewModel { SearchText = "redis" };

        Assert.Single(vm.Containers);
        Assert.Equal("redis-cache", vm.Containers[0].Name);
    }

    [Fact]
    public void SearchText_FiltersByImage_CaseInsensitive()
    {
        var vm = new ContainersViewModel { SearchText = "POSTGRES" };

        Assert.Single(vm.Containers);
        Assert.Equal("postgres-db", vm.Containers[0].Name);
    }

    [Fact]
    public void SearchText_Cleared_RestoresAllContainers()
    {
        var vm = new ContainersViewModel { SearchText = "redis" };
        Assert.Single(vm.Containers);

        vm.SearchText = string.Empty;

        Assert.Equal(4, vm.Containers.Count);
    }

    [Fact]
    public void LifecycleCommands_DoNotThrow()
    {
        var vm = new ContainersViewModel();
        var running = vm.Containers.First(c => c.IsRunning);
        var stopped = vm.Containers.First(c => !c.IsRunning);

        vm.RefreshCommand.Execute(null);
        vm.RunContainerCommand.Execute(null);
        vm.StartCommand.Execute(stopped);
        vm.StopCommand.Execute(running);
        vm.RestartCommand.Execute(running);
        vm.KillCommand.Execute(running);
        vm.RemoveCommand.Execute(stopped);
        vm.LogsCommand.Execute(running);
        vm.ExecCommand.Execute(running);
        vm.InspectCommand.Execute(stopped);
    }
}
