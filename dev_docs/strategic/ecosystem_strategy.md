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
We are **not** optimizing for "can I use this on a desert island with no other Compze packages.

### Sub-products should be extractable
Each sub-product should have its own solution, production projects, and test projects. Unless doing cross cutting refactoring these should be the best way to work on the sub-project 
Theoretically we should be able to easily split each sub-project into its own repository.

### FlexRef makes this practical
[Compze.Build.FlexRef](https://www.nuget.org/packages/Compze.Build.FlexRef) lets any `.slnx` include any subset of projects. Flex references become `ProjectReference` when the project is in the solution, `PackageReference` when it's not. This means:
- Each sub-product can have its own `.slnx` for focused work
- The monolithic `Compze.slnx` still exists for CI and cross-cutting refactors
- No duplication or special configuration per solution

---

## Open Questions

1. **Core's future**: Does `Compze.Core` survive, or do its concepts get redistributed to Tessaging and Sql? It was born from a "minimize projects" strategy that FlexRef obsoletes.
2. **Naming**: Should extracted sub-products keep the `Compze.Utilities.*` prefix or get shorter names? E.g., `Compze.InterProcessObject` vs `Compze.Utilities.SystemCE.ThreadingCE.InterProcessObject`.
3. **Test decomposition timeline**: Splitting the monolithic test projects is gradual, hard work. Each test needs to find its natural sub-product home.
4. **When to actually restructure directories**: This is a plan, not an immediate action. The classification informs decisions; the directory moves can happen incrementally as sub-products mature.