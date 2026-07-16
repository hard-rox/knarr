---
name: add-feature
description: "Scaffold a new Knarr UI feature. Use when: adding a feature, creating a new page, adding a new view, new screen, new section. Creates the feature folder under Features/, the AXAML view, code-behind, ViewModel, registers it in ViewLocator, and adds a test file."
argument-hint: "Feature name in PascalCase, e.g. Dashboard"
---

# Add Feature

Scaffolds all files for a new Knarr UI feature following the feature-based directory structure.

## When to Use

- User asks to "add a new feature called X"
- User asks to "create a new page / view / screen / section"
- Any prompt that implies a new navigable or standalone UI component

## Inputs

- **FeatureName** — PascalCase name provided by the user (e.g. `Dashboard`, `ContainerDetail`, `Settings`)

## Procedure

### 1. Create the feature directory

Create `src/Knarr.App/Features/{FeatureName}/`.

### 2. Create the AXAML view

Create `src/Knarr.App/Features/{FeatureName}/{FeatureName}View.axaml` using [the view template](./assets/FeatureView.axaml).

Replace every occurrence of `FEATURE_NAME` with the actual `{FeatureName}`.

### 3. Create the code-behind

Create `src/Knarr.App/Features/{FeatureName}/{FeatureName}View.axaml.cs` using [the code-behind template](./assets/FeatureView.axaml.cs).

Replace every occurrence of `FEATURE_NAME` with the actual `{FeatureName}`.

### 4. Create the ViewModel

Create `src/Knarr.App/Features/{FeatureName}/{FeatureName}ViewModel.cs` using [the view model template](./assets/FeatureViewModel.cs).

Replace every occurrence of `FEATURE_NAME` with the actual `{FeatureName}`.

### 5. ViewLocator (no action required)

`src/Knarr.App/ViewLocator.cs` resolves views by reflection convention: it replaces
`ViewModel` with `View` in the view model's full type name and instantiates the result.
Because `{FeatureName}View` lives in the same namespace as `{FeatureName}ViewModel`, it is
resolved automatically — **no manual registration is needed**.

### 6. Create the test file

Create `tests/Knarr.App.Tests/Features/{FeatureName}/{FeatureName}ViewModelTests.cs` with a basic
test class:

```csharp
using Knarr.App.Features.{FeatureName};
using Xunit;

namespace Knarr.App.Tests.Features.{FeatureName};

public class {FeatureName}ViewModelTests
{
    [Fact]
    public void DefaultState_IsValid()
    {
        var vm = new {FeatureName}ViewModel();

        Assert.NotNull(vm);
    }
}
```

## Rules

- Do NOT add a `NavigationItem` to `SidebarViewModel` unless the user explicitly asks.
- Follow `.editorconfig`: file-scoped namespaces, 4-space C# indent, 2-space AXAML/XML.
- Use compiled bindings (`x:DataType` on the view root). Never use `{ReflectionBinding}` unless it is the only option.
- Keep code-behind to `InitializeComponent()` only. No logic in code-behind.
- ViewModel must extend `ViewModelBase` from `Knarr.App.Common`.
- Use `[ObservableProperty]` and `[RelayCommand]` from `CommunityToolkit.Mvvm` for state and commands.
