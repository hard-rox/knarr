using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace Knarr.App.Services;

public sealed class AutoRefreshService : IAutoRefreshService, IDisposable
{
    private static readonly TimeSpan _defaultInterval = TimeSpan.FromSeconds(5);

    private readonly ILogger<AutoRefreshService> _logger;
    private readonly List<Subscription> _subscriptions = [];
    private DispatcherTimer? _timer;
    private TimeSpan _interval = _defaultInterval;
    private bool _tickInFlight;

    public AutoRefreshService(ILogger<AutoRefreshService> logger)
    {
        _logger = logger;
    }

    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (_interval == value)
            {
                return;
            }

            _interval = value;

            if (_timer is null) return;
            _timer.Stop();
            _timer.Interval = _interval;
            _timer.Start();
            _logger.LogDebug("Auto-refresh interval changed to {Interval}s", _interval.TotalSeconds);
        }
    }

    public IDisposable Subscribe(Func<CancellationToken, Task> onTick)
    {
        Console.WriteLine("Subscribing");
        Subscription sub = new(this, onTick);
        _subscriptions.Add(sub);

        if (_timer is not null) return sub;
        _timer = new DispatcherTimer { Interval = _interval };
        _timer.Tick += OnTick;
        _timer.Start();
        _logger.LogDebug("Auto-refresh timer started ({Interval}s)", _interval.TotalSeconds);

        return sub;
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_tickInFlight)
        {
            return;
        }

        _tickInFlight = true;
        try
        {
            Subscription[] snapshot = [.. _subscriptions];
            foreach (Subscription sub in snapshot)
            {
                try
                {
                    await sub.Invoke(CancellationToken.None).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto-refresh subscriber threw an exception");
                }
            }
        }
        finally
        {
            _tickInFlight = false;
        }
    }

    private void Unsubscribe(Subscription sub)
    {
        Console.WriteLine("Unsubscribing");
        _subscriptions.Remove(sub);

        if (_subscriptions.Count != 0 || _timer is null) return;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;
        _logger.LogDebug("Auto-refresh timer stopped (no subscribers)");
    }

    public void Dispose()
    {
        _subscriptions.Clear();

        if (_timer is null) return;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly AutoRefreshService _owner;
        private readonly Func<CancellationToken, Task> _onTick;
        private bool _disposed;

        internal Subscription(AutoRefreshService owner, Func<CancellationToken, Task> onTick)
        {
            _owner = owner;
            _onTick = onTick;
        }

        internal Task Invoke(CancellationToken cancellationToken) => _onTick(cancellationToken);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.Unsubscribe(this);
        }
    }
}
