# Contributing to Knarr

Thanks for your interest in contributing!

## Getting started

1. Install the .NET SDK (see `global.json` / project `TargetFramework`).
2. Restore and build:
   ```sh
   dotnet build
   ```
3. Run the app:
   ```sh
   dotnet run --project src/Knarr.App
   ```

## Guidelines

- Follow the rules in [.github/copilot-instructions.md](.github/copilot-instructions.md).
- Respect [.editorconfig](.editorconfig) for formatting.
- Add/upgrade NuGet packages via `Directory.Packages.props` (Central Package Management) — do not
  put versions in `.csproj` files.
- Avalonia MVVM: use `CommunityToolkit.Mvvm`, compiled bindings, and keep business logic out of
  code-behind. Never use ReactiveUI or WPF-style `DependencyProperty`/Triggers.
- Knarr must never reimplement runtime logic — every GUI action maps to a vendor CLI command.

## Design & theming

Knarr has a **liquid-glass** design system. Before adding or restyling UI, read
[docs/DESIGN.md](docs/DESIGN.md) and follow it:

- Bind colors to the theme tokens in `src/Knarr.App/Themes/Glass.axaml` via `DynamicResource`; never
  hard-code hex values in a view, and make sure surfaces look right in both Light and Dark.
- Reuse the shared building blocks instead of per-view styling: `Border.glass-panel` and
  `Button.icon` (in `src/Knarr.App/App.axaml`) and the `GlassTableTheme` for data tables.
- Keep the palette lean — don't add unused tokens.

The `glass-design` skill (`.github/skills/glass-design/`) can guide you through this.

## Pull requests

- Keep changes focused and describe the intent.
- Ensure the solution builds and existing behavior is preserved.
