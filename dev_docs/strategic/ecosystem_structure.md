# Compze Sub-Product Structure

See also: [Product Packaging Strategy](ecosystem_strategy.md)

---

## Sub-Product Classification

### Foundation Layer
These are the lowest-level building blocks. Each is a promotable concept in its own right.

#### Contracts
> Argument validation, preconditions, postconditions.
- `src/Compze.Contracts`
- `test/Compze.Contracts.Specifications`
- **Dependencies**: _(none)_
- **Status**: Already published as a separate NuGet package.

#### Functional
> Functional programming extensions for C#.
- `src/Compze.Functional`
- `test/Compze.Functional.Specifications`
- **Dependencies**: _(none)_
- **Status**: Already published as a separate NuGet package.

---

### SystemCE
> Quality of life extensions and abstractions over System.* — IO, collections, LINQ, etc.

- `src/Compze.Utilities.SystemCE`
- **Dependencies**: Contracts, Functional, ThreadingCE (below)
- **Status**: Published as NuGet package.
- **Note**: Contains `MachineWideSharedObject` which may move to its own sub-product under Threading.

---

### Threading
> Thread safety made easy and reliable.

- `src/Compze.Utilities.SystemCE.ThreadingCE` — core threading primitives (MutexCE, IMonitor, ResourceAccess, etc.)
- `src/Compze.Utilities.SystemCE.ThreadingCE.Testing` — testing utilities for threading code
- _Candidate_: `Compze.Threading.InterProcessObject` — `MachineWideSharedObject` extracted from SystemCE
- **Dependencies**: Contracts, Functional
- **Test projects**: Currently tested within `Compze.Tests.Unit.Internals` and `Compze.Tests.Performance.Internals` — would need dedicated test projects.

---

### DependencyInjection
> Minimalistic DI library abstraction layer removing complexity and DI "magic" while making swapping out components in testing a first class supported concern.

- `src/Compze.Utilities.DependencyInjection` — abstractions
- `src/Compze.Utilities.DependencyInjection.Microsoft` — Microsoft DI implementation
- `src/Compze.Utilities.DependencyInjection.SimpleInjector` — SimpleInjector implementation
- **Dependencies**: Contracts, Functional, Logging, SystemCE, ThreadingCE
- **Test projects**: Currently tested within `Compze.Tests.Unit.Internals` and `Compze.Tests.Integration` — would need dedicated test projects.

---

### Testing
> Test infrastructure, assertion libraries, xUnit integration.

- `src/Compze.Utilities.Testing.Must` — assertion library
- `src/Compze.Utilities.Testing.XUnit` — xUnit integration and base classes
- `test/Compze.Utilities.Testing.XUnit.Tests`
- **Dependencies**: Contracts, Functional, DependencyInjection, Logging, SystemCE, ThreadingCE

---

### DbPool
> Cross-process test database pooling with pluggable SQL providers.

- `src/Compze.Utilities.Testing.DbPool` — core pooling (uses `MachineWideSharedObject` for cross-process state)
- `src/Compze.Utilities.Testing.DbPool.MicrosoftSql`
- `src/Compze.Utilities.Testing.DbPool.MySql`
- `src/Compze.Utilities.Testing.DbPool.PostgreSql`
- `src/Compze.Utilities.Testing.DbPool.Sqlite`
- `test/Compze.Utilities.Tests` (partially — DbPool tests live here)
- **Dependencies**: Contracts, Functional, DependencyInjection, Logging, SystemCE, ThreadingCE, Sql (internal)

---

---

### Tessaging
> Type-based messaging (Typermedia) + Teventive programming.

This is the largest sub-product, and the one that composes everything else.

- `src/Compze.Tessaging` — messaging infrastructure
- `src/Compze.Tessaging.Teventive.TeventStore` — event store
- `src/Compze.Tessaging.Hosting.AspNetCore` — ASP.NET Core hosting
- `src/Compze.Tessaging.Hosting.Testing` — testing host (the "hub" project, 23 direct references)
- **Dependencies**: Nearly everything — Contracts, Core, Functional, Sql, DI, Logging, SystemCE, ThreadingCE, Serialization, Testing, DbPool

---

## Internal Libraries (Not Promoted Separately)

These are published as NuGet packages (since other Compze packages depend on them), but are not intended as standalone offerings for external consumers.

### Core
Core was designed around minimizing project count to avoid overloading Visual Studio. FlexRef + partial solutions solve that problem far better. We should probably consider removing it

