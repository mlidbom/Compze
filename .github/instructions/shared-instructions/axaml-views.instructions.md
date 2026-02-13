---
applyTo: "**/*.axaml,**/*.axaml.cs"
---

# Avalonia AXAML View Guidelines

**Scope:** All `.axaml` and `.axaml.cs` files  — Views, Controls, Dialogs, and their code-behind.

## Binding & DataContext

### Compiled Bindings
- Use `x:DataType="vm:XxxViewModel"` on the root element of Windows/Dialogs that have a ViewModel
- UserControls (`Controls/`) may omit `x:DataType` — they receive their DataContext from the parent

### DataContext Setup
- **Never** set `DataContext` in AXAML — always set it in the code-behind constructor:
  ```csharp
  DataContext = new MyViewModel(services);
  ```
- Use `Design.DataContext` separately for the AXAML previewer:
  ```xml
  <Design.DataContext>
      <vm:MyViewModel />
  </Design.DataContext>
  ```

### Designer Support
Views and ViewModels that require constructor parameters must have a parameterless constructor for the XAML designer:
```csharp
[Obsolete("For XAML designer/previever only")]
#pragma warning disable CS8618
public MyDialog() { InitializeComponent(); }
#pragma warning restore CS8618
```

## Commands vs Event Handlers
- Prefer **command bindings** (`{Binding MyCommand}`) for all user interactions
- Use **event handlers** only when commands are impractical (e.g., `DoubleTapped`, focus management)
- When a command needs `Window.Close()`, declare it as a settable `IRelayCommand`/`IAsyncRelayCommand` on the ViewModel and wire it in code-behind after construction

## Custom UserControls
- Parent sets `DataContext="{Binding SubViewModel}"` on the control element
- The control's internal bindings resolve against the sub-ViewModel without needing `x:DataType`

## Layout Patterns
- `DockPanel` for top-level layout
- `StackPanel`/`Grid` for sections
- `Border` with `BorderBrush="Gray"` for visual section grouping
- `LayoutTransformControl` with `ScaleTransform` for DPI scaling on complex dialogs
- `WindowStartupLocation="Manual"` with `WindowPositioner.RepositionNearCursor()` in code-behind

## Resources & Styling
- No external stylesheet files — all styling is inline
- Shared spacing/margin constants go in `Window.Resources` as `<x:Double>` and `<Thickness>`:
  ```xml
  <Window.Resources>
      <x:Double x:Key="SectionSpacing">8</x:Double>
      <Thickness x:Key="SectionPadding">8</Thickness>
  </Window.Resources>
  ```
- Reference via `{StaticResource SectionSpacing}`
