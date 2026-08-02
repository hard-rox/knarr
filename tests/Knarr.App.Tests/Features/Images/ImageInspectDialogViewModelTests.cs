using System;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Images;
using Knarr.Service;
using Knarr.Service.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Knarr.App.Tests.Features.Images;

public class ImageInspectDialogViewModelTests
{
    private static readonly ImageInspection _sample = new()
    {
        Id = "sha256:d9f3c2aa7439bef41152d378fbd0f569f6c751db9b782fb63598cf23fd125c51",
        ShortId = "d9f3c2aa7439",
        RepoTags = ["redis:latest"],
        Architecture = "amd64",
        Os = "linux",
        Size = "136 MB",
        Entrypoint = ["docker-entrypoint.sh"],
        Command = ["redis-server"],
        ExposedPorts = ["6379/tcp"],
        EnvironmentVariables = [new InspectionEntry("REDIS_VERSION", "8.8.1")],
        RawJson = "[\n  {}\n]",
    };

    private static IContainerCliProvider ProviderWith(ImageInspection inspection)
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.InspectImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(inspection));
        return provider;
    }

    [Fact]
    public void Reset_PopulatesInspectionAndDisplayProperties()
    {
        var vm = new ImageInspectDialogViewModel(ProviderWith(_sample), NullLogger<ImageInspectDialogViewModel>.Instance);

        vm.Reset("redis:latest");

        Assert.Equal("redis:latest", vm.ImageReference);
        Assert.True(vm.HasInspection);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal("d9f3c2aa7439", vm.ShortId);
        Assert.Equal("amd64", vm.Architecture);
        Assert.Equal("docker-entrypoint.sh", vm.EntrypointText);
        Assert.Equal("redis-server", vm.CommandText);
        Assert.True(vm.HasExposedPorts);
        Assert.True(vm.HasEnvironmentVariables);
    }

    [Fact]
    public void Reset_WhenProviderThrows_SetsErrorMessage()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.InspectImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("no such image"));

        var vm = new ImageInspectDialogViewModel(provider, NullLogger<ImageInspectDialogViewModel>.Instance);
        vm.Reset("missing:latest");

        Assert.True(vm.HasError);
        Assert.Equal("no such image", vm.ErrorMessage);
        Assert.False(vm.HasInspection);
    }

    [Fact]
    public void MissingValues_RenderAsDash()
    {
        var vm = new ImageInspectDialogViewModel(
            ProviderWith(new ImageInspection { Id = string.Empty }),
            NullLogger<ImageInspectDialogViewModel>.Instance);

        vm.Reset("x");

        Assert.Equal("\u2014", vm.AuthorText);
        Assert.Equal("\u2014", vm.EntrypointText);
        Assert.Equal("\u2014", vm.CreatedText);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        var vm = new ImageInspectDialogViewModel(ProviderWith(_sample), NullLogger<ImageInspectDialogViewModel>.Instance);
        bool closed = false;
        vm.CloseRequested += (_, _) => closed = true;

        vm.CloseCommand.Execute(null);

        Assert.True(closed);
    }
}
