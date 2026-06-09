# Compze.xUnitMatrix

**Run every test against every combination of your pluggable components. Automatically.**

Define your component dimensions as enums. Create an attribute. Every test runs once per combination — no loops, no parameterized boilerplate, no copy-paste.

## The problem

You have pluggable components — persistence layers, DI containers, serializers, transports. You need every test to pass with every supported combination. Without framework support, you end up with one of:

- **Copy-paste test classes** per combination — unmaintainable
- **Parameterized tests with manual wiring** — verbose and fragile
- **"We only test SQLite in CI"** — then production breaks on PostgreSQL

## How it works

### 1. Define your dimensions as enums

```csharp
public enum PersistenceLayer { Sqlite, SqliteMemory, MicrosoftSql, PostgreSql, MySql }
public enum DIContainer { Microsoft, SimpleInjector, Autofac }
```

### 2. Create your attribute

```csharp
public class MyMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<PersistenceLayer, DIContainer>(
      configurationFileName: null,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static PersistenceLayer PersistenceLayer => CurrentDimensionValue1;
   public static DIContainer DIContainer => CurrentDimensionValue2;
}
```

The generic type parameters define the matrix dimensions. With `configurationFileName: null`, every combination of every enum value runs automatically — the full Cartesian product.

`CurrentDimensionValue1`, `CurrentDimensionValue2`, etc. give type-safe access to the current combination's value for each dimension. Expose them as named properties for readability.

### 3. Use it

```csharp
public class When_saving_data
{
   readonly IDatabase _db;

   public When_saving_data()
   {
      var persistence = MyMatrixAttribute.PersistenceLayer;
      var container = MyMatrixAttribute.DIContainer;
      _db = CreateDatabase(persistence, container);
      _db.Save(new Widget { Id = 1, Name = "sprocket" });
   }

   [MyMatrix] public void Saved_widget_can_be_loaded() =>
      _db.Load<Widget>(1).Name.Must().Be("sprocket");

   [MyMatrix] public void Loading_nonexistent_widget_throws() =>
      Invoking(() => _db.Load<Widget>(999)).Must().Throw<NotFoundException>();
}
```

> **Note:** The assertions above use our [Compze.Must](https://www.nuget.org/packages/Compze.Must) fluent assertion library. You may want to check it out.

The constructor does setup using the current combination. Each test method is just an assertion. No parameters on the test methods — the combination flows via the attribute's static properties. In Test Explorer, each test appears once per combination with the combination shown in the test name.

### Skipping specific combinations

Some combinations may not be supported. Skip them with `[Skip<T>]`:

```csharp
[MyMatrix]
[Skip<PersistenceLayer>(PersistenceLayer.SqliteMemory, "Sqlite doesn't support this feature")]
public void Uses_advanced_sql_feature() { }
```

Multiple values from the same dimension can be skipped with one attribute:

```csharp
[MyMatrix]
[Skip<PersistenceLayer>([PersistenceLayer.Sqlite, PersistenceLayer.SqliteMemory], "SQLite deadlocks under parallel writes")]
public void Multithreaded_test() { }
```

The generic type parameter ensures the enum type is preserved through IL metadata encoding, and the compiler prevents passing a value from the wrong enum type.

## Any number of dimensions

Generic convenience base classes `MatrixTheoryAttribute<T1>` through `MatrixTheoryAttribute<T1, T2, T3, T4, T5>` cover the common cases — each exposes `CurrentDimensionValue1` through `CurrentDimensionValueN` for type-safe access.

For more than 5 dimensions, inherit directly from the non-generic `MatrixTheoryAttribute` and pass the component enum types manually.

## Configuration files

Sometimes you don't want to run every combination. A configuration file lets you list exactly which combinations to run — one per line, or with `*` as an auto-expanding wildcard:

```
# Only run these specific combinations
SqliteMemory:Autofac
SqliteMemory:SimpleInjector
*:Microsoft
```

The `*` on the third line expands to every `PersistenceLayer` value paired with `Microsoft` — `Sqlite:Microsoft`, `SqliteMemory:Microsoft`, `MicrosoftSql:Microsoft`, `PostgreSql:Microsoft`, `MySql:Microsoft` — for seven combinations total from three lines. Lines starting with `#` are comments.

Pass the file name as `configurationFileName` in your attribute constructor instead of `null`. The file is located relative to the test assembly's output directory, so configure your test project to copy it there:

```xml
<None Update="MyCombinations">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Must](https://www.nuget.org/packages/Compze.Must) | Fluent assertions (`Must().Be()`, `Must().Throw<>()`, etc.) |
| [Compze.xUnitBDD](https://www.nuget.org/packages/Compze.xUnitBDD) | BDD-style specification testing |

## License

Apache-2.0
