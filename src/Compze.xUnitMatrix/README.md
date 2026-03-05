# Compze.xUnitMatrix

**Matrix testing for xUnit v3** — run tests across all combinations of pluggable components via a configuration file and custom theory attributes.

## What's in this package?

A framework for running tests against every combination of pluggable components (persistence layers, DI containers, serializers, transports, etc.) defined in a configuration file.

- **Configuration-driven** — Define component combinations in a text file (`Component1:Component2:...`), one per line
- **Wildcard expansion** — Use `*` to expand all values of an enum component
- **Skip support** — Skip specific component values with reasons
- **Generic attributes** — `ComponentCombinationsTheoryAttribute<T1>` through `<T1..T5>` for 1–5 component dimensions
- **Context flow** — `ComponentCombination.Current` gives test code access to the active combination via `AsyncLocal`

### Quick start

1. Define your component enums:
```csharp
public enum PersistenceLayer { SqliteMemory, MicrosoftSql, PostgreSql, MySql }
public enum DIContainer { Microsoft, SimpleInjector }
```

2. Create a configuration file (e.g., `TestUsingComponents`):
```
SqliteMemory:Microsoft
SqliteMemory:SimpleInjector
*:Microsoft
```

3. Create an attribute:
```csharp
using Compze.xUnitMatrix;

public class MyMatrixAttribute(string? sourceFilePath = null, int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<PersistenceLayer, DIContainer>(
      configurationFileName: "TestUsingComponents",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
```

4. Use it in tests:
```csharp
public class When_saving_data
{
   [MyMatrix] public void Data_round_trips()
   {
      var combination = ComponentCombination.Current;
      // ... test with current combination
   }
}
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.xUnitBDD](https://www.nuget.org/packages/Compze.xUnitBDD) | BDD-style specification testing |
| [Compze.xUnit](https://www.nuget.org/packages/Compze.xUnit) | Shared xUnit extensibility foundation |

## License

Apache-2.0
