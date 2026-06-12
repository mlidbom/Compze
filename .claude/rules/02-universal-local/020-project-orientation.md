# Project orientation

> **Greenfield — there is no production.** Zero deployed applications, no persisted data, and **no
> backward-compatibility constraints**. Any format, identifier, schema, or behavior may change freely —
> optimize for the best long-term design, never hedge for migration safety. (Code must still build and the
> full test suite must still pass.)

## What Compze is

A .NET framework for building expressive domains through:

- **Teventive programming**: Type-routed events (also called "Semantic Events") that leverage .NET type
  compatibility for elegant event modeling. Events use interface inheritance for type-based routing —
  e.g. `IUserImported : IUserRegistered : IUserEvent : IAggregateEvent` — and subscribers receive every
  event compatible with their subscribed type through the type hierarchy.
- **Typermedia APIs**: Type-based message routing that extends hypermedia principles with .NET types.

## Tech stack

- **Language**: C# (.NET 10, see `src/global.json`)
- **Testing**: xUnit v3 (via `Compze.xUnit`, `Compze.xUnitBDD`, `Compze.xUnitMatrix`)
- **Build System**: MSBuild (.NET SDK), solution file: `src/Compze.AllProjects.slnx`
- **References**: FlexRef — auto-switches between `ProjectReference` and `PackageReference` depending on which projects are in the current solution
- **Dependency Injection**: Pluggable (Microsoft DI, Autofac)
- **Persistence**: Pluggable (SQLite in-memory, SQL Server, PostgreSQL, MySQL)
- **Serialization**: Pluggable (Newtonsoft)
- **Transport**: Pluggable (AspNetCore)
- **Documentation**: DocFX (site in `src/Websites/Website/`)
- **Development Tools**: PowerShell module (`DevScripts/Compze.psm1`)

## Documentation

- Docs are co-located: they live in `_docs/` folders next to the code they document. See
  `src/Documentation-CoLocation.README.md`.
- Claude config: shared standards arrive via the `.claude-shared/` subtree, selected per project by
  symlinks under `.claude/` — start at [.claude/rules/README.txt](../README.txt).
