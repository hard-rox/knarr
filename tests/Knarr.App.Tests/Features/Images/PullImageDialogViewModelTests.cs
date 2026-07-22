using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Controls;
using Knarr.App.Features.Images;
using Knarr.Service;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Knarr.App.Tests.Features.Images;

public class PullImageDialogViewModelTests
{
    private const string ValidReference = "docker.io/library/alpine:3.20";

    private static PullImageDialogViewModel CreateViewModel(
        out IContainerCliProvider provider,
        IEnumerable<CliOutputLine>? streamed = null)
    {
        provider = Substitute.For<IContainerCliProvider>();
        if (streamed is not null)
        {
            provider
                .PullImageStreamingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(_ => ToAsyncStream(streamed));
        }

        return new PullImageDialogViewModel(provider, NullLogger<PullImageDialogViewModel>.Instance);
    }

    private static async IAsyncEnumerable<CliOutputLine> ToAsyncStream(
        IEnumerable<CliOutputLine> lines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (CliOutputLine line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return line;
        }
    }

#pragma warning disable CS1998 // async iterator intentionally has no awaits before throwing
    private static async IAsyncEnumerable<CliOutputLine> ThrowsCanceledStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return CliOutputLine.ForCommand("container image pull alpine");
        throw new OperationCanceledException();
    }
#pragma warning restore CS1998

    [Theory]
    [InlineData("alpine")]
    [InlineData("nginx:latest")]
    [InlineData("docker.io/library/alpine:3.20")]
    [InlineData("ghcr.io/user/app:1.2.3")]
    [InlineData("localhost:5000/team/service:dev")]
    public void CanPull_IsTrue_ForValidReferences(string reference)
    {
        PullImageDialogViewModel vm = CreateViewModel(out _);
        vm.ImageReference = reference;

        Assert.True(vm.PullCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UPPERCASE")]
    [InlineData("bad reference")]
    [InlineData(":::")]
    public void CanPull_IsFalse_ForInvalidReferences(string reference)
    {
        PullImageDialogViewModel vm = CreateViewModel(out _);
        vm.ImageReference = reference;

        Assert.False(vm.PullCommand.CanExecute(null));
    }

    [Fact]
    public async Task Pull_Success_SetsSuccessStateAndRaisesEvent()
    {
        var lines = new[]
        {
            CliOutputLine.ForCommand("container image pull alpine"),
            CliOutputLine.ForStandardOutput("Downloading layers..."),
            CliOutputLine.ForExit(0),
        };
        PullImageDialogViewModel vm = CreateViewModel(out _, lines);
        vm.ImageReference = ValidReference;

        var succeeded = false;
        vm.PullSucceeded += (_, _) => succeeded = true;

        await vm.PullCommand.ExecuteAsync(null);

        Assert.Equal(TerminalState.Success, vm.State);
        Assert.False(vm.IsRunning);
        Assert.Equal(3, vm.Output.Count);
        Assert.True(succeeded);
    }

    [Fact]
    public async Task Pull_NonZeroExit_SetsErrorState()
    {
        var lines = new[]
        {
            CliOutputLine.ForCommand("container image pull alpine"),
            CliOutputLine.ForStandardError("manifest not found"),
            CliOutputLine.ForExit(1),
        };
        PullImageDialogViewModel vm = CreateViewModel(out _, lines);
        vm.ImageReference = ValidReference;

        var succeeded = false;
        vm.PullSucceeded += (_, _) => succeeded = true;

        await vm.PullCommand.ExecuteAsync(null);

        Assert.Equal(TerminalState.Error, vm.State);
        Assert.False(vm.IsRunning);
        Assert.False(succeeded);
    }

    [Fact]
    public async Task Pull_Canceled_SetsCanceledState()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider
            .PullImageStreamingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => ThrowsCanceledStream());
        PullImageDialogViewModel vm =
            new(provider, NullLogger<PullImageDialogViewModel>.Instance) { ImageReference = ValidReference };

        await vm.PullCommand.ExecuteAsync(null);

        Assert.Equal(TerminalState.Canceled, vm.State);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task Pull_CapsOutputToMaxLines()
    {
        var lines = new List<CliOutputLine> { CliOutputLine.ForCommand("container image pull alpine") };
        for (var i = 0; i < 6000; i++)
        {
            lines.Add(CliOutputLine.ForStandardOutput($"layer {i}"));
        }

        lines.Add(CliOutputLine.ForExit(0));

        PullImageDialogViewModel vm = CreateViewModel(out _, lines);
        vm.ImageReference = ValidReference;

        await vm.PullCommand.ExecuteAsync(null);

        Assert.Equal(5000, vm.Output.Count);
        Assert.True(vm.IsTruncated);
        Assert.False(string.IsNullOrEmpty(vm.TruncationNote));
    }

    [Fact]
    public void CopyOutput_RaisesCopyRequestedWithTranscript()
    {
        var lines = new[]
        {
            CliOutputLine.ForCommand("container image pull alpine"),
            CliOutputLine.ForStandardOutput("Downloading layers..."),
        };
        PullImageDialogViewModel vm = CreateViewModel(out _);
        foreach (CliOutputLine line in lines)
        {
            vm.Output.Add(line);
        }

        string? copied = null;
        vm.CopyRequested += (_, text) => copied = text;

        vm.CopyOutputCommand.Execute(null);

        Assert.NotNull(copied);
        Assert.Contains("container image pull alpine", copied);
        Assert.Contains("Downloading layers...", copied);
    }

    [Fact]
    public void Reset_ClearsSessionAndSeedsReference()
    {
        PullImageDialogViewModel vm = CreateViewModel(out _);
        vm.Output.Add(CliOutputLine.ForStandardOutput("stale"));
        vm.IsTruncated = true;
        vm.State = TerminalState.Error;

        vm.Reset("nginx:latest");

        Assert.Equal("nginx:latest", vm.ImageReference);
        Assert.Empty(vm.Output);
        Assert.False(vm.IsTruncated);
        Assert.Equal(TerminalState.Idle, vm.State);
    }

    [Fact]
    public void Close_RaisesCloseRequested()
    {
        PullImageDialogViewModel vm = CreateViewModel(out _);

        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;

        vm.CloseCommand.Execute(null);

        Assert.True(closed);
    }
}
