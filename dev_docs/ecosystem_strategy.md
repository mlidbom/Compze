# Compze Product Packaging Strategy

## Guiding Principles


### Why not one big package?
#### Promotion
Merging Contracts, Functional, DependencyInjection, DbPool, InterProcessObjects, Testing tooling, etc. into `Compze.Utilities` would produce an unpromptable mess. Nobody knows what that package *is* or whether they need it.

#### Encapsulation
- Assemblies provide vilibility barries
- Each sub-project can, and should be, tested fully by its own test suite.


### Why not minimize dependencies?
Compze.* packages freely depend on each other. For us, depending on other Compze libraries is similar to depending on the BCL — we built these abstractions to reuse them. Dog-fooding increases visibility of the ecosystem and is a credibility signal. Internalizing copies creates forks we must maintain for zero user-facing benefit.

### Granularity serves discoverability, not independence
We are **not** optimizing for "can I use this on a desert island with no other Compze packages." We are optimizing for "can I find this, understand what it does, and decide to adopt it."

### Sub-products should be extractable
Each sub-product should be conceptually separate even though we use a mono-repo. It should have its own isolated solution, production projects, and test projects. Theoretically we should be able to easily split each into its own repository.

### FlexRef makes this practical
[Compze.Build.FlexRef](https://www.nuget.org/packages/Compze.Build.FlexRef) lets any `.slnx` include any subset of projects. Flex references become `ProjectReference` when the project is in the solution, `PackageReference` when it's not. This means:
- Each sub-product can have its own `.slnx` for focused work
- The monolithic `Compze.slnx` still exists for CI and cross-cutting refactors
- No duplication or special configuration per solution

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
> Extensions and abstractions over System.* — IO, collections, LINQ, etc.

- `src/Compze.Utilities.SystemCE`
- **Dependencies**: Contracts, Functional, ThreadingCE (below)
- **Status**: Published as NuGet package.
- **Note**: Contains `MachineWideSharedObject` which may move to its own sub-product under Threading.

---

### Threading
> Thread-safe primitives, synchronization, resource access, inter-process coordination.

- `src/Compze.Utilities.SystemCE.ThreadingCE` — core threading primitives (MutexCE, IMonitor, ResourceAccess, etc.)
- `src/Compze.Utilities.SystemCE.ThreadingCE.Testing` — testing utilities for threading code
- _Candidate_: `Compze.Threading.InterProcessObject` — `MachineWideSharedObject` extracted from SystemCE
- **Dependencies**: Contracts, Functional
- **Test projects**: Currently tested within `Compze.Tests.Unit.Internals` and `Compze.Tests.Performance.Internals` — would need dedicated test projects.

---

---

### DependencyInjection
> DI container abstractions and pluggable implementations.

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

### Core
> Core framework abstractions (serialization, aggregate roots, value objects, etc.)

- `src/Compze.Core`
- `src/Compze.Serialization.Newtonsoft`
- **Dependencies**: Contracts, DependencyInjection, Logging, SystemCE, ThreadingCE
- **Open question**: Core may not survive long-term. It was designed around minimizing project count to avoid overloading Visual Studio. FlexRef + partial solutions solve that problem far better.

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

### Cross-Cutting Test Projects

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

---

## Dependency Direction (Bottom to Top)

```
Contracts    Functional
    \           /
     ThreadingCE
         |
      SystemCE
         |
      Logging
         |
  DependencyInjection
   /       |       \
Microsoft  SI    Serilog
         |
      Testing (Must, XUnit)
         |
   Core / Sql.Common
   /    |    |    \
MsSql MySql PgSql Sqlite
         |
       DbPool
         |
     Tessaging
     /       \
TeventStore  AspNetCore
         |
  Hosting.Testing
```

---

## Open Questions

1. **Core's future**: Does `Compze.Core` survive, or do its concepts get redistributed to Tessaging and Sql? It was born from a "minimize projects" strategy that FlexRef obsoletes.
2. **Naming**: Should extracted sub-products keep the `Compze.Utilities.*` prefix or get shorter names? E.g., `Compze.Threading.InterProcessObject` vs `Compze.Utilities.SystemCE.ThreadingCE.InterProcessObject`.
3. **Test decomposition timeline**: Splitting the monolithic test projects is gradual, hard work. Each test needs to find its natural sub-product home.
4. **When to actually restructure directories**: This is a plan, not an immediate action. The classification informs decisions; the directory moves can happen incrementally as sub-products mature.


## Internal Libraries (Not Promoted Separately)

These are published as NuGet packages (since other Compze packages depend on them), but are not intended as standalone offerings for external consumers.

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