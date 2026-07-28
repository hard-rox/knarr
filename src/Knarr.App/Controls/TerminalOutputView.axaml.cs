using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Knarr.App.Converters;
using Knarr.Service.Models;

namespace Knarr.App.Controls;

/// <summary>
/// Lifecycle state of a <see cref="TerminalOutputView"/>. Drives the panel's status accent via
/// pseudo-classes and let's host view-models describe the outcome of the streamed command.
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
    private SelectableTextBlock? _outputText;
    private Button? _copyButton;
    private INotifyCollectionChanged? _observedCollection;
    private bool _isCollectionSubscribed;
    private bool _isAttachedToVisualTree;

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
            RebuildInlines();
            ScrollToEnd();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _copyButton?.Click -= OnCopyAllClick;

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _outputText = e.NameScope.Find<SelectableTextBlock>("PART_OutputText");
        _copyButton = e.NameScope.Find<Button>("PART_CopyButton");

        _copyButton?.Click += OnCopyAllClick;

        UpdatePseudoClasses();
        HookCollection(Lines);
        RebuildInlines();
        ScrollToEnd();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttachedToVisualTree = true;
        AttachCollectionObserver();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachCollectionObserver();
        _isAttachedToVisualTree = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void HookCollection(IEnumerable? lines)
    {
        if (ReferenceEquals(_observedCollection, lines))
        {
            UpdateHasOutput();
            return;
        }

        DetachCollectionObserver();
        _observedCollection = null;

        if (lines is INotifyCollectionChanged incc)
        {
            _observedCollection = incc;
            AttachCollectionObserver();
        }

        UpdateHasOutput();
    }

    private void AttachCollectionObserver()
    {
        INotifyCollectionChanged? observedCollection = _observedCollection;
        if (_isCollectionSubscribed || observedCollection is null || !_isAttachedToVisualTree)
        {
            return;
        }

        observedCollection.CollectionChanged += OnLinesCollectionChanged;
        _isCollectionSubscribed = true;
    }

    private void DetachCollectionObserver()
    {
        INotifyCollectionChanged? observedCollection = _observedCollection;
        if (!_isCollectionSubscribed || observedCollection is null)
        {
            return;
        }

        observedCollection.CollectionChanged -= OnLinesCollectionChanged;
        _isCollectionSubscribed = false;
    }

    private void UpdateHasOutput()
    {
        IEnumerable? lines = Lines;
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
        RebuildInlines();
        ScrollToEnd();
    }

    /// <summary>
    /// Rebuilds the selectable output text from the bound <see cref="Lines"/> collection, one
    /// colored <see cref="Run"/> per line so the text remains selectable/copyable while keeping
    /// the per-kind color coding. Rebuilt in full on every change for simplicity; output volumes
    /// in this app are small enough that this is not a performance concern.
    /// </summary>
    private void RebuildInlines()
    {
        if (_outputText is null)
        {
            return;
        }

        _outputText.Inlines ??= new InlineCollection();
        _outputText.Inlines.Clear();

        if (Lines is not IEnumerable lines)
        {
            return;
        }

        List<CliOutputLine> outputLines = [];
        foreach (object? item in lines)
        {
            if (item is CliOutputLine line)
            {
                outputLines.Add(line);
            }
        }

        for (int i = 0; i < outputLines.Count; i++)
        {
            CliOutputLine line = outputLines[i];
            IBrush? brush = CliOutputKindToBrushConverter.Instance.Convert(
                line.Kind, typeof(IBrush), null, CultureInfo.InvariantCulture) as IBrush;

            _outputText.Inlines.Add(new Run(line.Text) { Foreground = brush });

            if (i < outputLines.Count - 1)
            {
                _outputText.Inlines.Add(new LineBreak());
            }
        }
    }

    private async void OnCopyAllClick(object? sender, RoutedEventArgs e)
    {
        if (Lines is not IEnumerable lines || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        StringBuilder builder = new();
        foreach (object? item in lines)
        {
            if (item is not CliOutputLine line)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(line.Text);
        }

        if (builder.Length > 0)
        {
            await clipboard.SetTextAsync(builder.ToString());
        }
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
