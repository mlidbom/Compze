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
- **Done since**: the `unit` type + Action/Func converters live in their own `Compze.Unit` package (`src/Compze.Unit`). **Still planned**: Rename to `Compze.Fluent` — "Functional" overpromises; the library doesn't address immutability, monads, or other FP tenets. What it provides is fluent, left-to-right chainable code.

---

### SystemCE
> Quality of life extensions and abstractions over System.* — IO, collections, LINQ, etc.

- `src/Compze.Internals.SystemCE`
- **Dependencies**: Contracts, Functional
- **Status**: Published as NuGet package (internal).
- **Done since**: `MachineWideSharedObject` moved out and grew into its own sub-product — `IInterprocessObject` in `src/Compze.InterprocessObject`.

---

### Threading
> Thread safety made easy and reliable.

- `src/Compze.Threading` — core threading primitives (MutexCE, IMonitor, ResourceAccess, etc.)
- `src/Compze.Threading.Interprocess` — the cross-process implementations (IAwaitableMutex, InterprocessSignal, InterprocessChangeCounter)
- `src/Compze.Threading.Testing` — testing utilities for threading code
- **Done since**: the interprocess-object candidate shipped as its own sub-product — `src/Compze.InterprocessObject` (+ `Compze.InterprocessObject.MemoryPack`).
- **Dependencies**: Contracts, Functional, SystemCE
- **Test projects**: dedicated projects exist — `test/Compze.Threading.Specifications`, `test/Compze.Threading.Tests`, `test/Compze.Threading.InternalSpecifications`, `test/Compze.Threading.Testing.Specifications`.

---

### DependencyInjection
> Minimalistic DI library abstraction layer removing complexity and DI "magic" while making swapping out components in testing a first class supported concern.

- `src/Compze.DependencyInjection` — abstractions
- `src/Compze.DependencyInjection.{Microsoft,Autofac,DryIoc,LightInject}` — container implementations (each with an `.Extensions.Hosting` adapter)
- **Dependencies**: Contracts, Functional, Logging, SystemCE, ThreadingCE
- **Test projects**: dedicated projects exist — `test/Compze.DependencyInjection.Specifications`, `test/Compze.DependencyInjection.MsDiCompliance`.

---

### Testing
> Test infrastructure, assertion libraries, xUnit integration.

- `src/Compze.Must` — assertion library
- `src/Compze.xUnit`, `src/Compze.xUnitBDD`, `src/Compze.xUnitMatrix` — xUnit integration, BDD-style specifications, matrix testing
- `test/Compze.xUnitBDD.Tests`, `test/Compze.xUnitMatrix.Tests`, `test/Compze.Must.Specifications`
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
- `src/Compze.Teventive.TeventStore` — the tevent store
- `src/Compze.Internals.Transport.AspNet` — the ASP.NET transport
- `src/Compze.Tessaging.Hosting.Testing` — testing host
- **Dependencies**: Nearly everything — Contracts, Functional, Sql, DI, Logging, SystemCE, ThreadingCE, Serialization, Testing, DbPool
- **Done since**: Teventive and Typermedia are extracted as top-level sub-products (`Compze.Teventive`, `Compze.Typermedia`); Tessaging survives as the messaging sub-product.

---

## Internal Libraries (`Compze.Internals.*`)

These are published as NuGet packages (since other Compze packages depend on them), but are **not intended as standalone offerings for external consumers**. They use the `Compze.Internals.*` naming prefix to clearly signal this. The API may change without notice. A future analyzer will prohibit client code from referencing `Compze.Internals.*` namespaces.

### Core
Core was designed around minimizing project count to avoid overloading Visual Studio. FlexRef + partial solutions solve that problem far better. **Done since**: Core has been dissolved — the taggregate/tevent interfaces live in Teventive and `Compze.Abstractions`, tommand/tuery routing in Tessaging and Typermedia.

### Sql
> SQL abstractions and provider implementations. Internal plumbing consumed by DbPool and Tessaging.

- `src/Compze.Internals.Sql.Common` — shared SQL infrastructure
- `src/Compze.Internals.Sql.MicrosoftSql`
- `src/Compze.Internals.Sql.MySql`
- `src/Compze.Internals.Sql.PostgreSql`
- `src/Compze.Internals.Sql.Sqlite`
- **Dependencies**: Contracts, DependencyInjection, Logging, SystemCE, ThreadingCE

### Serialization
> Serialization implementations. Internal — pluggable implementation detail.

- `src/Compze.Internals.Serialization.Newtonsoft` — Newtonsoft.Json serialization
- **Dependencies**: Abstractions, Newtonsoft.Json

### Logging
> Logging abstractions and Serilog implementation. Internal — the overall design is not yet settled for general use.

- `src/Compze.Internals.Logging` — abstractions
- `src/Compze.Internals.Logging.Serilog` — Serilog implementation
- **Dependencies**: Contracts, Functional, SystemCE, ThreadingCE

---

## Cross-Cutting Test Projects

These projects currently span multiple sub-products. Over time, tests should migrate to their natural homes.

| Project | Current Role | Natural Home |
|---------|-------------|-------------|
| `test/Compze.Tests.Infrastructure` | XUnit attributes, UniversalTestBase, PCT | Testing sub-product (it's test infra) |
| `test/Compze.Tests.Common` | Shared test base classes | Tessaging (depends on Tessaging.Hosting.Testing) |
| `test/Compze.Tests.Unit` | Mixed unit tests | Decompose into per-sub-product test projects |
| `test/Compze.Tests.Integration` | Integration tests | Tessaging (tests composition of the full stack) |
| `test/Compze.Tests.Performance.Internals` | Performance tests | Decompose — some Threading, some Tessaging |
| `test/Compze.Tests.CodePolicies` | Policy enforcement across all projects | Stays at root (references everything by design) |
| `test/Compze.Tests.ScratchPad` | Experimental/scratch | Stays at root |

**Strategy**: Black-box testing. Tests that compose many things belong with the sub-product doing the composing. Partial duplication across sub-products is fine.