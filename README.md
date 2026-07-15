# Knarr

Knarr is a cross-platform desktop application that provides a unified, native GUI over first-party
OS containerization CLIs:

- **macOS** — `container` (Apple Container)
- **Windows** — `wslc` (Windows Subsystem for Linux Container CLI)

Knarr is **not** a new runtime. It is a thin, auditable GUI orchestration layer that detects the host
OS and delegates 1:1 to the correct vendor CLI, surfacing the exact command executed for every action.

## Tech stack

- .NET
- [Avalonia UI](https://avaloniaui.net/) (AXAML) with compiled bindings
- MVVM via [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- Central Package Management (`Directory.Packages.props`)

## Project structure

```
Knarr.slnx                     Solution
Directory.Packages.props       Central NuGet package versions
.editorconfig                  Code style
docs/                          PRD and design mockups
src/
  Knarr.App/                   Application project
    Views/                     AXAML views
    ViewModels/                State, commands, orchestration
    Models/                    Domain/data types
    Assets/                    Bundled resources
```

## Build & run

```sh
dotnet build
dotnet run --project src/Knarr.App
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

Licensed under the [MIT License](LICENSE).
