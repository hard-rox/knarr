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

## Pull requests

- Keep changes focused and describe the intent.
- Ensure the solution builds and existing behavior is preserved.
