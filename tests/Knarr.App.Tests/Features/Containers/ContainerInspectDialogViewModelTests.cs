using System;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Containers.ContainerInspect;
using Knarr.Service;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Knarr.App.Tests.Features.Containers;

public class ContainerInspectDialogViewModelTests
{
    private static readonly ContainerInspection _sample = new()
    {
        Id = "c270199300c65415cde5e0ac8db14b6c4e2da7213e984de2084cbae608034a69",
        ShortId = "c270199300c6",
        Name = "whispering_rockies",
        Image = "chentex/random-logger:latest",
        Status = "running",
        IsRunning = true,
        ExitCode = 0,
        NetworkMode = "bridge",
        Networks = [new ContainerNetworkInfo("bridge", "172.17.0.1", "172.17.0.2", 16, "02:42:ac:11:00:02", [])],
        EnvironmentVariables = [new InspectionEntry("PATH", "/usr/local/sbin")],
        Labels = [new InspectionEntry("org.opencontainers.image.title", "random-logger")],
        RawJson = "[\n  {}\n]",
    };

    private static IContainerCliProvider ProviderWith(ContainerInspection inspection)
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.InspectContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(inspection));
        return provider;
    }

    [Fact]
    public void Reset_PopulatesInspectionAndDisplayProperties()
    {
        var vm = new ContainerInspectDialogViewModel(ProviderWith(_sample), NullLogger<ContainerInspectDialogViewModel>.Instance);

        vm.Reset("c270199300c6", "whispering_rockies");

        Assert.Equal("whispering_rockies", vm.ContainerName);
        Assert.True(vm.HasInspection);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal("running", vm.StatusText);
        Assert.True(vm.IsRunning);
        Assert.Equal("bridge", vm.NetworkModeText);
        Assert.True(vm.HasNetworks);
        Assert.True(vm.HasLabels);
        Assert.Equal("\u2014", vm.FinishedText);
    }

    [Fact]
    public void Reset_WhenProviderThrows_SetsErrorMessage()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.InspectContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("no such container"));

        var vm = new ContainerInspectDialogViewModel(provider, NullLogger<ContainerInspectDialogViewModel>.Instance);
        vm.Reset("missing", "missing");

        Assert.True(vm.HasError);
        Assert.Equal("no such container", vm.ErrorMessage);
        Assert.False(vm.HasInspection);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        var vm = new ContainerInspectDialogViewModel(ProviderWith(_sample), NullLogger<ContainerInspectDialogViewModel>.Instance);
        bool closed = false;
        vm.CloseRequested += (_, _) => closed = true;

        vm.CloseCommand.Execute(null);

        Assert.True(closed);
    }
}
