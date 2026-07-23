using System;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Images;
using Knarr.Service;
using Knarr.Service.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Knarr.App.Tests.Features.Images;

public class PullImageDialogViewModelTests
{
    private const string ValidReference = "docker.io/library/alpine:3.20";

    private static PullImageDialogViewModel CreateViewModel(out IContainerCliProvider provider)
    {
        provider = Substitute.For<IContainerCliProvider>();
        return new PullImageDialogViewModel(provider, NullLogger<PullImageDialogViewModel>.Instance);
    }

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
    public async Task Pull_Success_SetsStatusMessageAndRaisesEvent()
    {
        PullImageDialogViewModel vm = CreateViewModel(out IContainerCliProvider provider);
        provider
            .PullImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        vm.ImageReference = ValidReference;

        var succeeded = false;
        vm.PullSucceeded += (_, _) => succeeded = true;

        await vm.PullCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.Contains("Pulled", vm.StatusMessage);
        Assert.True(succeeded);
    }

    [Fact]
    public async Task Pull_CommandFailure_SetsErrorStatusMessage()
    {
        PullImageDialogViewModel vm = CreateViewModel(out IContainerCliProvider provider);
        var exception = new CliCommandException("wslc pull alpine", 1, "manifest not found");
        provider
            .PullImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));
        vm.ImageReference = ValidReference;

        var succeeded = false;
        vm.PullSucceeded += (_, _) => succeeded = true;

        await vm.PullCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.Equal(exception.Message, vm.StatusMessage);
        Assert.False(succeeded);
    }

    [Fact]
    public async Task Pull_Canceled_SetsCanceledStatusMessage()
    {
        PullImageDialogViewModel vm = CreateViewModel(out IContainerCliProvider provider);
        provider
            .PullImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new OperationCanceledException()));
        vm.ImageReference = ValidReference;

        await vm.PullCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.Equal("Pull canceled.", vm.StatusMessage);
    }

    [Fact]
    public void Reset_ClearsSessionAndSeedsReference()
    {
        PullImageDialogViewModel vm = CreateViewModel(out _);
        vm.StatusMessage = "stale";

        vm.Reset("nginx:latest");

        Assert.Equal("nginx:latest", vm.ImageReference);
        Assert.Null(vm.StatusMessage);
        Assert.False(vm.IsRunning);
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