### Sql
> SQL abstractions and provider implementations. Internal plumbing consumed by DbPool and Tessaging.

- `src/Compze.Sql.Common` — shared SQL infrastructure
- `src/Compze.Sql.MicrosoftSql`
- `src/Compze.Sql.MySql`
- `src/Compze.Sql.PostgreSql`
- `src/Compze.Sql.Sqlite`
- **Dependencies**: Contracts, Core, DependencyInjection, Logging, SystemCE, ThreadingCE

### Logging
> Logging abstractions and Serilog implementation. Potential long-term candidate for promotion, but currently internal.

- `src/Compze.Utilities.Logging` — abstractions
- `src/Compze.Utilities.Logging.Serilog` — Serilog implementation
- **Dependencies**: Contracts, Functional, SystemCE, ThreadingCE

---

## Cross-Cutting Test Projects

These projects currently span multiple sub-products. Over time, tests should migrate to their natural homes.

| Project | Current Role | Natural Home |
|---------|-------------|-------------|
| `test/Compze.Tests.Infrastructure` | XUnit attributes, UniversalTestBase, PCT | Testing sub-product (it's test infra) |
| `test/Compze.Tests.Common` | Shared test base classes | Tessaging (depends on Tessaging.Hosting.Testing) |
| `test/Compze.Tests.Unit` | Mixed unit tests | Decompose into per-sub-product test projects |
| `test/Compze.Tests.Unit.Internals` | Mixed unit tests (internals) | Decompose into per-sub-product test projects |
| `test/Compze.Tests.Integration` | Integration tests | Tessaging (tests composition of the full stack) |
| `test/Compze.Tests.Performance.Internals` | Performance tests | Decompose — some Threading, some Tessaging |
| `test/Compze.Tests.CodePolicies` | Policy enforcement across all projects | Stays at root (references everything by design) |
| `test/Compze.Tests.ScratchPad` | Experimental/scratch | Stays at root |

**Strategy**: Black-box testing. Tests that compose many things belong with the sub-product doing the composing (Tessaging, Teventive). Partial duplication across sub-products is fine.

---

## Possible Future Directory Layout

```
products/
  Contracts/
    src/Compze.Contracts/
    test/Compze.Contracts.Specifications/
    Contracts.slnx

  Functional/
    src/Compze.Functional/
    test/Compze.Functional.Specifications/
    Functional.slnx

  Threading/
    src/Compze.Utilities.SystemCE.ThreadingCE/
    src/Compze.Utilities.SystemCE.ThreadingCE.Testing/
    src/Compze.Threading.InterProcessObject/          (new, extracted from SystemCE)
    test/Compze.Threading.Tests/
    Threading.slnx

  SystemCE/
    src/Compze.Utilities.SystemCE/
    test/Compze.SystemCE.Tests/
    SystemCE.slnx

  DependencyInjection/
    src/Compze.Utilities.DependencyInjection/
    src/Compze.Utilities.DependencyInjection.Microsoft/
    src/Compze.Utilities.DependencyInjection.SimpleInjector/
    test/Compze.DependencyInjection.Tests/
    DependencyInjection.slnx

  Testing/
    src/Compze.Utilities.Testing.Must/
    src/Compze.Utilities.Testing.XUnit/
    test/Compze.Utilities.Testing.XUnit.Tests/
    Testing.slnx

  DbPool/
    src/Compze.Utilities.Testing.DbPool/
    src/Compze.Utilities.Testing.DbPool.MicrosoftSql/
    src/Compze.Utilities.Testing.DbPool.MySql/
    src/Compze.Utilities.Testing.DbPool.PostgreSql/
    src/Compze.Utilities.Testing.DbPool.Sqlite/
    test/Compze.DbPool.Tests/
    DbPool.slnx

  Tessaging/
    src/Compze.Core/                                  (if it survives)
    src/Compze.Serialization.Newtonsoft/
    src/Compze.Tessaging/
    src/Compze.Tessaging.Teventive.TeventStore/
    src/Compze.Tessaging.Hosting.AspNetCore/
    src/Compze.Tessaging.Hosting.Testing/
    test/Compze.Tests.Common/
    test/Compze.Tests.Infrastructure/
    test/Compze.Tests.Unit/
    test/Compze.Tests.Integration/
    test/Compze.Tests.Performance.Internals/
    Tessaging.slnx

test/
  Compze.Tests.CodePolicies/                          (stays at root — references everything)
  Compze.Tests.ScratchPad/                            (stays at root — experimental)

Compze.slnx                                          (monolithic — everything, for CI)
```