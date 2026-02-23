# Library Extraction Plan

## Strategy

Compze contains a set of utility libraries that are **not part of the Teventive/Typermedia core**. These libraries are valuable in their own right and could attract users to the broader Compze ecosystem.

The plan is to:

1. Get each into their own solutions with a test project for just them.
2.  **Get each library to "Good Enough"** — clean public API, adequate tests, no phantom dependencies
3. **Publish them as stable (0.8+) NuGet packages**
4. **Maintain them separately** in their own solution (`Compze.Threading.slnx` etc), only opening them when they need changes

This creates a clear boundary between stable utility code and the evolving core framework, while keeping a monolithic solution for CI and cross-cutting work.

## Solution Strategy

- **`Compze.slnx` stays monolithic** — all projects, all tests. Used for CI, cross-cutting refactors, and as the source of truth.
- **Focused solutions** (e.g., `Compze.Utilities.slnx`, `Compze.Tessaging.slnx`) are created for daily development — faster IDE, less noise, same code.
- **FlexRef** makes this frictionless. Projects outside the focused solution are consumed as NuGet packages automatically. Open a different solution and they become project references. No configuration, no switching — it just works.

## Why

- **Enforce stability.** Once published, these libraries have a real public API contract. Changes become deliberate.
- **Attract users.** Small focused libraries with clear value propositions are how people discover a framework. Someone finds `Compze.Utilities.Functional` via `unit`, likes it, explores further.
- **Developer ergonomics.** Working in a focused solution with 8 projects is dramatically faster than a monolithic 30+ project solution — without sacrificing the ability to see and build everything when needed.
- **FlexRef enables this.** The same tool that lets us work in subsets of the codebase now lets us treat stable libraries as packages while keeping them as project references in the full solution.

## Libraries and Status

### Tier 1 — Near-ready

| Library | Key Value Proposition | Compze Dependencies | Status |
|---------|----------------------|---------------------|--------|
| **Contracts** | Fluent precondition/postcondition assertions (`Assert.Argument`, `Assert.State`, `Assert.Result`) | None | Fix `IsValid` inverted logic bug. Add test coverage for UsageGuards and uncovered methods. |
| **Functional** | `unit` type eliminates the `Action`/`Func` split. `Pipe` enables fluent left-to-right composition. | Contracts (2 call sites, replaceable with BCL) | Remove `DiscriminatedUnion`, `Option`, `ObjectCE`. Move `EnumerableCE.OfTypes<>()` to SystemCE. What remains: `unit` and `Pipe`. |

### Tier 2 — Needs API review and naming pass

| Library | Key Value Proposition | Compze Dependencies | Work Needed |
|---------|----------------------|---------------------|-------------|
| **SystemCE** | The .NET extensions everyone writes but nobody publishes — ordinal string operations, `TimeSpan` factories, LINQ gaps, IO helpers, reflection utilities | Contracts, Functional, ThreadingCE | Naming pass (e.g., `.ContainsCE()` → `.ContainsOrdinal()`). Review what should be public vs internal. Ships as a pair with ThreadingCE due to circular dependency. |
| **ThreadingCE** | `IMonitorCE` — sane thread-safe resource access. `MachineWideSharedObject` (internal for now). Thread gates for testing. | Contracts, Functional (+ internalized SystemCE) | `MachineWideSharedObject` stays internal or becomes a separate package. `IMonitorCE` and testing utilities need API review. |
| **Must** | Fluent test assertions with exhaustive equality checking, deep comparison, and readable diff output | Functional, SystemCE, ThreadingCE (`.caf()` only) | Remove 3 phantom dependencies (DI, Logging, Contracts). ~100 lines of SystemCE usage are trivially inlinable if full independence is ever desired, but keeping the dependency is fine for now. |
| **XUnit** | `[XF]`/`[ExclusiveFact]` for BDD-style tests. `[PCT]` for pluggable component testing across all configured combinations. | Functional, SystemCE, ThreadingCE | BDD feature is nearly standalone. ComponentCombinations has moderate SystemCE coupling. Zero dedicated tests — relies on indirect coverage. |

### Tier 3 — Needs deeper extraction work

| Library | Key Value Proposition | Compze Dependencies | Work Needed |
|---------|----------------------|---------------------|-------------|
| **DependencyInjection** | Fluent DI with lifestyle validation (singleton-depends-on-scoped detection), type-safe factory registration | Contracts, Functional, SystemCE, ThreadingCE | Remove phantom Logging reference. Core API is clean. Consider whether transaction support should be separate. |
| **DbPool** | Machine-wide database pool for integration testing — reserves/releases databases across parallel test processes | 8 Compze packages (most incidental) | Move `IDbPoolSqlLayer`, `DbPoolDatabase`, `DbPoolId` into DbPool (currently in Sql.Common). Move `MachineWideSharedObject` + `MutexCE` in. Replace Compze logging with `Microsoft.Extensions.Logging`. Inline trivial SystemCE usages. |

### Not extracting (stays internal)

| Library | Reason |
|---------|--------|
| **Logging** | Coupled to `unit`, custom `ILogger` — not valuable enough externally to warrant the extraction effort. Primarily serves Compze internals. |

## Dependency Flow (post-extraction)

```
Contracts
    └── Functional (unit, Pipe)
            └── SystemCE (extensions, LINQ, IO, reflection)
                    └── ThreadingCE (IMonitorCE, thread gates, async lock)
                            ├── Must (test assertions)
                            ├── XUnit (test infrastructure)
                            ├── DependencyInjection
                            └── DbPool
```

Each layer depends only on the ones above it. Libraries within the same tier may depend on each other — this is intentional and showcases the ecosystem.

## Bugs to Fix Before Publishing

- [ ] **Contracts:** `ContractAsserter.IsValid<TEnum>` has inverted logic — succeeds on invalid, throws on valid
- [ ] **Functional:** 6-case `DiscriminatedUnion` `AllowedTypes` missing `TOption6` (moot if DiscriminatedUnion is removed)

## Process for Each Library

1. Review public API — names, placement, what should be public vs internal
2. Remove phantom/unnecessary dependencies from csproj
3. Fix known bugs
4. Write/verify test coverage for all public API surface
5. Update README with clear value proposition and usage examples
6. Set version to `0.8.0-beta.1`
7. Publish to NuGet
8. Update core Compze to consume via package reference in focused solutions
9. Create/update focused `.slnx` files as needed (monolithic `Compze.slnx` always keeps all projects)
