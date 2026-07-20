# Plan: Strongly-typed record-based CLI JSON parsing

## Goal
Replace string-based JsonLinq parsing in Knarr.Service with strongly-typed System.Text.Json
deserialization. Collapse ImageSummary+App.ImageItem and ContainerSummary+App.ContainerItem into
single shared records (Knarr.Service.Models) used directly by the ViewModels, with computed arrow
(=>) display properties. Cover BOTH images and containers, BOTH platforms.

## Decisions (from user)
- Scope: images AND containers.
- Collapse into ONE record per entity, used by ViewModel directly. Records live in
  Knarr.Service.Models (provider must return them; Service can't reference App).
  - Records need `IsSelected` (checkbox two-way + bulk select) => partial record : ObservableObject
    (CommunityToolkit.Mvvm). Add CommunityToolkit.Mvvm PackageReference to Knarr.Service.
    (CommunityToolkit.Mvvm is UI-framework-agnostic, not Avalonia — respects layering rule.)
- Separate per-platform internal DTO records for deserialization -> map to shared record.
- Humanized "X ago" computed in record from Unix epoch (wslc Created = epoch seconds).
- Use System.Text.Json (reflection-based, PropertyNameCaseInsensitive=true).
- RelativeTime.Humanize handles FUTURE timestamps gracefully (e.g. "just now") — acceptable.

## Layering wrinkle
Current App ContainerItem.PillStatus returns Controls.PillStatus (App-layer enum). Cannot move to
Service. Solution: record keeps `Status` (ContainerStatus); add App Converter
`ContainerStatusToPillStatusConverter` and bind Pill Status through it in ContainersView.

## Phase 0 — Gather real JSON
RESOLVED shapes:
- wslc images: {Created epoch, Id "sha256:..", Repository, Size long bytes, Tag}
- wslc list: {CreatedAt epoch, Id 64-char (no sha256:), Image, Name, Ports[] {BindingAddress,
  ContainerPort, HostPort, Protocol int(6=TCP,17=UDP)}, State int(2=Running observed),
  StateChangedAt epoch}. NO Cpu/Memory.
macOS `container *`: KEEP AS-IS (JsonLinq navigation) for now — only adapt its output to new shared
  records. Real JSON to be gathered later.
Full feature command surface (reference):
- wslc: start/stop/remove[--force] <id>; pull <ref>; rmi[--force] <ref>
- container: start/stop/delete[--force] <id>; image pull <ref>; image delete[--force] <ref>

## Phase 1 — Shared records (Knarr.Service.Models)
- Create `ImageItem` (partial record : ObservableObject):
  - [ObservableProperty] bool isSelected
  - init: Repository, Tag, Digest (full id incl sha256:), SizeBytes (long), CreatedAt (DateTimeOffset?)
  - arrow: RepoTag => "repo:tag"; ShortId => strip "sha256:"/algo prefix, first 12;
    Size => SizeFormatter.Humanize(SizeBytes) (MB/GB); Created => RelativeTime.Humanize(CreatedAt)
- Create `ContainerItem` (partial record : ObservableObject):
  - [ObservableProperty] bool isSelected
  - init: Name, Id, Image, Status (ContainerStatus), Ports, Cpu, Memory, Uptime (as available)
  - arrow: ShortId, StatusText, ResourceUsage, IsRunning  (NO PillStatus)
- Add helpers in Service.Models: `SizeFormatter.Humanize(long)` (move logic from
  CliProviderBase.FormatSize), `RelativeTime.Humanize(DateTimeOffset?, DateTimeOffset? now=null)`
  (now param for deterministic tests).
- Delete ImageSummary.cs, ContainerSummary.cs. Keep ContainerStatus.cs.

## Phase 2 — Per-platform parsing
Windows (wslc) => System.Text.Json typed DTOs (drop JsonLinq from Wslc provider):
- WslcImageJson: {long Created, string Id, string Repository, long Size, string Tag}
- WslcContainerJson: {long CreatedAt, string Id, string Image, string Name,
  WslcPortJson[] Ports, int State, long StateChangedAt}; WslcPortJson {string BindingAddress,
  int ContainerPort, int HostPort, int Protocol}
- Map: Id -> full digest (containers have no sha256: prefix; images do -> strip in ShortId).
  Ports[] -> formatted string "addr:host->container/proto" joined by ", " (Protocol 6=tcp,17=udp);
  empty -> em dash. State int -> ContainerStatus (2=Running observed; unknown=Exited — REFINE when
  other states seen). CreatedAt/Created epoch -> CreatedAt DateTimeOffset. Uptime = for Running,
  RelativeTime.Humanize(StateChangedAt); else em dash. No Cpu/Memory (em dash).
macOS (container) => KEEP JsonLinq navigation as-is, but change mapping output to new shared records
  (ImageItem/ContainerItem) instead of ImageSummary/ContainerSummary.
- JsonLinq PackageReference STAYS (Apple provider still uses it). Only Wslc provider stops using it.
- Keep internal static ParseImages/ParseContainers on both providers for tests.

## Phase 3 — Wire-up
- IContainerCliProvider: return IReadOnlyList<ImageItem>/<ContainerItem>.
- DesignTimeContainerCliProvider: rebuild sample data with new records.
- ImagesViewModel/ContainersViewModel: bind provider results directly into ObservableCollection
  (drop the summary->item mapping loops). ObservableCollection<ImageItem>/<ContainerItem> now use
  Service.Models types. Keep IsSelected/filter logic.
- Delete App Models: ImageItem.cs, ContainerItem.cs.
- Views: change xmlns:models to `using:Knarr.Service.Models`. Add
  ContainerStatusToPillStatusConverter (App/Converters) + register in App.axaml/Glass resources;
  bind Pill Status through it.
- GlobalUsings: App may `using Knarr.Service.Models;` where models referenced.

## Phase 4 — Tests
- Update CliProviderParsingTests: assert new records (ShortId strip sha256:, Size MB/GB, relative
  Created). Add cases for epoch->relative and MB/GB boundary via helpers with fixed `now`.
- Update Knarr.App.Tests Images/Containers tests referencing removed App models.

## Relevant files
- src/Knarr.Service/CliProviderBase.cs (FormatSize -> SizeFormatter; SplitReference stays/moves)
- src/Knarr.Service/WslcCliProvider.cs, AppleContainerCliProvider.cs (typed parsing)
- src/Knarr.Service/IContainerCliProvider.cs, DesignTimeContainerCliProvider.cs
- src/Knarr.Service/Models/ImageSummary.cs, ContainerSummary.cs (delete), + new ImageItem.cs,
  ContainerItem.cs, SizeFormatter.cs, RelativeTime.cs
- src/Knarr.App/Models/ImageItem.cs, ContainerItem.cs (delete)
- src/Knarr.App/Features/{Images,Containers}/*ViewModel.cs + *View.axaml
- src/Knarr.App/Converters/ (new ContainerStatusToPillStatusConverter)
- Directory.Packages.props, Knarr.Service.csproj (CommunityToolkit.Mvvm add; JsonLinq KEPT for Apple)
- tests/Knarr.Service.Tests/CliProviderParsingTests.cs; tests/Knarr.App.Tests/*

## Verification
- `dotnet build` clean; `dotnet test` green.
- Run Avalonia previewer / app: Images + Containers tables render via DesignTime provider.
- Manual: on Windows run app against real wslc; confirm rows match `wslc images`/`wslc list` output
  (repo:tag, 12-char id no sha:, MB/GB, "X ago").
