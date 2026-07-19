# Knarr Design & Theming Guide

Knarr uses a **liquid-glass (glassmorphism)** design language: frosted, translucent surfaces layered
over a soft acrylic gradient backdrop. This document is the source of truth for building UI that
matches the app. Keep it in sync with `src/Knarr.App/Themes/Glass.axaml` and `src/Knarr.App/App.axaml`.

> For an agent-assisted workflow, see the `glass-design` skill in `.github/skills/glass-design/`.

## Principles

1. **Translucent, not solid.** Surfaces are frosted glass over the gradient backdrop. Depth comes
   from translucency + soft shadow + a hairline stroke — not from heavy borders or opaque fills.
2. **Let the backdrop show through.** Never paint an opaque background over the shell gradient.
3. **Quiet chrome, legible content.** Controls recede; data is the focus. Primary text uses
   `TextBrush`, secondary/muted text and icons use `TextDimBrush`.
4. **Theme-variant aware.** Everything must look correct in both Light and Dark. Always bind colors
   to tokens with `{DynamicResource ...}`; never hard-code hex in a view.
5. **Reuse shared resources.** Prefer the tokens, `Border.glass-panel`, `Button.icon`, and
   `GlassTableTheme` over per-view styling. Keep the palette lean — no unused tokens.

## Design tokens

Defined per theme variant in `src/Knarr.App/Themes/Glass.axaml` (`Light` and `Dark` dictionaries).

| Token | Purpose |
| --- | --- |
| `AccentColor` / `AccentBrush` | Brand accent, reserved for primary emphasis. |
| `TextBrush` | Primary text color. |
| `TextDimBrush` | Secondary / muted text, icons. |
| `BackgroundBaseBrush` | Acrylic gradient backdrop (shell window). |
| `GlassFillBrush` | Standard frosted card/panel fill. |
| `GlassStrongBrush` | More opaque frosted surface (e.g. sidebar). |
| `SidebarBrush` | Sidebar surface tint. |
| `StripBrush` | Subtle tinted strip (table header row, pill background). |
| `StrokeBrush` | Hairline light border on glass surfaces. |
| `HoverBrush` | Hover fill for interactive controls. |

If a genuinely new semantic color is required, add it to **both** the `Light` and `Dark`
dictionaries. Do not add tokens that aren't referenced.

## Reusable building blocks

### `Border.glass-panel`

The standard card surface (fill + stroke + soft shadow, `CornerRadius=14`). Defined in `App.axaml`.

```xml
<Border Classes="glass-panel" Padding="14,10"> ... </Border>
<Border Classes="glass-panel" CornerRadius="16" ClipToBounds="True"> ... </Border>
```

Don't re-set `Background`/`BorderBrush`/`BorderThickness`/`BoxShadow` — the class provides them.
Override `CornerRadius` or `Padding` as needed.

### `Button.icon`

Minimal transparent icon button (row/bulk actions). `HoverBrush` on hover, 14x14 `PathIcon`.

```xml
<Button Classes="icon" ToolTip.Tip="Logs" Command="{Binding LogsCommand}">
  <PathIcon Data="{StaticResource TextBulletListRegular}" />
</Button>
```

### `GlassTableTheme`

Shared `TableView` theme (defined in `Glass.axaml`). Renders a fixed-height (35px) tinted header
strip (`StripBrush`), hides vertical header separators, aligns cell content with headers, and
disables the row-selection highlight (selection is driven by a per-row checkbox).

```xml
<Border Classes="glass-panel" CornerRadius="16" ClipToBounds="True">
  <TableView ItemsSource="{Binding Items}"
             Theme="{StaticResource GlassTableTheme}"
             Background="Transparent"
             BorderThickness="0"
             CanUserResizeColumns="False">
    <TableView.Columns> ... </TableView.Columns>
  </TableView>
</Border>
```

Table conventions:
- Toolbar and table each live in their own sibling `glass-panel`.
- Content columns use `Width="Auto"`; the actions column uses a **fixed** width so it never shrinks.
- Use `SelectableTextBlock` for copyable values (names, IDs, images) — no dedicated copy buttons.
- Bind cell actions to the parent VM:
  `{Binding $parent[TableView].((vm:MyViewModel)DataContext).SomeCommand}` with
  `CommandParameter="{Binding}"`.

See `src/Knarr.App/Features/Containers/ContainersView.axaml` for a complete worked example.

## Do / Don't

**Do**
- Reuse `glass-panel`, `Button.icon`, and `GlassTableTheme`.
- Bind colors to `Glass.axaml` tokens via `DynamicResource`.
- Verify both Light and Dark variants.
- Keep ViewModels UI-framework-agnostic; put styling in AXAML.

**Don't**
- Hard-code hex colors or opaque backgrounds in a view.
- Re-declare the table theme / icon button / panel styling per feature.
- Use WPF-isms (Triggers, `Visibility`, `DependencyProperty`) — use Avalonia selectors, `IsVisible`,
  and `StyledProperty`/`DirectProperty`.
- Add unused tokens to `Glass.axaml`.

## Where things live

- Tokens + `GlassTableTheme`: `src/Knarr.App/Themes/Glass.axaml`
- Global styles (`glass-panel`, `Button.icon`, row-selection, button shadows): `src/Knarr.App/App.axaml`
- Icon geometries: `src/Knarr.App/Themes/Icons.axaml`
- Status pill: `src/Knarr.App/Controls/Pill.axaml`
- Design mockups: `docs/mockups/`
