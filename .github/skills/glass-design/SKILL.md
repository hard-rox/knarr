---
name: glass-design
description: "Design Knarr UI components using the liquid-glass design system. Use when: styling a view, building a data table, adding buttons/panels, choosing colors/brushes, working with Glass.axaml, or ensuring a feature matches Knarr's glassmorphism look. Covers design tokens, the GlassTableTheme, icon buttons, and glass panels."
argument-hint: "Optional: the feature/component being styled, e.g. Images table"
---

# Glass Design

Knarr's UI is a **liquid-glass (glassmorphism)** shell: frosted, translucent surfaces layered over a
soft acrylic gradient backdrop. Use this skill whenever you build or restyle a view so it stays
visually consistent with the rest of the app.

## Philosophy

- **Translucent surfaces, not solid panels.** Content sits on frosted glass (`GlassFillBrush`) with a
  faint light stroke (`StrokeBrush`) and a soft shadow. Depth comes from blur + shadow, not hard borders.
- **The backdrop shows through.** Never paint opaque backgrounds over the gradient. Prefer the semi-
  transparent brush tokens so the acrylic backdrop remains visible.
- **Quiet chrome, legible content.** Controls (buttons, headers) are low-contrast and recede;
  text and data are the focus. Use `TextBrush` for primary text, `TextDimBrush` for secondary.
- **Theme-variant aware.** Every surface must look right in both Light and Dark. Always bind to the
  theme tokens (via `DynamicResource`) — never hard-code colors in a view.
- **Consistency via shared resources.** Reuse the tokens, `Border.glass-panel`, `Button.icon`, and
  `GlassTableTheme` below instead of inventing per-view styling.

## Design tokens (`src/Knarr.App/Themes/Glass.axaml`)

All tokens are theme-variant aware and resolved with `{DynamicResource ...}`.

| Token | Purpose |
| --- | --- |
| `AccentColor` / `AccentBrush` | Brand accent, reserved for primary emphasis. |
| `TextBrush` | Primary text color. |
| `TextDimBrush` | Secondary / muted text, icons. |
| `BackgroundBaseBrush` | The app's acrylic gradient backdrop (set on the shell window). |
| `GlassFillBrush` | Standard frosted card/panel fill. |
| `GlassStrongBrush` | A more opaque frosted surface (e.g. sidebar). |
| `SidebarBrush` | Sidebar surface tint. |
| `StripBrush` | Subtle tinted strip (table header row, pill background). |
| `StrokeBrush` | Hairline light border on glass surfaces. |
| `HoverBrush` | Hover fill for interactive controls. |

**Rules**
- Bind to these tokens with `{DynamicResource Key}` so surfaces swap correctly on theme change.
- Do NOT add new one-off color brushes in a view. If a genuinely new semantic color is needed, add
  it to BOTH the `Light` and `Dark` dictionaries in `Glass.axaml`.
- Do NOT reintroduce unused tokens "just in case" — the palette is intentionally lean.

## Reusable building blocks

### Glass panel — `Border.glass-panel` (App.axaml)

The standard card surface: `GlassFillBrush` fill, `StrokeBrush` hairline, soft shadow,
`CornerRadius=14`. Use it for toolbars, cards, and table containers.

```xml
<Border Classes="glass-panel" Padding="14,10">
  <!-- content -->
</Border>

<!-- Override CornerRadius when needed (e.g. a larger table container) -->
<Border Classes="glass-panel" CornerRadius="16" ClipToBounds="True">
  <!-- table -->
</Border>
```

Do not manually re-set `Background`/`BorderBrush`/`BorderThickness`/`BoxShadow` — that's what the
class provides.

### Icon button — `Button.icon` (App.axaml)

Minimal transparent button for row actions and bulk actions. Transparent chrome, `HoverBrush` on
hover, 14x14 icon. Pair with a `PathIcon` whose `Data` comes from `Themes/Icons.axaml`.

```xml
<Button Classes="icon" ToolTip.Tip="Logs" Command="{Binding LogsCommand}">
  <PathIcon Data="{StaticResource TextBulletListRegular}" />
</Button>
```

### Data table — `GlassTableTheme` (Glass.axaml)

Every feature that lists data (Containers, Images, Volumes, …) should use the shared
`GlassTableTheme`. It renders a fixed-height (35px) tinted header strip (`StripBrush`), hides the
vertical header separators, aligns cell content with headers, and disables the row-selection
highlight (selection is driven by a per-row checkbox, not by clicking the row).

```xml
<Border Classes="glass-panel" CornerRadius="16" ClipToBounds="True">
  <TableView ItemsSource="{Binding Items}"
             Theme="{StaticResource GlassTableTheme}"
             Background="Transparent"
             BorderThickness="0"
             CanUserResizeColumns="False">
    <TableView.Columns>
      <!-- Selection checkbox column (fixed width, centered) -->
      <TableViewColumn Width="48" HorizontalContentAlignment="Center">
        <TableViewColumn.Header>
          <CheckBox IsThreeState="True"
                    IsChecked="{Binding $parent[TableView].((vm:MyViewModel)DataContext).AllSelected, Mode=TwoWay}" />
        </TableViewColumn.Header>
        <TableViewColumn.CellTemplate>
          <DataTemplate x:DataType="models:MyItem">
            <CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}" />
          </DataTemplate>
        </TableViewColumn.CellTemplate>
      </TableViewColumn>

      <!-- Content columns: Width="Auto" so they size to content -->
      <TableViewColumn Header="Name" Width="Auto"> ... </TableViewColumn>

      <!-- Actions column: FIXED width so it never shrinks; left-aligned Button.icon set -->
      <TableViewColumn Header="Actions" Width="120" HorizontalContentAlignment="Left"> ... </TableViewColumn>
    </TableView.Columns>
  </TableView>
</Border>
```

Table conventions:
- Wrap the table in a `Border Classes="glass-panel"` (with `ClipToBounds="True"` so rows respect the
  rounded corners), and put a matching toolbar in a sibling `glass-panel` above it.
- Use `Width="Auto"` for content columns and a **fixed** width for the actions column.
- Use `SelectableTextBlock` for values users may want to copy (names, IDs, images) — do not add copy
  buttons.
- Reference the parent VM command from a cell with
  `{Binding $parent[TableView].((vm:MyViewModel)DataContext).SomeCommand}` and
  `CommandParameter="{Binding}"`.

## Do / Don't

**Do**
- Reuse `glass-panel`, `Button.icon`, and `GlassTableTheme`.
- Bind every color to a `Glass.axaml` token via `DynamicResource`.
- Verify both Light and Dark variants look correct.
- Keep the ViewModel UI-framework-agnostic; put styling in AXAML.

**Don't**
- Hard-code hex colors or opaque backgrounds in a view.
- Re-declare the table theme, icon button, or panel styling per feature.
- Use WPF-isms (Triggers, `Visibility`, `DependencyProperty`). Use Avalonia selectors, `IsVisible`,
  and `StyledProperty`.
- Add unused tokens to `Glass.axaml`.

## Reference files

- Tokens + `GlassTableTheme`: `src/Knarr.App/Themes/Glass.axaml`
- Global styles (`glass-panel`, `Button.icon`, row-selection, button shadows): `src/Knarr.App/App.axaml`
- Icon geometries: `src/Knarr.App/Themes/Icons.axaml`
- Status pill control: `src/Knarr.App/Controls/Pill.axaml`
- Worked example: `src/Knarr.App/Features/Containers/ContainersView.axaml`
- Full guideline: `docs/DESIGN.md`
