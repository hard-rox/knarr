# Knarr

Knarr is a cross-platform desktop application that provides a unified, native GUI over first-party
OS containerization CLIs:

- **macOS** — `container` (Apple Container)
- **Windows** — `wslc` (Windows Subsystem for Linux Container CLI)

Knarr is **not** a new runtime. It is a thin, auditable GUI orchestration layer that detects the host
OS and delegates 1:1 to the correct vendor CLI, surfacing the exact command executed for every action.

## Current capabilities

- Browse containers and run lifecycle actions.
- Configure and run a container with environment variables, published ports, volumes, and resource limits.
- View container logs and live statistics.
- Browse images, inspect them, and pull images from a registry.

Networks, volumes, registries, and application preferences are planned but are not yet implemented.

## Name

A knarr was a Norse cargo vessel used to carry goods across long distances. The name reflects Knarr's
role as a practical vessel for carrying container workloads across the first-party container platforms
provided by macOS and Windows.

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
    Features/                  One folder per UI feature
      Shell/                   Main window (view + view model)
      Sidebar/                 Navigation sidebar (view + view model)
    Common/                    Shared base types (ViewModelBase)
    Models/                    Domain/data types
    Services/                  Platform service interfaces + implementations
    Converters/                Avalonia value converters
    Themes/                    Resource dictionaries
    Assets/                    Bundled resources
tests/
  Knarr.App.Tests/
    Features/                  Tests mirroring src/Features layout
```

## Build & run

```sh
dotnet build
dotnet run --project src/Knarr.App
```

## Releases

Release packaging and signing are not configured yet. When downloadable releases are published, they
will be available from the [GitHub releases page](https://github.com/hard-rox/knarr/releases). The
maintainer process and release prerequisites are documented in [docs/RELEASING.md](docs/RELEASING.md).

## Code signing policy

Knarr intends to use a SignPath Foundation certificate for future Windows releases. Until the signing
process is configured and approved, no Knarr release is represented as SignPath-signed.

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by
[SignPath Foundation](https://signpath.org/).

- **Committers and reviewers:** the contributors to the
  [Knarr repository](https://github.com/hard-rox/knarr/graphs/contributors). Changes proposed by
  non-committers must be reviewed by a maintainer before merge.
- **Approver:** the [repository owner](https://github.com/hard-rox) approves each code-signing request
  until a public maintainer team is established.
- **Access controls:** maintainers with source-control or signing authority must use multi-factor
  authentication.
- **Artifact provenance:** only binaries built from this repository's source and approved release
  process may be submitted for signing.
- **Privacy:** Knarr does not transfer information to networked systems unless explicitly requested by
  its operator.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

Licensed under the [MIT License](LICENSE).
