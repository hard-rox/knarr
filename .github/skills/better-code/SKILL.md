---
name: better-code
description: "Commenting and documentation discipline for Knarr C#/AXAML code. Use whenever writing or editing code in this repo: adding comments, XML doc comments, naming classes/fields/methods, or reviewing/cleaning up existing comments."
argument-hint: ""
---

# Better Code — Comments & Docs Discipline

Rules for comments and XML documentation when writing or editing C#/AXAML code in Knarr.

## Rules

- Do NOT add `///` XML doc comments or `//` comments on private or internal classes, fields,
  properties, or methods.
- Only add `///` XML doc comments on:
  - public interfaces (e.g. `IContainerCliProvider`, `IContainerSystemService`)
  - shared public abstract base classes (e.g. `ContainerCliProviderBase`, `ViewModelBase`)
- Do not add XML docs to ordinary public classes (views, view models, records, models) unless they are
  a shared abstraction meant to be extended/implemented by other types.
- Names for classes, fields, methods, and properties MUST be self-explanatory. Prefer renaming a symbol
  to make its purpose obvious over adding a comment to explain it.
- Remove comments that just restate what the code already says (e.g. section headers like
  `// Lifecycle commands`, or a comment repeating a method name).
- **Exception:** if there is truly no self-explanatory name available — e.g. a non-obvious business
  rule, a CLI/OS quirk, a race condition, or an initialization-order constraint — add a single concise,
  one-line comment explaining *why*, not *what*. Known examples of this exception in this codebase:
  - `ContainerCliProviderBase.cs` — graceful-stop-then-forceful-kill fallback timing
  - `AppleContainerCli/AppleContainerCliProvider.cs` — Apple CLI uses id as both id and name; colon
    disambiguation between a tag and a registry port
  - `AppleContainerCli/AppleContainerSystemService.cs` — unregistered system exit code vs. payload
    authority
  - `Features/ContainerLogs/ContainerLogsDialogViewModel.cs` — background-thread cancellation
    requirement; disposed-source race in a catch block
  - `Program.cs` — Velopack/Avalonia startup ordering constraint
  - `Controls/TerminalOutputView.axaml.cs` — deferring for `ScrollViewer` measure timing

## When Reviewing Existing Code

- Delete `///` docs found on private/internal members rather than relocating them, unless the doc
  content reveals a naming problem — in that case, rename the symbol instead.
- Delete vague or obvious inline comments; keep only the "why" comments described above.
- If a comment could be replaced by a better name, rename the symbol (checking usages first) and
  remove the comment.
