using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Images;
using Knarr.Service;
using Knarr.Service.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Knarr.App.Tests.Features.Images;

public class ImagesViewModelTests
{
    [Fact]
    public void DefaultState_SeedsImages()
    {
        var vm = new ImagesViewModel();

        Assert.Equal(5, vm.Images.Count);
        Assert.Contains(vm.Images, i => i.Repository == "nginx");
        Assert.Contains(vm.Images, i => i.Tag == "7-alpine");
    }

    [Fact]
    public void SearchText_FiltersByRepository()
    {
        var vm = new ImagesViewModel { SearchText = "redis" };

        Assert.Single(vm.Images);
        Assert.Equal("redis", vm.Images[0].Repository);
    }

    [Fact]
    public void SearchText_FiltersByTag_CaseInsensitive()
    {
        var vm = new ImagesViewModel { SearchText = "LATEST" };

        Assert.Single(vm.Images);
        Assert.Equal("nginx", vm.Images[0].Repository);
    }

    [Fact]
    public void SearchText_Cleared_RestoresAllImages()
    {
        var vm = new ImagesViewModel { SearchText = "redis" };
        Assert.Single(vm.Images);

        vm.SearchText = string.Empty;

        Assert.Equal(5, vm.Images.Count);
    }

    [Fact]
    public void ToolbarAndRowCommands_DoNotThrow()
    {
        var vm = new ImagesViewModel();
        var image = vm.Images.First();

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
        var vm = new ImagesViewModel();
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
        var vm = new ImagesViewModel();

        vm.Images[0].IsSelected = true;

        Assert.Null(vm.AllSelected);
    }

    [Fact]
    public void AllSelected_SetTrue_SelectsEveryRow()
    {
        var vm = new ImagesViewModel();

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
        var vm = new ImagesViewModel();
        vm.AllSelected = true;

        vm.DeleteSelectedCommand.Execute(null);
    }

    [Fact]
    public void LoadedState_HasItems()
    {
        var vm = new ImagesViewModel();

        Assert.True(vm.HasItems);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void EmptyState_WhenCliReturnsNoImages()
    {
        var provider = Substitute.For<IContainerCliProvider>();
        provider.ListImagesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ImageSummary>>([]));

        var vm = new ImagesViewModel(provider);

        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasItems);
        Assert.False(vm.HasError);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void ErrorState_WhenCliThrows()
    {
        var provider = Substitute.For<IContainerCliProvider>();
        provider.ListImagesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("cli unreachable"));

        var vm = new ImagesViewModel(provider);

        Assert.True(vm.HasError);
        Assert.Equal("cli unreachable", vm.ErrorMessage);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void NoResultsState_WhenSearchMatchesNothing()
    {
        var vm = new ImagesViewModel { SearchText = "zzz-no-such-image" };

        Assert.True(vm.HasNoResults);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void LoadingState_WhileListInFlight()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<ImageSummary>>();
        var provider = Substitute.For<IContainerCliProvider>();
        provider.ListImagesAsync(Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var vm = new ImagesViewModel(provider);

        Assert.True(vm.IsLoading);
        Assert.False(vm.HasItems);
        Assert.False(vm.IsEmpty);

        tcs.SetResult([]);

        Assert.False(vm.IsLoading);
        Assert.True(vm.IsEmpty);
    }
}
