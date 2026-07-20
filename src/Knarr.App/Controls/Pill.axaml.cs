using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Knarr.App.Controls;

/// <summary>
/// Visual status conveyed by a <see cref="Pill"/>. Drives the control's colour via pseudo-classes.
/// </summary>
public enum PillStatus
{
    Neutral,
    Running,
    Stopped,
    Paused
}

public class Pill : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<Pill, string?>(nameof(Text));

    public static readonly StyledProperty<PillStatus> StatusProperty =
        AvaloniaProperty.Register<Pill, PillStatus>(nameof(Status));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

