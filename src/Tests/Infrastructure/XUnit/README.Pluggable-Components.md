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

### Basic Test Pattern (RECOMMENDED)

The simplest way to create a test that runs for all pluggable component combinations:

```csharp
using Compze.Tessaging.Hosting.Testing;
using Xunit;

public class MyPluggableComponentTest : DuplicateByPluggableComponentTest
{
   [PluggableComponentsTheory]
   public void My_test(string pluggableComponentsCombination)  // ← String parameter gets the combination
   {
      // TestEnv.SetTestContext() is called automatically!
      // Just use TestEnv directly:
      var persistenceLayer = TestEnv.PersistenceLayer.Current;
      var diContainer = TestEnv.DIContainer.Current;

      // Your test logic here
      // ...
   }
}
```

**That's it!** The `[PluggableComponentsTheory]` attribute:
- Automatically discovers all combinations from the config file
- Passes the combination string as a parameter to your test
- Calls `TestEnv.SetTestContext()` before each test run
- Creates separate test cases in the test explorer

This is **almost as simple as the NUnit pattern** - just one parameter to declare!

### Advanced Pattern (Manual Control)

If you want full control:

```csharp
[Theory]
[MemberData(nameof(GetPluggableComponentCombinations), MemberType = typeof(DuplicateByPluggableComponentTest))]
public void My_test(string pluggableComponentsCombination)
{
   // STEP 1: Set the test context (REQUIRED!)
   TestEnv.SetTestContext(pluggableComponentsCombination);

   // STEP 2: Use TestEnv to access current configuration
   var persistenceLayer = TestEnv.PersistenceLayer.Current;
   var diContainer = TestEnv.DIContainer.Current;

   // STEP 3: Your test logic here
   // ...
}
```

### Using PersistenceLayer-Specific Values

When you need different values for different persistence layers:

```csharp
[PluggableComponentsTheory]
public void Test_with_layer_specific_values(string pluggableComponentsCombination)
{
   // Context is already set automatically
   var timeout = TestEnv.PersistenceLayer.ValueFor(
      msSql: TimeSpan.FromSeconds(5),
      mySql: TimeSpan.FromSeconds(10),
      pgSql: TimeSpan.FromSeconds(7),
      memory: TimeSpan.FromSeconds(1)
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
   public void Test_with_all_combinations(string pluggableComponentsCombination)
   {
      // Test logic - context already set
   }

   // Runs only once
   [Fact]
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

###PluggableComponentsTheoryAttribute

**This is the recommended way to write tests.** Custom XUnit attribute that:
- Discovers all pluggable component combinations from the config file
- Creates a separate test case for each combination
- Automatically calls `TestEnv.SetTestContext()` with the combination before each test
- Passes the combination string as a parameter to your test method
- Requires only a single string parameter in your test method

Usage: Replace `[Fact]` with `[PluggableComponentsTheory]` and add a string parameter!

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

### XUnit Pattern (RECOMMENDED)
```csharp
public class MyTest : DuplicateByPluggableComponentTest
{
   [PluggableComponentsTheory]
   public void My_test(string pluggableComponentsCombination)
   {
      // XUnit automatically sets context via custom attribute
      var persistenceLayer = TestEnv.PersistenceLayer.Current;
      // Test logic
   }
}
```

### XUnit Pattern (ADVANCED - if you need the combination string)
```csharp
public class MyTest : DuplicateByPluggableComponentTest
{
   [Theory]
   [MemberData(nameof(GetPluggableComponentCombinations), MemberType = typeof(DuplicateByPluggableComponentTest))]
   public void My_test(string pluggableComponentsCombination)
   {
      // XUnit requires explicit context setting in this mode
      TestEnv.SetTestContext(pluggableComponentsCombination);
      
      var persistenceLayer = TestEnv.PersistenceLayer.Current;
      // Test logic
   }
}
```

### Key Differences

| Aspect | NUnit | XUnit (Recommended) | XUnit (Advanced) |
|--------|-------|---------------------|------------------|
| **Class Instantiation** | Multiple instances, one per combination | Single instance | Single instance |
| **Context Setting** | Automatic via constructor | Automatic via `[PluggableComponentsTheory]` | Manual via `TestEnv.SetTestContext()` |
| **Test Discovery** | `[TestFixtureSource]` on class | `[PluggableComponentsTheory]` on method | `[Theory]` + `[MemberData]` on method |
| **Parameter Handling** | Constructor parameter | Method parameter (string) | Method parameter (string) |
| **Boilerplate** | Low | **Low** | Moderate |

## Common Pitfalls

### ❌ Forgetting the string parameter
```csharp
[PluggableComponentsTheory]
public void My_test() // ❌ Missing string parameter
{
   var layer = TestEnv.PersistenceLayer.Current;
}
```

### ✅ Correct usage
```csharp
[PluggableComponentsTheory]
public void My_test(string pluggableComponentsCombination) // ✅ 
{
   var layer = TestEnv.PersistenceLayer.Current; // ✅ Works!
}
```

### ❌ Using wrong attribute
```csharp
[Fact] // ❌ Should be [PluggableComponentsTheory]
public void My_test(string pluggableComponentsCombination)
{
   var layer = TestEnv.PersistenceLayer.Current; // ❌ Will throw exception!
}
```

## See Also

- `DuplicateByPluggableComponentTest.Example.cs` - Working example
- `TestUsingPluggableComponentCombinations` - Configuration file
- NUnit version: `Tests/Infrastructure/NUnit/DuplicateByPluggableComponentTest.cs`
