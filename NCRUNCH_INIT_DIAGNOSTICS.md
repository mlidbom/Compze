# NCrunch Initialization Diagnostics

## Problem
Tests frequently fail in NCrunch with the error:
```
NCrunch: This test was executed on server '(local)'

This test was not executed during a planned execution run. Ensure your test project is stable and does not contain issues in initialisation/teardown fixtures.
```

## Solution Implemented
Added minimal try-catch instrumentation to all XUnit and NUnit initialization and teardown code paths to log which class fails during fixture setup or teardown.

## Instrumented Files

### 1. Common Test Infrastructure (Shared Helper)
- **File**: `src\Tests\Infrastructure\TestFixtureHelper.cs`
  - **Method**: `LogFailure(Type type)` - Simple helper that logs just the type's FullName
  - **Method**: `PerformSetup()` - Wrapped in try-catch
  - **Method**: `PerformTeardown()` - Wrapped in try-catch
  - **Method**: `AssertAllTestClassesInheritFromBase()` - Wrapped in try-catch

### 2. XUnit Assembly Fixtures (Module Initializers)
- **File**: `src\Tests\Unit\XUnit\AssemblyFixture.cs`
  - **Method**: `Initialize()` - Setup wrapped in try-catch
  - **Method**: `Cleanup()` - Teardown wrapped in try-catch

- **File**: `src\Tests\Unit\Internals\XUnit\AssemblyFixture.cs`
  - **Method**: `Initialize()` - Setup wrapped in try-catch
  - **Method**: `Cleanup()` - Teardown wrapped in try-catch

### 3. NUnit Universal Test Fixture
- **File**: `src\Tests\Infrastructure\NUnit\UniversalTestFixture.cs`
  - **Method**: `UniversalSetup()` - OneTimeSetUp wrapped in try-catch
  - **Method**: `UniversalTeardown()` - OneTimeTearDown wrapped in try-catch

## Log File Location
All failures will be logged to:
```
c:\tmp\init_failure.txt
```

## Log Format
Each logged failure contains just the full type name of the class that failed, one per line:
```
Compze.Tests.Unit.XUnit.AssemblySetup
Compze.Tests.Infrastructure.NUnit.UniversalTestFixture
Compze.Tests.Infrastructure.TestFixtureHelper
```

## Next Steps
1. Run tests in NCrunch
2. If initialization/teardown failures occur, check `c:\tmp\init_failure.txt` to see which class failed
3. The class name will tell you whether the failure was in:
   - Setup or teardown logic (based on which class is logged)
   - XUnit or NUnit infrastructure
   - Common helper methods
4. Investigate the specific class to determine the root cause
5. Common issues to look for:
   - Serilog configuration issues (Seq server unreachable at `http://192.168.0.11:5341`)
   - Assembly scanning problems in `AssertAllTestClassesInheritFromBase`
   - Teardown failures in log flushing or exception gathering

## Test Assemblies Covered
- `Tests.Unit` (XUnit) - Setup and Cleanup
- `Tests.Unit.Internals` (XUnit) - Setup and Cleanup
- `Tests.Integration` (NUnit) - Setup and Teardown (via Infrastructure.NUnit.UniversalTestFixture)
- `Tests.Integration.Internals` (NUnit) - Setup and Teardown (via Infrastructure.NUnit.UniversalTestFixture)
- `Tests.Performance.Internals` (NUnit) - Setup and Teardown (via Infrastructure.NUnit.UniversalTestFixture)

All NUnit test assemblies inherit from the instrumented `Tests.Infrastructure.NUnit.UniversalTestFixture`.
