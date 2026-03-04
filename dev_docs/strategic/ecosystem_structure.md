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
> Pipe forward operator, unit type, Action/Func converters, chainable extensions.
- `src/Compze.Underscore`
- `test/Compze.Underscore.Specifications`
- **Dependencies**: _(none)_
- **Status**: Already published as a separate NuGet package.
- **Planned**: Rename to `Compze.Fluent` and extract the `unit` type + Action/Func converters into a new `Compze.Unit` package. "Functional" overpromises — the library doesn't address immutability, monads, or other FP tenets. What it provides is fluent, left-to-right chainable code. Extracting `unit` also breaks the circular dependency that would otherwise exist if SystemCE needs to depend on Fluent while Fluent depends on SystemCE.

---

### SystemCE
> Quality of life extensions and abstractions over System.* — IO, collections, LINQ, etc.

- `src/Compze.Utilities.SystemCE`
- **Dependencies**: Contracts, Functional
- **Status**: Published as NuGet package.
- **Note**: Contains `MachineWideSharedObject` which may move to its own sub-product under Threading.

---

### Threading
> Thread safety made easy and reliable.

- `src/Compze.Utilities.SystemCE.ThreadingCE` — core threading primitives (MutexCE, IMonitor, ResourceAccess, etc.)
- `src/Compze.Utilities.SystemCE.ThreadingCE.Testing` — testing utilities for threading code
- _Candidate_: `Compze.Threading.InterProcessObject` — `MachineWideSharedObject` extracted from SystemCE
- **Dependencies**: Contracts, Functional, SystemCE
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

- `src/Compze.Must` — assertion library
- `src/Compze.Utilities.Testing.XUnit` — xUnit integration and base classes
- `test/Compze.Utilities.Testing.XUnit.Tests`
- **Dependencies**: Contracts, Functional, DependencyInjection, Logging, SystemCE, ThreadingCE

---

### DbPool
> Cross-process test database pooling with pluggable SQL providers.

- `src/Compze.DbPool` — core pooling (uses `MachineWideSharedObject` for cross-process state)
- `src/Compze.DbPool.MicrosoftSql`
- `src/Compze.DbPool.MySql`
- `src/Compze.DbPool.PostgreSql`
- `src/Compze.DbPool.Sqlite`
- `test/Compze.DbPool.Tests` — DbPool tests
- **Dependencies**: Contracts, Functional, DependencyInjection, Logging, SystemCE, ThreadingCE, Sql (internal)

---

### Tessaging
> Type-based messaging (Typermedia) + Teventive programming.

This is the largest sub-product, and the one that composes everything else.

- `src/Compze.Tessaging` — messaging infrastructure
- `src/Compze.Tessaging.Teventive.TeventStore` — event store
- `src/Compze.Tessaging.Hosting.AspNetCore` — ASP.NET Core hosting
- `src/Compze.Tessaging.Hosting.Testing` — testing host (the "hub" project, 23 direct references)
- **Dependencies**: Nearly everything — Contracts, Core, Functional, Sql, DI, Logging, SystemCE, ThreadingCE, Serialization, Testing, DbPool
- **Planned**: Extract Teventive and Typermedia as top-level sub-products (`Compze.Teventive`, `Compze.Typermedia`). Tessaging itself either survives as a thin shared abstraction layer (interfaces and base classes only) or is eliminated entirely, with its contents absorbed into Teventive and Typermedia.

---

## Internal Libraries (Not Promoted Separately)

These are published as NuGet packages (since other Compze packages depend on them), but are not intended as standalone offerings for external consumers.

### Core
Core was designed around minimizing project count to avoid overloading Visual Studio. FlexRef + partial solutions solve that problem far better. **Planned**: Dissolve Core — aggregate/event interfaces move to Teventive, command/query routing moves to Typermedia.

### Sql
> SQL abstractions and provider implementations. Internal plumbing consumed by DbPool and Tessaging.

- `src/Compze.Sql.Common` — shared SQL infrastructure
- `src/Compze.Sql.MicrosoftSql`
- `src/Compze.Sql.MySql`
- `src/Compze.Sql.PostgreSql`
- `src/Compze.Sql.Sqlite`
- **Dependencies**: Contracts, Core, DependencyInjection, Logging, SystemCE, ThreadingCE

### Logging
> Logging abstractions and Serilog implementation. Internal — the overall design is not yet settled for general use.

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

**Strategy**: Black-box testing. Tests that compose many things belong with the sub-product doing the composing. Partial duplication across sub-products is fine.