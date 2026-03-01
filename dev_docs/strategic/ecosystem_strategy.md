# Compze Product Packaging Strategy

See also: [Sub-Product Structure](ecosystem_structure.md)

## Guiding Principles


### Why not one big package?

#### Promotion
Merging Contracts, Functional, DependencyInjection, DbPool, InterProcessObjects, Testing tooling, etc. into `Compze.Utilities` would produce an unpromptable mess. Nobody knows what that package *is* or whether they need it.

#### Encapsulation
- Assemblies provide visibility barriers
- Each sub-project can, and should be, tested fully by its own test suite and have a carefully designed public API.


### Subprojects may freely use other sub-projects
Compze.* packages freely depend on each other. We built these abstractions to help ourselves and others avoid having to implement these things over and over. What signal does it send if our own libraries avoid using the libraries we ourselves publish in favor of reimplementing the functionality themselves?

### Granularity serves discoverability and encapsulation, not total isolated independence
We are **not** optimizing for "can I use this on a desert island with no other Compze packages."

### Package names describe what they are, not where they live in a hierarchy
Drop the `Compze.Utilities.*` prefix for promoted sub-products. `Compze.Threading` says what it is. `Compze.Utilities.SystemCE.ThreadingCE` describes a path in a monolith. Package names should be self-explanatory — the internal organizational history is not the consumer's concern.

### Sub-products should be extractable
Each sub-product should have its own solution, production projects, and test projects. Unless doing cross-cutting refactoring these should be the best way to work on the sub-project.
Theoretically we should be able to easily split each sub-project into its own repository.

### FlexRef makes this practical
[Compze.Build.FlexRef](https://www.nuget.org/packages/Compze.Build.FlexRef) lets any `.slnx` include any subset of projects. Flex references become `ProjectReference` when the project is in the solution, `PackageReference` when it's not. This means:
- Each sub-product can have its own `.slnx` for focused work
- The monolithic `Compze.slnx` still exists for CI and cross-cutting refactors
- No duplication or special configuration per solution

### Versioning
SemVer. Dependencies use compatible version ranges — any semver-compatible newer version is acceptable. We do not require exact versions.

### Test/packable detection by naming convention
Rather than relying on directory hierarchy (`src/` vs `test/`) to determine `IsPackable` and `IsTestProject`, use a naming convention:
- Projects ending in `.Tests` or `.Specifications` → `IsTestProject=true`, `IsPackable=false`
- All other projects → packable by default

This decouples project classification from directory structure, which is important if sub-products eventually co-locate production and test projects.

---

## Decided

1. **Core dissolves.** Its types redistribute to Teventive (aggregate/event interfaces) and Typermedia (command/query routing). Core was born from a "minimize projects" strategy that FlexRef obsoletes.
2. **Tessaging becomes thin or disappears.** The Tessaging project either becomes a small project containing only shared interfaces/base classes, or is eliminated entirely. Teventive and Typermedia become top-level sub-products directly under Compze.
3. **Functional → Unit + Fluent.** `Compze.Functional` is renamed/split: `Compze.Unit` (the `unit` type + Action/Func converters) and `Compze.Fluent` (pipe, tap, mutate, then, forEach). This resolves a circular dependency between Fluent and SystemCE, since SystemCE can depend on Unit without pulling in Fluent's dependency on Unit.
4. **Naming**: Promoted sub-products use short names (`Compze.Threading`, not `Compze.Utilities.SystemCE.ThreadingCE`).

## Open Questions

1. **Test decomposition timeline**: Splitting the monolithic test projects is gradual, hard work. Each test needs to find its natural sub-product home.
2. **Directory restructuring**: The classification informs decisions; directory moves can happen incrementally as sub-products mature. Current `src/` + `test/` flat layout is kept for now.