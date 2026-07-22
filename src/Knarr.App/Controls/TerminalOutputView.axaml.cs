using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Knarr.App.Controls;

/// <summary>
/// Lifecycle state of a <see cref="TerminalOutputView"/>. Drives the panel's status accent via
/// pseudo-classes and lets host view-models describe the outcome of the streamed command.
/// </summary>
public enum TerminalState
{
    Idle,
    Running,
    Success,
    Error,
    Canceled
}

/// <summary>
/// A reusable, feature-neutral terminal-style panel that renders a stream of CLI output lines
/// (command / stdout / stderr / exit) with monospaced text and auto-scroll. Designed to be shared
/// by any feature that surfaces live command output (image pull today; container logs/exec/build
/// later). The exact command line appears first because it is the first line in the bound
/// collection. Auto-scrolls to the newest line while output is arriving.
/// </summary>
public class TerminalOutputView : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> LinesProperty =
        AvaloniaProperty.Register<TerminalOutputView, IEnumerable?>(nameof(Lines));

    public static readonly StyledProperty<TerminalState> StateProperty =
        AvaloniaProperty.Register<TerminalOutputView, TerminalState>(nameof(State));

    public static readonly StyledProperty<bool> IsTruncatedProperty =
        AvaloniaProperty.Register<TerminalOutputView, bool>(nameof(IsTruncated));

    public static readonly StyledProperty<string?> TruncationNoteProperty =
        AvaloniaProperty.Register<TerminalOutputView, string?>(nameof(TruncationNote));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<TerminalOutputView, string?>(nameof(Placeholder));

    public static readonly StyledProperty<bool> HasOutputProperty =
        AvaloniaProperty.Register<TerminalOutputView, bool>(nameof(HasOutput));

    private ScrollViewer? _scrollViewer;
    private INotifyCollectionChanged? _observedCollection;

    public IEnumerable? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    public TerminalState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool IsTruncated
    {
        get => GetValue(IsTruncatedProperty);
        set => SetValue(IsTruncatedProperty, value);
    }

    public string? TruncationNote
    {
        get => GetValue(TruncationNoteProperty);
        set => SetValue(TruncationNoteProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>True when the bound <see cref="Lines"/> collection currently contains any entries.</summary>
    public bool HasOutput
    {
        get => GetValue(HasOutputProperty);
        private set => SetValue(HasOutputProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StateProperty)
        {
            UpdatePseudoClasses();
        }
        else if (change.Property == LinesProperty)
        {
            HookCollection(change.GetNewValue<IEnumerable?>());
            ScrollToEnd();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        UpdatePseudoClasses();
        HookCollection(Lines);
        ScrollToEnd();
    }

    private void HookCollection(IEnumerable? lines)
    {
        if (_observedCollection is not null)
        {
            _observedCollection.CollectionChanged -= OnLinesCollectionChanged;
            _observedCollection = null;
        }

        if (lines is INotifyCollectionChanged incc)
        {
            _observedCollection = incc;
            incc.CollectionChanged += OnLinesCollectionChanged;
        }

        UpdateHasOutput();
    }

    private void UpdateHasOutput()
    {
        var lines = Lines;
        if (lines is null)
        {
            HasOutput = false;
            return;
        }

        IEnumerator enumerator = lines.GetEnumerator();
        try
        {
            HasOutput = enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateHasOutput();
        ScrollToEnd();
    }

    private void ScrollToEnd()
    {
        if (_scrollViewer is null)
        {
            return;
        }

        // Defer so the ScrollViewer measures the newly added content before we scroll.
        Dispatcher.UIThread.Post(() => _scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":running", State == TerminalState.Running);
        PseudoClasses.Set(":success", State == TerminalState.Success);
        PseudoClasses.Set(":error", State == TerminalState.Error);
        PseudoClasses.Set(":canceled", State == TerminalState.Canceled);
    }
}
