using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Containers;
using Knarr.App.Models;
using Knarr.Service;
using Knarr.Service.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Knarr.App.Tests.Features.Containers;

public class ContainersViewModelTests
{
    private static readonly IReadOnlyList<Container> _sampleContainers =
    [
        new() { Id = "aaaaaaaaaaaa", Name = "web-api", Image = "nginx:latest", State = ContainerState.Running },
        new() { Id = "bbbbbbbbbbbb", Name = "postgres-db", Image = "postgres:16", State = ContainerState.Running },
        new() { Id = "cccccccccccc", Name = "redis-cache", Image = "redis:7-alpine", State = ContainerState.Running },
        new() { Id = "dddddddddddd", Name = "batch-worker", Image = "worker:dev", State = ContainerState.Exited },
    ];

    private static IContainerCliProvider ProviderWith(IReadOnlyList<Container> containers)
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.ListContainersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(containers));
        return provider;
    }

    private static ContainersViewModel CreateViewModel() => new(ProviderWith(_sampleContainers));

    [Fact]
    public void DefaultState_LoadsContainers()
    {
        ContainersViewModel vm = CreateViewModel();

        Assert.Equal(4, vm.Containers.Count);
        Assert.Contains(vm.Containers, c => c.Name == "web-api");
        Assert.Contains(vm.Containers, c => c.Status == ContainerState.Exited);
    }

    [Fact]
    public void SearchText_FiltersByName()
    {
        ContainersViewModel vm = CreateViewModel();
        vm.SearchText = "redis";

        Assert.Single(vm.Containers);
        Assert.Equal("redis-cache", vm.Containers[0].Name);
    }

    [Fact]
    public void SearchText_FiltersByImage_CaseInsensitive()
    {
        ContainersViewModel vm = CreateViewModel();
        vm.SearchText = "POSTGRES";

        Assert.Single(vm.Containers);
        Assert.Equal("postgres-db", vm.Containers[0].Name);
    }

    [Fact]
    public void SearchText_Cleared_RestoresAllContainers()
    {
        ContainersViewModel vm = CreateViewModel();
        vm.SearchText = "redis";
        Assert.Single(vm.Containers);

        vm.SearchText = string.Empty;

        Assert.Equal(4, vm.Containers.Count);
    }

    [Fact]
    public void LifecycleCommands_DoNotThrow()
    {
        ContainersViewModel vm = CreateViewModel();
        ContainerItem running = vm.Containers.First(c => c.IsRunning);
        ContainerItem stopped = vm.Containers.First(c => !c.IsRunning);

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
        ContainersViewModel vm = CreateViewModel();
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
        ContainersViewModel vm = CreateViewModel();

        vm.Containers[0].IsSelected = true;

        Assert.Null(vm.AllSelected);
    }

    [Fact]
    public void AllSelected_SetTrue_SelectsEveryRow()
    {
        ContainersViewModel vm = CreateViewModel();

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
        ContainersViewModel vm = CreateViewModel();
        vm.AllSelected = true;

        vm.StartSelectedCommand.Execute(null);
        vm.StopSelectedCommand.Execute(null);
        vm.DeleteSelectedCommand.Execute(null);
    }

    [Fact]
    public void StartSelected_RoutesEverySelectedIdInOneProviderCall()
    {
        IContainerCliProvider provider = ProviderWith(_sampleContainers);
        ContainersViewModel vm = new ContainersViewModel(provider);
        vm.Containers[0].IsSelected = true;
        vm.Containers[2].IsSelected = true;

        vm.StartSelectedCommand.Execute(null);

        provider.Received(1).StartContainersAsync(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids != null && ids.Count == 2 && ids.Contains("aaaaaaaaaaaa") && ids.Contains("cccccccccccc")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void StopSelected_RoutesEverySelectedIdInOneProviderCall()
    {
        IContainerCliProvider provider = ProviderWith(_sampleContainers);
        ContainersViewModel vm = new ContainersViewModel(provider);
        vm.AllSelected = true;

        vm.StopSelectedCommand.Execute(null);

        provider.Received(1).StopContainersAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids != null && ids.Count == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DeleteSelected_RoutesEverySelectedIdInOneForcedProviderCall()
    {
        IContainerCliProvider provider = ProviderWith(_sampleContainers);
        ContainersViewModel vm = new ContainersViewModel(provider);
        vm.Containers[1].IsSelected = true;

        vm.DeleteSelectedCommand.Execute(null);

        provider.Received(1).RemoveContainersAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids != null && ids.Count == 1 && ids.Contains("bbbbbbbbbbbb")),
            force: true,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void BulkCommand_WithNoSelection_DoesNotCallProvider()
    {
        IContainerCliProvider provider = ProviderWith(_sampleContainers);
        ContainersViewModel vm = new ContainersViewModel(provider);

        vm.StartSelectedCommand.Execute(null);
        vm.StopSelectedCommand.Execute(null);
        vm.DeleteSelectedCommand.Execute(null);

        provider.DidNotReceive().StartContainersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        provider.DidNotReceive().StopContainersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        provider.DidNotReceive().RemoveContainersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void LoadedState_HasItems()
    {
        ContainersViewModel vm = CreateViewModel();

        Assert.True(vm.HasItems);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void EmptyState_WhenCliReturnsNoContainers()
    {
        ContainersViewModel vm = new ContainersViewModel(ProviderWith([]));

        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasItems);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void ErrorState_WhenCliThrows()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.ListContainersAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("cli unreachable"));

        ContainersViewModel vm = new ContainersViewModel(provider);

        Assert.True(vm.HasError);
        Assert.Equal("cli unreachable", vm.ErrorMessage);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void NoResultsState_WhenSearchMatchesNothing()
    {
        ContainersViewModel vm = CreateViewModel();
        vm.SearchText = "zzz-no-such-container";

        Assert.True(vm.HasNoResults);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void LoadingState_WhileListInFlight()
    {
        TaskCompletionSource<IReadOnlyList<Container>> tcs = new TaskCompletionSource<IReadOnlyList<Container>>();
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.ListContainersAsync(Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        ContainersViewModel vm = new ContainersViewModel(provider);

        Assert.True(vm.IsLoading);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);

        tcs.SetResult([]);

        Assert.False(vm.IsLoading);
        Assert.True(vm.IsEmpty);
    }
}
