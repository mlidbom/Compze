# NCrunch Initialization Diagnostics

## Problem
Tests frequently fail in NCrunch with the error:
```
NCrunch: This test was executed on server '(local)'

This test was not executed during a planned execution run. Ensure your test project is stable and does not contain issues in initialisation/teardown fixtures.
```

## Solution Implemented
Added comprehensive try-catch instrumentation to all XUnit and NUnit initialization code paths to capture and log any exceptions that might be occurring during test fixture initialization.

## Instrumented Files

### 1. XUnit Assembly Fixtures (Module Initializers)
- **File**: `src\Tests\Unit\XUnit\AssemblyFixture.cs`
  - **Method**: `AssemblySetup.Initialize()` (ModuleInitializer)
  - **Location Tag**: `Unit.XUnit.AssemblySetup.Initialize`

- **File**: `src\Tests\Unit\Internals\XUnit\AssemblyFixture.cs`
  - **Method**: `AssemblySetup.Initialize()` (ModuleInitializer)
  - **Location Tag**: `Unit.Internals.XUnit.AssemblySetup.Initialize`

### 2. NUnit Universal Test Fixture
- **File**: `src\Tests\Infrastructure\NUnit\UniversalTestFixture.cs`
  - **Method**: `UniversalSetup()` (OneTimeSetUp)
  - **Location Tag**: `NUnit.UniversalTestFixture.UniversalSetup`

### 3. Common Test Infrastructure
- **File**: `src\Tests\Infrastructure\TestFixtureHelper.cs`
  - **Method**: `PerformSetup()`
    - **Location Tag**: `TestFixtureHelper.PerformSetup`
  - **Method**: `AssertAllTestClassesInheritFromBase()`
    - **Location Tag**: `TestFixtureHelper.AssertAllTestClassesInheritFromBase (Assembly: {assembly.GetName().Name})`

## Log File Location
All initialization failures will be logged to:
```
c:\tmp\init_failure.txt
```

## Log Format
Each logged exception includes:
- Timestamp (yyyy-MM-dd HH:mm:ss.fff)
- Location identifier
- Exception type (full name)
- Exception message
- Full stack trace
- Exception ToString() output
- Separator line

## Next Steps
1. Run tests in NCrunch
2. If initialization failures occur, check `c:\tmp\init_failure.txt` for detailed exception information
3. Analyze the logged exceptions to identify the root cause
4. Fix the underlying issue
5. Remove or keep this diagnostic logging as needed

## Test Assemblies Covered
- `Tests.Unit` (XUnit)
- `Tests.Unit.Internals` (XUnit)
- `Tests.Integration` (NUnit, via Infrastructure.NUnit.UniversalTestFixture)
- `Tests.Integration.Internals` (NUnit, via Infrastructure.NUnit.UniversalTestFixture)
- `Tests.Performance.Internals` (NUnit, via Infrastructure.NUnit.UniversalTestFixture)

All NUnit test assemblies inherit from the instrumented `Tests.Infrastructure.NUnit.UniversalTestFixture`.
