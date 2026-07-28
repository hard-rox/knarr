using System;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Controls;
using Knarr.App.Features.RunContainer;
using Knarr.Service;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Knarr.App.Tests.Features.RunContainer;

public class RunContainerDialogViewModelTests
{
    private static RunContainerDialogViewModel CreateViewModel(out IContainerCliProvider provider, bool supportsPublishAllPorts = true)
    {
        provider = Substitute.For<IContainerCliProvider>();
        provider.SupportsPublishAllPorts.Returns(supportsPublishAllPorts);
        provider
            .BuildRunContainerCommand(Arg.Any<RunContainerOptions>())
            .Returns(ci => Describe(ci.Arg<RunContainerOptions>()));
        return new RunContainerDialogViewModel(provider, NullLogger<RunContainerDialogViewModel>.Instance);
    }

    private static string Describe(RunContainerOptions options)
        => $"{(options.Detach ? "d" : "-")}|{options.ImageReference}|env={options.EnvironmentVariables.Count}|vol={options.Volumes.Count}";

    [Fact]
    public void Defaults_DetachOnRemoveOff()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);

        Assert.True(vm.Detach);
        Assert.False(vm.RemoveOnExit);
        Assert.Empty(vm.EnvironmentVariables);
        Assert.Empty(vm.Volumes);
        Assert.True(vm.SupportsPublishAllPorts);
    }

    [Fact]
    public void UnsupportedPublishAll_HidesCapabilityAndKeepsAddPortAvailable()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _, supportsPublishAllPorts: false);

        vm.PublishAllPorts = true;

        Assert.False(vm.SupportsPublishAllPorts);
        Assert.True(vm.CanAddPort);
    }

    [Fact]
    public void CanRun_RequiresImageReference()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);
        Assert.False(vm.RunCommand.CanExecute(null));

        vm.ImageReference = "alpine:3.20";
        Assert.True(vm.RunCommand.CanExecute(null));

        vm.ImageReference = "   ";
        Assert.False(vm.RunCommand.CanExecute(null));
    }

    [Fact]
    public void AddAndRemove_EnvironmentVariables_MutatesCollection()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);

        vm.AddEnvironmentVariableCommand.Execute(null);
        Assert.Single(vm.EnvironmentVariables);

        EnvironmentVariableEntry entry = vm.EnvironmentVariables[0];
        vm.RemoveEnvironmentVariableCommand.Execute(entry);
        Assert.Empty(vm.EnvironmentVariables);
    }

    [Fact]
    public void AddAndRemove_Volumes_MutatesCollection()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);

        vm.AddVolumeCommand.Execute(null);
        Assert.Single(vm.Volumes);

        VolumeMountEntry entry = vm.Volumes[0];
        vm.RemoveVolumeCommand.Execute(entry);
        Assert.Empty(vm.Volumes);
    }

    [Fact]
    public void CommandPreview_UpdatesOnInputChange()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);

        vm.ImageReference = "nginx:latest";
        Assert.Equal("d|nginx:latest|env=0|vol=0", vm.CommandPreview);

        vm.Detach = false;
        Assert.Equal("-|nginx:latest|env=0|vol=0", vm.CommandPreview);
    }

    [Fact]
    public void CommandPreview_UpdatesWhenEntryEdited()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);
        vm.ImageReference = "alpine";

        vm.AddEnvironmentVariableCommand.Execute(null);

        // A blank-key row is skipped when building options, so it is not yet counted.
        Assert.Equal("d|alpine|env=0|vol=0", vm.CommandPreview);

        // Editing the key makes the row valid; the preview recomputes via the entry's change notification.
        vm.EnvironmentVariables[0].Key = "KEY";
        Assert.Equal("d|alpine|env=1|vol=0", vm.CommandPreview);
    }

    [Fact]
    public async Task Run_Detached_RaisesContainerStartedAndSetsStatus()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out IContainerCliProvider provider);
        provider
            .RunContainerAsync(Arg.Any<RunContainerOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("abc123"));
        vm.ImageReference = "alpine:3.20";

        var started = false;
        vm.ContainerStarted += (_, _) => started = true;

        await vm.RunCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.True(started);
        Assert.Contains("abc123", vm.StatusMessage);
    }

    [Fact]
    public async Task Run_Detached_Success_RaisesCloseRequested()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out IContainerCliProvider provider);
        provider
            .RunContainerAsync(Arg.Any<RunContainerOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("abc123"));
        vm.ImageReference = "alpine:3.20";

        var closeRequested = false;
        vm.CloseRequested += (_, _) => closeRequested = true;

        await vm.RunCommand.ExecuteAsync(null);

        Assert.True(closeRequested);
    }

    [Fact]
    public async Task Run_Detached_Failure_SetsErrorStatus()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out IContainerCliProvider provider);
        provider
            .RunContainerAsync(Arg.Any<RunContainerOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("boom")));
        vm.ImageReference = "alpine:3.20";

        var started = false;
        vm.ContainerStarted += (_, _) => started = true;

        await vm.RunCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.False(started);
        Assert.Equal("boom", vm.StatusMessage);
    }

    [Fact]
    public void Reset_ClearsSessionAndSeedsImage()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);
        vm.AddEnvironmentVariableCommand.Execute(null);
        vm.AddVolumeCommand.Execute(null);
        vm.RemoveOnExit = true;
        vm.Detach = false;
        vm.StatusMessage = "stale";

        vm.Reset("redis:7");

        Assert.Equal("redis:7", vm.ImageReference);
        Assert.True(vm.Detach);
        Assert.False(vm.RemoveOnExit);
        Assert.Empty(vm.EnvironmentVariables);
        Assert.Empty(vm.Volumes);
        Assert.Null(vm.StatusMessage);
        Assert.Equal(TerminalState.Idle, vm.TerminalState);
    }

    [Fact]
    public void Close_RaisesCloseRequested()
    {
        RunContainerDialogViewModel vm = CreateViewModel(out _);

        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;

        vm.CloseCommand.Execute(null);

        Assert.True(closed);
    }
}
