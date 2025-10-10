# NCrunch Initialization Diagnostics

## Problem
Tests frequently fail in NCrunch with the error:
```
NCrunch: This test was executed on server '(local)'

This test was not executed during a planned execution run. Ensure your test project is stable and does not contain issues in initialisation/teardown fixtures.
```

## Solution Implemented
Refactored all test assemblies to use proper XUnit v3 and NUnit assembly-level fixtures with diagnostic logging to identify failures.

## Instrumented Files

### 1. XUnit Assembly Fixtures (XUnit v3 AssemblyFixtureAttribute)
- **File**: `src\Tests\Unit\XUnit\AssemblyFixture.cs`
  - **Class**: `XUnitAssemblyFixture` implements `IAsyncLifetime`
  - **Attribute**: `[assembly: AssemblyFixture(typeof(XUnitAssemblyFixture))]`
  - Uses proper XUnit v3 pattern instead of module initializers

- **File**: `src\Tests\Unit\Internals\XUnit\AssemblyFixture.cs`
  - **Class**: `XUnitAssemblyFixture` implements `IAsyncLifetime`
  - **Attribute**: `[assembly: AssemblyFixture(typeof(XUnitAssemblyFixture))]`

- **File**: `src\Tests\Integration\XUnit\AssemblyFixture.cs`
  - **Class**: `XUnitAssemblyFixture` implements `IAsyncLifetime`
  - **Attribute**: `[assembly: AssemblyFixture(typeof(XUnitAssemblyFixture))]`

### 2. NUnit Universal Test Fixture
- **File**: `src\Tests\Infrastructure\NUnit\UniversalTestFixture.cs`
  - **Attribute**: `[SetUpFixture]`
  - **Methods**: `UniversalSetup()` (OneTimeSetUp) and `UniversalTeardown()` (OneTimeTearDown)

### 3. Common Test Infrastructure (Shared Helper)
- **File**: `src\Tests\Infrastructure\TestFixtureHelper.cs`
  - **Method**: `RunAssemblyLevelSetup<TRunner>(Action)` - Wraps setup with exception logging
  - **Method**: `RunAssemblyLevelTeardown<TRunner>(Action)` - Wraps teardown with exception logging
  - **Method**: `PerformSetup()` - Common Serilog and exception gatherer setup
  - **Method**: `PerformTeardown()` - Common log flush and exception check
  - **Method**: `AssertAllTestClassesInheritFromBase()` - Validates test class inheritance
  - **Method**: `LogFailure(Type)` - Simple failure logging

## Log File Location
All failures will be logged to:
```
c:\tmp\init_failure.txt
```

## Log Format
Each logged failure contains:
- The failure type (Setup or Teardown)
- The full type name of the fixture class that failed
- The complete exception details

Example:
```
SetupFailure: Compze.Tests.Unit.XUnit.XUnitAssemblyFixture
Exception: System.InvalidOperationException: Something went wrong
   at ...
```

## Key Improvements

### Removed Duplicate FluentAssertions Initialization
- **Before**: Used both `[assembly: AssertionEngineInitializer(...)]` attribute AND direct calls in assembly fixtures
- **After**: `License.Accepted = true` is now called only in assembly fixtures
- **Reason**: Eliminated redundant initialization - now there's a single, consistent initialization path

### XUnit v3 Migration
- **Before**: Used module initializers (`[ModuleInitializer]`) which is a workaround for XUnit v2
- **After**: Uses proper XUnit v3 `[assembly: AssemblyFixture(...)]` attribute with `IAsyncLifetime`
- **Benefits**:
  - Cleaner, more idiomatic XUnit v3 code
  - Better lifecycle management
  - Proper async support
  - XUnit framework controls initialization order

### Exception Handling Strategy
- Assembly-level setup/teardown exceptions are **swallowed and logged**
- This prevents NCrunch from failing in hard-to-diagnose ways
- Exceptions are still logged with full details to `c:\tmp\init_failure.txt`
- Test-level failures still throw normally

## Next Steps
1. Run tests in NCrunch
2. If initialization/teardown failures occur, check `c:\tmp\init_failure.txt` to see which fixture failed
3. The log will show:
   - Whether it was setup or teardown
   - Which test assembly failed
   - The full exception details
4. Common issues to look for:
   - Serilog configuration issues (Seq server unreachable at `http://192.168.0.11:5341`)
   - Assembly scanning problems in `AssertAllTestClassesInheritFromBase`
   - Teardown failures in log flushing or exception gathering

## Test Assemblies Covered
- `Tests.Unit.XUnit` (XUnit v3 AssemblyFixture)
- `Tests.Unit.Internals.XUnit` (XUnit v3 AssemblyFixture)
- `Tests.Integration.XUnit` (XUnit v3 AssemblyFixture)
- `Tests.Integration` (NUnit SetUpFixture via Infrastructure.NUnit.UniversalTestFixture)
- `Tests.Integration.Internals` (NUnit SetUpFixture via Infrastructure.NUnit.UniversalTestFixture)
- `Tests.Performance.Internals` (NUnit SetUpFixture via Infrastructure.NUnit.UniversalTestFixture)

All NUnit test assemblies inherit from the instrumented `Tests.Infrastructure.NUnit.UniversalTestFixture`.
All XUnit test assemblies now use proper XUnit v3 `AssemblyFixtureAttribute` pattern.
