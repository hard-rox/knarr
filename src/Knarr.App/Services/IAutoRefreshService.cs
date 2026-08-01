namespace Knarr.App.Services;

/// <summary>
/// Drives app-wide periodic refresh. One shared ticker fires registered callbacks on the UI thread;
/// setting <see cref="Interval"/> restarts the ticker so all subscribers immediately adopt the new cadence.
/// </summary>
public interface IAutoRefreshService
{
    public TimeSpan Interval { get; set; }

    public IDisposable Subscribe(Func<CancellationToken, Task> onTick);
}
