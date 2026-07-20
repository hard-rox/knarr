using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Knarr.App.Controls;

/// <summary>
/// A compact stat card showing a caption and a large value (e.g. "Running" / "3"). Mirrors
/// <see cref="Pill"/>; the <see cref="Status"/> drives the value colour via pseudo-classes.
/// </summary>
public class Tile : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<Tile, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<Tile, string?>(nameof(Value));

    public static readonly StyledProperty<PillStatus> StatusProperty =
        AvaloniaProperty.Register<Tile, PillStatus>(nameof(Status));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PillStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StatusProperty)
        {
            UpdatePseudoClasses();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":running", Status == PillStatus.Running);
        PseudoClasses.Set(":stopped", Status == PillStatus.Stopped);
        PseudoClasses.Set(":paused", Status == PillStatus.Paused);
    }
}
