using System;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.App.Tests.Services;

public class AutoRefreshServiceTests
{
    [Fact]
    public void Subscribe_ReturnsDisposable()
    {
        using AutoRefreshService service = new(NullLogger<AutoRefreshService>.Instance);

        IDisposable sub = service.Subscribe(_ => Task.CompletedTask);

        Assert.NotNull(sub);
        sub.Dispose();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        using AutoRefreshService service = new(NullLogger<AutoRefreshService>.Instance);

        IDisposable sub = service.Subscribe(_ => Task.CompletedTask);

        sub.Dispose();
        sub.Dispose();
    }

    [Fact]
    public void Interval_SetterIsIdempotent_WhenValueUnchanged()
    {
        using AutoRefreshService service = new(NullLogger<AutoRefreshService>.Instance);

        TimeSpan original = service.Interval;
        service.Interval = original;

        Assert.Equal(original, service.Interval);
    }

    [Fact]
    public void Interval_ChangesValue()
    {
        using AutoRefreshService service = new(NullLogger<AutoRefreshService>.Instance);

        service.Interval = TimeSpan.FromSeconds(10);

        Assert.Equal(TimeSpan.FromSeconds(10), service.Interval);
    }

    [Fact]
    public void Dispose_ServiceAfterMultipleSubscribers_DoesNotThrow()
    {
        AutoRefreshService service = new(NullLogger<AutoRefreshService>.Instance);

        IDisposable sub1 = service.Subscribe(_ => Task.CompletedTask);
        IDisposable sub2 = service.Subscribe(_ => Task.CompletedTask);

        service.Dispose();

        sub1.Dispose();
        sub2.Dispose();
    }

    [Fact]
    public void Unsubscribe_OneOfTwo_LeavesServiceRunnable()
    {
        using AutoRefreshService service = new(NullLogger<AutoRefreshService>.Instance);

        IDisposable sub1 = service.Subscribe(_ => Task.CompletedTask);
        IDisposable sub2 = service.Subscribe(_ => Task.CompletedTask);

        sub1.Dispose();

        IDisposable sub3 = service.Subscribe(_ => Task.CompletedTask);
        sub2.Dispose();
        sub3.Dispose();
    }
}
