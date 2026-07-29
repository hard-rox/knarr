using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Controls;
using Knarr.App.Features.Containers.ContainerLogs;
using Knarr.Service;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Knarr.App.Tests.Features.Containers.ContainerLogs;

public class ContainerLogsDialogViewModelTests
{
    private static async IAsyncEnumerable<CliOutputLine> FiniteLines()
    {
        yield return CliOutputLine.ForCommand("wslc logs abc123");
        yield return CliOutputLine.ForStandardOutput("hello world");
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<CliOutputLine> InfiniteFollow(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return CliOutputLine.ForStandardOutput("first line");
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan? timeout = null)
    {
        TimeSpan remaining = timeout ?? TimeSpan.FromSeconds(2);
        while (!predicate() && remaining > TimeSpan.Zero)
        {
            await Task.Delay(10);
            remaining -= TimeSpan.FromMilliseconds(10);
        }

        Assert.True(predicate(), "Condition was not met within the timeout.");
    }

    [Fact]
    public async Task Reset_AutoStartsStreamAndPopulatesOutputLines()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.StreamContainerLogsAsync(Arg.Any<ContainerLogsOptions>(), Arg.Any<CancellationToken>())
            .Returns(FiniteLines());

        ContainerLogsDialogViewModel vm = new(provider, NullLogger<ContainerLogsDialogViewModel>.Instance);

        vm.Reset("abc123", "web-api");

        await WaitUntilAsync(() => vm.OutputLines.Count == 2);
        Assert.Equal("web-api", vm.ContainerName);
        Assert.Equal(TerminalState.Success, vm.TerminalState);
        Assert.True(vm.LimitTail);
        Assert.Equal(200, vm.TailLines);
        provider.Received().StreamContainerLogsAsync(
            Arg.Is<ContainerLogsOptions>(o => o != null && o.TailLines == 200),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stop_CancelsAnInFlightFollowStream()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.StreamContainerLogsAsync(Arg.Any<ContainerLogsOptions>(), Arg.Any<CancellationToken>())
            .Returns(ci => InfiniteFollow(ci.ArgAt<CancellationToken>(1)));

        ContainerLogsDialogViewModel vm = new(provider, NullLogger<ContainerLogsDialogViewModel>.Instance);
        vm.Reset("abc123", "web-api");
        vm.Follow = true;

        await WaitUntilAsync(() => vm.IsStreaming);

        vm.StopCommand.Execute(null);

        await WaitUntilAsync(() => !vm.IsStreaming);
        Assert.Equal(TerminalState.Canceled, vm.TerminalState);
    }

    [Fact]
    public void OptionChange_TriggersNewStreamCallWithUpdatedOptions()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.StreamContainerLogsAsync(Arg.Any<ContainerLogsOptions>(), Arg.Any<CancellationToken>())
            .Returns(FiniteLines());

        ContainerLogsDialogViewModel vm = new(provider, NullLogger<ContainerLogsDialogViewModel>.Instance);
        vm.Reset("abc123", "web-api");

        vm.TailLines = 500;

        provider.Received().StreamContainerLogsAsync(
            Arg.Is<ContainerLogsOptions>(o => o != null && o.ContainerId == "abc123" && o.TailLines == 500),
            Arg.Any<CancellationToken>());
    }
}
