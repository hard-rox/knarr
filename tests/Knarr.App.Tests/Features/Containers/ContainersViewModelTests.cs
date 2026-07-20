using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Containers;
using Knarr.Service;
using Knarr.Service.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Knarr.App.Tests.Features.Containers;

public class ContainersViewModelTests
{
    [Fact]
    public void DefaultState_SeedsContainers()
    {
        var vm = new ContainersViewModel();

        Assert.Equal(19, vm.Containers.Count);
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

        Assert.Equal(19, vm.Containers.Count);
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
        vm.RemoveCommand.Execute(stopped);
        vm.LogsCommand.Execute(running);
        vm.ExecCommand.Execute(running);
        vm.InspectCommand.Execute(stopped);
    }

    [Fact]
    public void Selection_TracksCountAndHasSelection()
    {
        var vm = new ContainersViewModel();
        Assert.False(vm.HasSelection);
        Assert.Equal(0, vm.SelectedCount);

        vm.Containers[0].IsSelected = true;
        vm.Containers[1].IsSelected = true;

        Assert.True(vm.HasSelection);
        Assert.Equal(2, vm.SelectedCount);
        Assert.Equal(2, vm.SelectedContainers.Count);
    }

    [Fact]
    public void AllSelected_IsNull_WhenSelectionIsMixed()
    {
        var vm = new ContainersViewModel();

        vm.Containers[0].IsSelected = true;

        Assert.Null(vm.AllSelected);
    }

    [Fact]
    public void AllSelected_SetTrue_SelectsEveryRow()
    {
        var vm = new ContainersViewModel();

        vm.AllSelected = true;

        Assert.True(vm.AllSelected);
        Assert.Equal(vm.Containers.Count, vm.SelectedCount);
        Assert.All(vm.Containers, c => Assert.True(c.IsSelected));

        vm.AllSelected = false;

        Assert.False(vm.AllSelected);
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public void BulkCommands_DoNotThrow()
    {
        var vm = new ContainersViewModel();
        vm.AllSelected = true;

        vm.StartSelectedCommand.Execute(null);
        vm.StopSelectedCommand.Execute(null);
        vm.DeleteSelectedCommand.Execute(null);
    }

    [Fact]
    public void LoadedState_HasItems()
    {
        var vm = new ContainersViewModel();

        Assert.True(vm.HasItems);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void EmptyState_WhenCliReturnsNoContainers()
    {
        var provider = Substitute.For<IContainerCliProvider>();
        provider.ListContainersAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContainerSummary>>([]));

        var vm = new ContainersViewModel(provider);

        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasItems);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void ErrorState_WhenCliThrows()
    {
        var provider = Substitute.For<IContainerCliProvider>();
        provider.ListContainersAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("cli unreachable"));

        var vm = new ContainersViewModel(provider);

        Assert.True(vm.HasError);
        Assert.Equal("cli unreachable", vm.ErrorMessage);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void NoResultsState_WhenSearchMatchesNothing()
    {
        var vm = new ContainersViewModel { SearchText = "zzz-no-such-container" };

        Assert.True(vm.HasNoResults);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void LoadingState_WhileListInFlight()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<ContainerSummary>>();
        var provider = Substitute.For<IContainerCliProvider>();
        provider.ListContainersAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var vm = new ContainersViewModel(provider);

        Assert.True(vm.IsLoading);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);

        tcs.SetResult([]);

        Assert.False(vm.IsLoading);
        Assert.True(vm.IsEmpty);
    }
}
