# Knarr — Copilot Instructions

Knarr is a cross-platform desktop app (.NET / Avalonia UI / MVVM) that provides a unified GUI over
first-party OS containerization CLIs: `container` (Apple Container) on macOS and `wslc` on Windows.
Knarr is a thin, auditable GUI orchestration layer — it never reimplements runtime logic; every GUI
action maps 1:1 onto an underlying CLI command.

## Tech stack
- .NET (see `Directory.Packages.props` / project `TargetFramework`)
- Avalonia UI (AXAML) with `AvaloniaUseCompiledBindingsByDefault=true`
- MVVM via `CommunityToolkit.Mvvm`
- Central Package Management: all package versions live in `Directory.Packages.props`

## Project layout
- `src/Knarr.App/` — the application project
  - `Features/{Name}/` — one folder per UI feature; each contains the `.axaml` view, `.axaml.cs` code-behind, and `ViewModel`
    - `Features/Shell/` — `MainWindow` (host window + view model)
    - `Features/Sidebar/` — `Sidebar` (nav sidebar + view model)
  - `Common/` — `ViewModelBase` and other shared base types
  - `Models/` — data/domain types
  - `Services/` — platform service interfaces and implementations
  - `Converters/` — Avalonia value converters
  - `Themes/` — resource dictionaries (Glass, Icons)
  - `Assets/` — bundled resources
- `docs/` — PRD and design mockups
- `Directory.Packages.props` — central NuGet versions (add new versions here, not in `.csproj`)
- `.editorconfig` — code style; follow it

## Avalonia build MCP (REQUIRED)
- At the start of any Avalonia work, call the Avalonia MCP `get_avalonia_expert_rules` tool and follow it.
- Use `search_avalonia_docs` / `lookup_avalonia_api` to verify APIs before using them — do not guess.
- Prefer the MCP over assumptions from WPF/UWP/WinUI. Avalonia is NOT WPF.

## Avalonia MVVM rules (must follow)
- AXAML files use the `.axaml` extension; root namespace is `xmlns="https://github.com/avaloniaui"`.
- ALWAYS set `x:DataType` on view roots; use compiled bindings. Use `{ReflectionBinding}` only as a last resort.
- Use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm.
- NEVER use ReactiveUI / Avalonia.ReactiveUI.
- NEVER use `DependencyProperty` — use `StyledProperty`, `DirectProperty`, or `AttachedProperty`.
- NO business logic in code-behind; keep it to `InitializeComponent()`. Bind commands in XAML.
- Styling uses CSS-like selectors and pseudo-classes (`:pointerover`, `:pressed`), NOT WPF Triggers/VisualStateManager.
- Layout: use `IsVisible` (not `Visibility`), star/`Auto` sizing (avoid fixed pixels), and `GridSplitter` for resizable panes.
- ViewModels must be UI-framework-agnostic and unit-testable; wire services via DI behind interfaces.

## Architecture layering
- Views → ViewModels → Services → Infrastructure. No reference from Services/Domain back to Avalonia/UI types.
- Platform differences hide behind an `IContainerCliProvider` abstraction; concrete provider chosen at runtime by OS
  (`AppleContainerCliProvider` on macOS, `WslcCliProvider` on Windows).
- Surface the exact CLI command executed for every action (transparency is a product requirement).

## Package management
- Add/upgrade packages by editing `Directory.Packages.props` (`<PackageVersion>`); reference them version-less in `.csproj`.

## Adding a new feature
- Use the `/add-feature` skill (`.github/skills/add-feature/SKILL.md`) to scaffold all required files.
- Every new feature view model needs one new line added to the `ViewLocator.cs` pattern-matching switch.
- Do NOT add a `NavigationItem` to `SidebarViewModel` unless explicitly requested.

## Conventions
- Respect `.editorconfig`: file-scoped namespaces, 4-space C# indent, 2-space AXAML/XML, `_camelCase` private fields, `I`-prefixed interfaces.
- Nullable reference types are enabled.
