using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Images;
using Knarr.App.Models;
using Knarr.Service;
using Knarr.Service.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Knarr.App.Tests.Features.Images;

public class ImagesViewModelTests
{
    private static readonly IReadOnlyList<ContainerImage> _sampleImages =
    [
        new() { Id = "111111111111", Repository = "nginx", Tag = "latest", Size = "187 MB" },
        new() { Id = "222222222222", Repository = "postgres", Tag = "16", Size = "438 MB" },
        new() { Id = "333333333333", Repository = "redis", Tag = "7-alpine", Size = "41 MB" },
        new() { Id = "444444444444", Repository = "worker", Tag = "dev", Size = "312 MB" },
        new() { Id = "555555555555", Repository = "ubuntu", Tag = "24.04", Size = "78 MB" },
    ];

    private static IContainerCliProvider ProviderWith(IReadOnlyList<ContainerImage> images)
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.ListImagesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(images));
        return provider;
    }

    private static ImagesViewModel CreateViewModel() => new(ProviderWith(_sampleImages));

    [Fact]
    public void DefaultState_LoadsImages()
    {
        ImagesViewModel vm = CreateViewModel();

        Assert.Equal(5, vm.Images.Count);
        Assert.Contains(vm.Images, i => i.Repository == "nginx");
        Assert.Contains(vm.Images, i => i.Tag == "7-alpine");
    }

    [Fact]
    public void SearchText_FiltersByRepository()
    {
        ImagesViewModel vm = CreateViewModel();
        vm.SearchText = "redis";

        Assert.Single(vm.Images);
        Assert.Equal("redis", vm.Images[0].Repository);
    }

    [Fact]
    public void SearchText_FiltersByTag_CaseInsensitive()
    {
        ImagesViewModel vm = CreateViewModel();
        vm.SearchText = "LATEST";

        Assert.Single(vm.Images);
        Assert.Equal("nginx", vm.Images[0].Repository);
    }

    [Fact]
    public void SearchText_Cleared_RestoresAllImages()
    {
        ImagesViewModel vm = CreateViewModel();
        vm.SearchText = "redis";
        Assert.Single(vm.Images);

        vm.SearchText = string.Empty;

        Assert.Equal(5, vm.Images.Count);
    }

    [Fact]
    public void ToolbarAndRowCommands_DoNotThrow()
    {
        ImagesViewModel vm = CreateViewModel();
        ImageItem image = vm.Images.First();

        vm.RefreshCommand.Execute(null);
        vm.BuildCommand.Execute(null);
        vm.PullCommand.Execute(null);
        vm.ImportCommand.Execute(null);
        vm.RunCommand.Execute(image);
        vm.TagCommand.Execute(image);
        vm.InspectCommand.Execute(image);
        vm.RemoveCommand.Execute(image);
    }

    [Fact]
    public void Selection_TracksCountAndHasSelection()
    {
        ImagesViewModel vm = CreateViewModel();
        Assert.False(vm.HasSelection);
        Assert.Equal(0, vm.SelectedCount);

        vm.Images[0].IsSelected = true;
        vm.Images[1].IsSelected = true;

        Assert.True(vm.HasSelection);
        Assert.Equal(2, vm.SelectedCount);
        Assert.Equal(2, vm.SelectedImages.Count);
    }

    [Fact]
    public void AllSelected_IsNull_WhenSelectionIsMixed()
    {
        ImagesViewModel vm = CreateViewModel();

        vm.Images[0].IsSelected = true;

        Assert.Null(vm.AllSelected);
    }

    [Fact]
    public void AllSelected_SetTrue_SelectsEveryRow()
    {
        ImagesViewModel vm = CreateViewModel();

        vm.AllSelected = true;

        Assert.True(vm.AllSelected);
        Assert.Equal(vm.Images.Count, vm.SelectedCount);
        Assert.All(vm.Images, i => Assert.True(i.IsSelected));

        vm.AllSelected = false;

        Assert.False(vm.AllSelected);
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public void BulkCommands_DoNotThrow()
    {
        ImagesViewModel vm = CreateViewModel();
        vm.AllSelected = true;

        vm.DeleteSelectedCommand.Execute(null);
    }

    [Fact]
    public void LoadedState_HasItems()
    {
        ImagesViewModel vm = CreateViewModel();

        Assert.True(vm.HasItems);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void EmptyState_WhenCliReturnsNoImages()
    {
        ImagesViewModel vm = new ImagesViewModel(ProviderWith([]));

        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasItems);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void ErrorState_WhenCliThrows()
    {
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.ListImagesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("cli unreachable"));

        ImagesViewModel vm = new ImagesViewModel(provider);

        Assert.True(vm.HasError);
        Assert.Equal("cli unreachable", vm.ErrorMessage);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void NoResultsState_WhenSearchMatchesNothing()
    {
        ImagesViewModel vm = CreateViewModel();
        vm.SearchText = "zzz-no-such-image";

        Assert.True(vm.HasNoResults);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void LoadingState_WhileListInFlight()
    {
        TaskCompletionSource<IReadOnlyList<ContainerImage>> tcs = new TaskCompletionSource<IReadOnlyList<ContainerImage>>();
        IContainerCliProvider provider = Substitute.For<IContainerCliProvider>();
        provider.ListImagesAsync(Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        ImagesViewModel vm = new ImagesViewModel(provider);

        Assert.True(vm.IsLoading);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);

        tcs.SetResult([]);

        Assert.False(vm.IsLoading);
        Assert.True(vm.IsEmpty);
    }
}
