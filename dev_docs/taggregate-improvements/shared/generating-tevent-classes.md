# Generating tevent implementation classes

**Status: exploration, 2026-11-16.** What jastudio-rep-opus found in answering Magnus's questions about README decision
12, a generator for tevent implementation classes. Nothing is decided or built. Both teams proposed it as an effort of
its own, after JAStudio has written one note type's tevents by hand.

## The headline

**If a generator is built, it should be a build step that writes ordinary source files, which are checked in, and it
should never write a type id.** The tevent classes are then ordinary code to every tool, refactoring works as it does
today, every change in what is generated is a reviewed diff, and type ids stay what they are today: GUIDs declared by
hand.

## What it generates

A tevent is an interface, which subscribers subscribe to, and an implementation class, which mirrors it mechanically,
nested as the interfaces are:

```csharp
interface IStoredVocabMeaningChanged : IStoredVocabTevent
{
   string Meaning { get; }
}

class StoredVocabMeaningChanged(string meaning) : StoredVocabTevent, IStoredVocabMeaningChanged
{
   public string Meaning { get; private set; } = meaning;
}
```

The class says nothing the interface doesn't, so a generator can write it, and each hierarchy's wrapper tevent classes.
JAStudio's notes would have hundreds of tevent types.

## Two kinds of generator

| | A Roslyn source generator | A build step writing checked-in files (recommended) |
|---|---|---|
| What it is | A plug-in to the compiler that adds source to the compilation, in memory | An MSBuild target before compilation that runs a program shipped in the package and writes `.cs` files into the project |
| When it runs | At every compilation, and continuously in the IDE as you type | At every build |
| Its output | In memory. Files written to disk are a record only, excluded from compilation | Ordinary source files, checked in and compiled as any other |
| Renaming a tevent interface with a refactoring tool | The generated class follows, but hand-written references to it, such as `new AnimalTevent.Born(id)` and its type mapping, don't, and break the build | The tool renames interface, class and every reference together; the next build regenerates the same thing |
| Costs | Code must refer only to interfaces to stay refactorable | Classes stale until the next build; the generator builds its own semantic model of the project; a build that writes into the source tree; more work |

**Why the build step:** it keeps everything ordinary code, so refactoring works exactly as it does today, and the
classes in git are exactly the classes that run. Renaming only the interface, and not the class, breaks the build
loudly: the next build regenerates the class under the new name, and references to the old name no longer compile.

**What a project does to use it:** a package reference with `PrivateAssets="all"`, and optionally a property naming the
folder. The package's `build/` targets file runs the generator, out of process, since loading Roslyn into MSBuild's
process conflicts with the Roslyn MSBuild holds. CI builds in a checking mode that fails when a regenerated file differs
from the committed one. Nothing to install: `dotnet build`, Rider and Visual Studio all run it.

## Type ids: the generator never writes one

- **Type ids are GUIDs declared by hand** in each assembly's one `IAssemblyTypeMapper`, `map.Map<AnimalTevent.Born>(…)`.
  Storage and the wire carry only the GUID, which is why a rename by refactoring keeps old data and other systems
  working today.
- **With the build step, nothing about that changes:** the mapping refers to the class as ordinary code, and a
  refactoring renames it with the class.
- **`TaggregateTypeValidator` already asserts** that a taggregate's tevent interfaces and implementation classes are all
  mapped, at the taggregate's first use, so a missing mapping fails then, not at the first write.
- **So the generator has no need to write ids.** Two ways it could were considered:
  - **Deriving a class's id from its interface's GUID.** Not needed, so not proposed. Were it used, the function would
    become a stored and wire format that must never change. Magnus's bar: "It must NEVER EVER happen. If there is any
    risk that we cannot guarantee that FOREVER we cannot use this idea." A specification pinning known inputs to known
    outputs would hold it as firmly as Compze already holds its other stored formats, such as a mapped type's
    `"GUID, 0"` and the ids composed for generic types.
  - **A random GUID assigned once and kept in the checked-in file.** Rejected: renaming only the interface would look
    to the generator like one class deleted and another added, and the "new" class would silently get a new identity.

## The emitted shape is a stored format

Compze's Newtonsoft settings read stored tevents through private setters, non-public default constructors, and the
parameters of a primary constructor matched to properties by name. A change in what the generator emits, such as a
setter's shape or a parameter's name, can leave properties at their defaults when old tevents are read, with no error.
The guards:

- **The checked-in output:** every change in what is generated is a diff someone reviews.
- **A round trip of every generated shape** through each pluggable serializer, asserting that every property survives.
- **Serialization samples:** see the next section.

## Renaming a tevent's property

**This is a silent loss of data today, with or without a generator.** A refactoring that renames a stored tevent's
property changes its name in the JSON. Tevents stored before, and messages from peers that still have the old name,
carry the old name. Newtonsoft ignores a member it doesn't know, and the renamed property is left at its default. The
fix today is Newtonsoft's `[JsonProperty("OldName")]` on the class, which no tevent in Compze uses, and nothing reminds
anyone to add it.

**A generated class can't carry the attribute,** since the next build overwrites it. So the serialized name belongs on
the interface, the generator's input. What we'd propose:

- **A Compze attribute on the interface's property** that fixes the property's serialized name, such as
  `[SerializedAs("Question")]`. It fixes the name for writing as well as reading, so that peers that still have the old
  name keep understanding new messages.
- **Serializer-neutral:** a Newtonsoft attribute would put one pluggable serializer's type into the domain's public
  tevent interfaces. Each of Compze's serializer plugs honours Compze's attribute. Newtonsoft's does through the
  contract resolver's modifiers it already has, reading the attribute from the interface the class implements.
- **It works for hand-written classes and generated ones alike,** so the generator needn't know about it.
- **The guard against forgetting it:** serialization samples, checked-in JSON of each tevent type as earlier versions
  wrote it, which a specification reads back and compares. A rename without the attribute fails it.

## Adopting it over existing tevents

Since the generator never writes ids, the mapper keeps every existing class's GUID, and stored tevents and peers resolve
as before. The emitted shape must match what the hand-written classes stored, which the serialization samples check.

## Effort

Roughly two to three weeks, and uncertain. Compze has no generator or analyzer project today, so besides the generator,
it needs: reading the project's source with Roslyn's libraries, incremental runs, deleting stale files, the checking
mode for CI, packaging, and FlexRef's handling of the generator within Compze's own build.

## Open

- Whether to build it: README decision 12.
- Whether Compze adopts the attribute for serialized names, and serialization samples, regardless of a generator.
- Not verified: how Rider and ReSharper treat a rename that involves source-generated code.
