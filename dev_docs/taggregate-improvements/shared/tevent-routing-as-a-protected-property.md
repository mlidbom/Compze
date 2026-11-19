# Tevent routing handed over through a protected property, not an explicit ITeventRouted

> Some names and API here were changed by later decisions of the end review (2026-11-18): see
> [the-naming-pass.md](the-naming-pass.md) and [reviews/end-review-consolidated.md](reviews/end-review-consolidated.md).

**Decided by Magnus, 2026-11-18** (the end review's ninth question, item 74). Put to him: item 24 had chosen an explicit
implementation of the public interface `ITeventRouted`, listed again in the base list of every class that declares a
routing, without weighing that CA1033 fires on consumers' classes too, nor the alternatives. Recommended: a protected
abstract property on Compze's base classes. He asked whether its downside was only that it shows among the class's
protected members, and was told the others: CA2214 in Compze's base constructors, which read it; a subclass that forgets
its own still failing at its first construction; the parts' typed parents, internal work; JAStudio's 17 classes changing
a line each. Then: "Does this effect client projects' ability to build their own extensions to taggregate/teventive base
classes? If not the upsides seem to win to me too." It doesn't: a consumer extends Compze's base classes by deriving
from them, and derives the same way after the change.

**This supersedes item 24's choice 1,** and what [the README](README.md) (step 3's progress) and
[step-5-the-rest-of-the-teventives.md](step-5-the-rest-of-the-teventives.md) say of the explicit `ITeventRouted`. The
rest of the routing, the tables, `Extend`, the slots and the assertions at construction, stands.

## What a consumer writes

Before:

```csharp
class VocabNote : JPNote, ITeventRouted
{
   static readonly TeventRouting<VocabNote, INoteTevent> Appliers = JPNote.Appliers.Extend<VocabNote>(it => it...);
   TeventRouting ITeventRouted.TeventAppliers => Appliers;
}
```

After:

```csharp
class VocabNote : JPNote
{
   static readonly TeventRouting<VocabNote, INoteTevent> Appliers = JPNote.Appliers.Extend<VocabNote>(it => it...);
   protected override TeventRouting TeventAppliers => Appliers;
}
```

- **Every teventive class gives its routing by overriding `TeventAppliers`:** a taggregate, a component, an entity, a
  shared tomponent, a shared tentity, a query model and its parts. A taggregate class that declares tevent handlers also
  overrides `TeventHandlers`, which returns none unless overridden.
- **A class that derives directly from a Compze base class and overrides nothing doesn't compile:** the property is
  abstract there. A subclass that forgets its own override inherits its base class's, and is refused at its first
  construction, as before, by the message naming both classes and what to write.
- **Nothing is public that exists only for Compze to read:** `ITeventRouted` is gone. No CA1033 in a consumer's classes.
- **The query models' parts take a typed parent:** a component's constructor, and `CreateSelfManagingCollection`, take
  `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.IParent`, which the query model and its components and
  entities are. A consumer passes `this`, as before, and never implements it.

## How it's built

- `TeventApplyingRoot` and `TeventApplyingComponent` declare `protected abstract TeventRouting TeventAppliers { get; }`;
  so do `SharedTentity`, and `SharedTomponent`, whose property is nullable: null only in Compze's own
  `SharedTentityCollection`, which routes its tentities' tevents in code of its own. `Taggregate` declares
  `protected virtual TeventRouting? TeventHandlers => null`. `Taggregate` becomes abstract: it was a class only its
  subclasses construct.
- The base constructors assert, as before, that the routing is declared for the instance's exact class, reading the
  property: a call of an overridable member from a constructor. CA2214 turned out not to report a property's read, so
  nothing is suppressed for it; ReSharper's inspection of a virtual member called in a constructor does, and is
  suppressed at each, with the reason: the routing is a table in a static field, and the assertion must run at
  construction.
- `TeventRouting`'s internal assertions take the routing and the instance, instead of an `ITeventRouted`.
- A part asserts its parent's routing routes to it, reading the parent's routing through `ITeventiveInternals` for a
  taggregate's parts, and through the query model's `IParent` for a query model's: each with a member for Compze's use
  only, marked as `PublishInternal` is.

## Specifications

The existing routing specifications keep specifying the behaviour, each class now overriding `TeventAppliers`:

1. A taggregate, its components and entities, shared tomponents and tentities, and query models and their parts apply
   their tevents through the routing their class declares.
2. A class that inherits its base class's routing without declaring its own is refused at its first construction, with
   the message naming both classes and the override to write.
3. A class whose override returns no routing is refused at its first construction.
4. A part whose parent's routing doesn't route its tevents to a part is refused at its construction.
5. A subclass's routing extends its base class's, and its tevent handlers run after its appliers.
