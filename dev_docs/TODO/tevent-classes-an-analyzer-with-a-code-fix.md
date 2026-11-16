# Tevent classes: an analyzer with a code fix that adds only what is missing

Decided by Magnus, 2026-11-16: "I think that is the best available solution. Record it as an accepted todo. Something we
will do." Not yet started. When is not decided: proposed as an effort of its own, after the taggregate store.

The design, and the alternatives weighed on the way to it, are in
`dev_docs/taggregate-improvements/shared/generating-tevent-classes.md`.

## The problem

- **A tevent is an interface, and an implementation class that mirrors it mechanically,** nested as the interfaces are.
  JAStudio's notes, as taggregates, would have hundreds of them.
- **A hand-written class can be shaped so that a property silently doesn't survive being read back:** no private
  setter, and no constructor parameter matching it by name. Compze's Newtonsoft settings then leave the property at its
  default when a stored tevent or a message is read. Nothing checks for it today: `TessageTypeInspector`'s design rules
  cover interfaces and wrappers, not a class's properties.

## The plan

- **A Roslyn analyzer** that reports:
  - a tevent interface with no implementing class;
  - an implementing class missing members;
  - a property shaped so that it won't survive a round trip through the serializers.
- **A code fix that adds exactly what is missing,** from one tested template, and touches nothing else: not existing
  members, not attributes. Magnus asked for something "clever enough to be able to add only what is missing and not mess
  in any way with existing code, serialization attributes etc."
- **It never writes a type id.** Mappings stay hand-written in the assembly's `IAssemblyTypeMapper`, and
  `TaggregateTypeValidator` already reports a missing one at the taggregate's first use.
- **It matches a class to an interface by the class implementing it, not by name,** so renaming only the interface is
  harmless.
- **It runs inside the compiler,** at every build and as you type, and writes nothing on its own. Fixes are applied on
  demand: in Rider or Visual Studio, by an agent through the dotnet-analysis-and-refactoring MCP's quick fixes, or with
  `dotnet format analyzers` (believed, not checked).
- **In CI its diagnostics are errors.**

## What it needs that Compze doesn't have yet

- An analyzer project, and its packaging in NuGet: inside Compze.Teventive's package, or one of its own.
- FlexRef's handling of an analyzer reference within Compze's own build, where projects reference each other.

## Related

- **Decided by Magnus, 2026-11-16:** a serialized name is declared on the tevent interface's property, through a
  Compze attribute that each serializer plug honours, not on the classes.
- **Still open:** serialization samples, checked-in JSON of each tevent type as earlier versions wrote it, read back by a
  specification. The analyzer's round-trip diagnostic covers a class's shape; the samples cover renames.
