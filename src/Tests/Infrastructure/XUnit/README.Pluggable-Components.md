# XUnit Pluggable Components Testing

This infrastructure allows you to test all pluggable component combinations (persistence layers and DI containers) with minimal code duplication in XUnit.

## How It Works

### Overview

The system works differently from NUnit's `TestFixtureSource` approach because XUnit doesn't support parameterized test classes. Instead, we use:

1. **Theory tests with MemberData** - XUnit's `Theory` attribute with `MemberData` to provide test data
2. **Thread-local context** - Store the current combination in thread-local storage
3. **TestEnv helpers** - Use `TestEnv.PersistenceLayer.Current` and `TestEnv.DIContainer.Current` to access the current configuration

### Configuration File

Component combinations are defined in `TestUsingPluggableComponentCombinations` file at the repository root:

```
#When running tests, all tests that use dependency injection and persistence will 
#be executed once for every configured combination of components in this file.
#Format is PersistenceLayer:DIContainer. Comment out the ones you do not want with #. 
#Empty/Whitespace lines are ignored

MicrosoftSqlServer:Microsoft
Memory:Microsoft
MySql:Microsoft
PostgreSql:Microsoft

MicrosoftSqlServer:SimpleInjector
Memory:SimpleInjector
MySql:SimpleInjector
PostgreSql:SimpleInjector
```

## Usage

### Basic Test Pattern

The simplest way to create a test that runs for all pluggable component combinations:

```csharp
using Compze.Tests.Infrastructure.XUnit;
using Xunit;

public class MyPluggableComponentTest : DuplicateByPluggableComponentTest
{
   [PluggableComponentsTheory]
   public void My_test()
   {
      // Fully parsed context object with convenient API
      var persistenceLayer = context.PersistenceLayer;  // Enum value
      var diContainer = context.DIContainer;            // Enum value
      var combination = context.Combination;            // String like "MicrosoftSqlServer:Microsoft"

      // Your test logic here
      // ...
   }
}
```

**That's it!** The `[PluggableComponentsTheory]` attribute:
- Automatically discovers all combinations from the config file
- Creates a `PluggableComponentTestContext` instance for each combination
- Injects it into your test method
- Creates separate test cases in the test explorer

This is **cleaner and more type-safe** than string-based approaches!

### Using PersistenceLayer-Specific Values

When you need different values for different persistence layers, use the extension method on `PersistenceLayer`:

```csharp
[PluggableComponentsTheory]
public void Test_with_layer_specific_values()
{
   // Recommended: Use the extension method directly on PersistenceLayer
   var timeout = context.PersistenceLayer.ValueFor(
      msSql: TimeSpan.FromSeconds(5),
      mySql: TimeSpan.FromSeconds(10),
      pgSql: TimeSpan.FromSeconds(7),
      memory: TimeSpan.FromSeconds(1)
   );

   // Alternative: Use the ValueForDb alias on context
   var connectionString = context.ValueForDb(
      msSql: "Server=localhost;...",
      memory: "InMemory",
      mySql: "Server=localhost;...",
      pgSql: "Host=localhost;..."
   );

   // Use the timeout appropriate for the current persistence layer
}
```

### Mixed Tests

You can have both pluggable component tests and regular tests in the same class:

```csharp
public class MyTest : DuplicateByPluggableComponentTest
{
   // Runs for each combination
   [PluggableComponentsTheory]
   public void Test_with_all_combinations()
   {
      // Test logic with context
   }

   // Runs only once
   [XFact]
   public void Regular_test()
   {
      // Test logic
   }
}
```

## Key Components

### DuplicateByPluggableComponentTest

Base class that provides:
- `GetPluggableComponentCombinations()` - Static method that returns all combinations from the configuration file
- Inherits from `UniversalTestBase` for common test infrastructure

### PluggableComponentsTheoryAttribute

Custom XUnit attribute that:
- Discovers all pluggable component combinations from the config file
- Creates a separate test case for each combination
- Automatically creates and injects a `PluggableComponentTestContext` instance
- Provides type-safe access to configuration via the context object

Usage: Use `[PluggableComponentsTheory]` instead of `[XFact]` and add a `PluggableComponentTestContext` parameter!

### PluggableComponentTestContext

Instance-based test context that provides:
- `PersistenceLayer` - Enum value of the current persistence layer
- `DIContainer` - Enum value of the current DI container
- `Combination` - Full combination string (e.g., "MicrosoftSqlServer:Microsoft")
- `ValueFor<T>()` - Get persistence-layer-specific values

### TestEnv

Static class providing test environment utilities:

#### TestEnv.SetTestContext(string)
**MUST be called at the start of every pluggable component test method.**
Sets the thread-local context so that `TestEnv.PersistenceLayer.Current` and `TestEnv.DIContainer.Current` work correctly.

#### TestEnv.PersistenceLayer.Current
Returns the current `Compze.Wiring.PersistenceLayer` enum value for the test.

#### TestEnv.DIContainer.Current
Returns the current `Compze.Wiring.DIContainer` enum value for the test.

#### TestEnv.PersistenceLayer.ValueFor<T>(...)
Returns a persistence-layer-specific value. Takes named parameters:
- `msSql` - Value for Microsoft SQL Server
- `mySql` - Value for MySQL  
- `pgSql` - Value for PostgreSQL
- `memory` - Value for in-memory persistence
- `db2` - Value for DB2 (if supported)
- `orcl` - Value for Oracle (if supported)

Throws an exception if the required value for the current layer is not provided.

## Comparison with NUnit

### NUnit Pattern (for reference)
```csharp
[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public class MyTest : DuplicateByPluggableComponentTest
{
   public MyTest(string pluggableComponentsCombination) {}

   [Test]
   public void My_test()
   {
      // NUnit automatically sets context via test fixture parameters
      var persistenceLayer = TestEnv.PersistenceLayer.Current;
      // Test logic
   }
}
```

### XUnit Pattern
```csharp
public class MyTest : DuplicateByPluggableComponentTest
{
   [PluggableComponentsTheory]
   public void My_test()
   {
      // XUnit automatically injects context object
      var persistenceLayer = context.PersistenceLayer;
      // Test logic
   }
}
```

### Key Differences

| Aspect | NUnit | XUnit |
|--------|-------|-------|
| **Class Instantiation** | Multiple instances, one per combination | Single instance |
| **Context Setting** | Automatic via constructor | Automatic via `[PluggableComponentsTheory]` |
| **Test Discovery** | `[TestFixtureSource]` on class | `[PluggableComponentsTheory]` on method |
| **Parameter Handling** | Constructor parameter | Method parameter (context object) |
| **Type Safety** | String parsing required | Full type safety with context object |
| **Boilerplate** | Low | **Low** |

## Common Pitfalls

### ❌ Forgetting the context parameter
```csharp
[PluggableComponentsTheory]
public void My_test() // ❌ Missing PluggableComponentTestContext parameter
{
   // Won't work - no context injected
}
```

### ✅ Correct usage
```csharp
[PluggableComponentsTheory]
public void My_test() // ✅ 
{
   var layer = context.PersistenceLayer; // ✅ Works!
}
```

### ❌ Using wrong attribute
```csharp
[XFact] // ❌ Should be [PluggableComponentsTheory]
public void My_test()
{
   // Won't run multiple times
}
```

## See Also

- `DuplicateByPluggableComponentTest.Example.cs` - Working example
- `TestUsingPluggableComponentCombinations` - Configuration file
- NUnit version: `Tests/Infrastructure/NUnit/DuplicateByPluggableComponentTest.cs`
