namespace Knarr.App.Services;

/// <summary>
/// Drives app-wide periodic refresh. One shared ticker fires registered callbacks on the UI thread;
/// setting <see cref="Interval"/> restarts the ticker so all subscribers immediately adopt the new cadence.
/// </summary>
public interface IAutoRefreshService
{
    TimeSpan Interval { get; set; }

    IDisposable Subscribe(Func<CancellationToken, Task> onTick);
}
