# Generating a tevent's implementation class from its interface

A finding, 2026-11-16. Nothing is built.

## The headline

**It fits Compze, and it is a proposal of its own: decide it after JAStudio has written one note type's tevents by
hand.** Nothing that JAStudio's notes need of `Taggregate` waits for it.

## What it is

Every tevent is an interface, which is what subscribers name, and a class that implements it, which is what gets
published. The class repeats the interface: a constructor parameter and a property for each property of each interface
it implements. JAStudio would need about a hundred such pairs. A Roslyn source generator could write the classes.

## Why it fits

Its output is types that exist when the code is compiled. The decision that "no runtime-generated tevent type may ever
be sent or persisted" (`src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md`, D4) was about
`WrapperTeventImplementationGenerator`, which emitted types while the program ran. A source generator doesn't touch it.

## Why it is a proposal of its own

- **Compze has no source generator and no analyzer today.** The first one brings its own work: how it is packed into a
  package, how FlexRef switches it between a project reference and a package reference, and how Rider and NCrunch run
  it.
- **What it must know isn't known yet:**
  - which interfaces get a class, and which only carry meaning for other interfaces to implement;
  - the `TypeId` of each generated class, for tevents that are stored or sent, which today is a mapping written by
    hand for each class;
  - how an entity's tevents get and set the entity's id, which today is a class written for each entity
    (`IGetSetTaggregateEntityTeventEntityId`);
  - the adopting wrapper tevents of shared tomponents.
- **A pair may cost less than it looks.** With a primary constructor a class is about two lines:

  ```csharp
  internal class Renamed(string name) : RootTevent.Component, IRootTevent.Component.Renamed
  {
     public string Name { get; } = name;
  }
  ```

## The number to watch: what a plain property costs

A text value in a note is one line in JAStudio today. As a tevent it is five things: the interface, the class, the
entry in the declaration of tevent appliers, the property, and the method that publishes when the value differs.
JAStudio has about 34 such values across its three note types. A generator of implementation classes writes one of the
five. The JAStudio session gives the real count when one note type is written.

## What I'd do

Write one note type's tevents by hand, ten to twenty pairs. That shows what a pair costs, and what a generator would
have to be told. Then decide.
