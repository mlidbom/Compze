# Tevent routing decided once per class, not set up per instance

Proposed 2026-11-16 and agreed by both sessions. Not started.

A coined term, used throughout: **tevent routing** means which applier, or which tevent handler, a tevent of a given
wrapper tevent type reaches, including the route through a component, an entity collection or a shared-tomponent slot
to the applier inside it.

## Why

### What it costs: measured with Compze.Teventive 0.6.0-alpha on .NET 11

Bytes retained per note, over 10,000 instances. Each component has 3 string properties and one applier per property:

| Components | Plain objects | Loaded taggregate | Added after one publish on each component |
|-----------:|--------------:|------------------:|------------------------------------------:|
|         15 |         733 B |          11,457 B |                                  +7,094 B |
|          6 |         303 B |           5,240 B |                                  +2,944 B |
|          1 |          47 B |           1,592 B |                                    +721 B |

For JAStudio's 80,000 notes, that's roughly 600 to 900 MB more than today's managed heap of about 733 MB.

### Where it goes

Each instance builds its own tevent routing, from scratch:

- **`Taggregate` creates two `IMutableTeventDispatcher`s,** one for appliers and one for tevent handlers.
  **`TeventiveComponent` creates one more,** and registers `For<TComponentTevent>(ApplyTevent)` on its parent's.
- **Each dispatcher has a `Subscriptions`** with three lists, and a `HashSet` built from its `TeventDispatcherConfig`.
- **Each `RegisterTeventAppliers()` call creates a `TeventSubscriber`** with a list of its own. Each `For<>` adds a
  `RegisteredHandler`, a delegate, and a closure over `this`.
- **Each dispatcher resolves its own handlers per wrapper tevent type** and keeps them in
  `_handlersByResolvedWrapperTeventType`. That's the growth after one publish on each component: each instance resolves
  them again on its first dispatch of each type.
- **Entity collections (`Tentity.CollectionManager`) and `SharedTomponentSlot`** register on their owner's dispatcher
  per instance too.
- **Every registration takes the dispatcher's lock.** About 80,000 notes times some 50 registrations is millions of
  `Monitor` acquisitions at startup. Not measured.
- **At dispatch, each component level wraps the tevent again:** `TeventiveComponent.ApplyTevent` calls
  `Dispatch(tevent)`, which allocates a new `PublisherTevent` wrapper. That's garbage per dispatch, not retained.

### Why it's the wrong abstraction, whatever it costs

Everything registered on these dispatchers is decided by the class, never by the instance: a taggregate's own
appliers and handlers, its inherited appliers, each component's route on its parent, entity collections,
shared-tomponent slots, even the `TeventDispatcherConfig`s (`TeventiveRemovableEntity` passes
`IgnoreUnhandled<TEntityRemovedTevent>()`). Yet each instance builds an `IMutableTeventDispatcher` to hold it: a
thread-safe dispatcher with a lock, made for subscriptions that come and go at run time. An applier never goes.

So tevent routing belongs to the class: declared once, resolved once per wrapper tevent type, and shared by every
instance. A loaded note then holds only its state, its parent references and its id.

## The change

### A table of appliers per class

The base class reaches it through a virtual property, as it already reaches `WrapperTeventImplementation`:

```csharp
class VocabNote : JPNote
{
   static readonly TeventAppliers<VocabNote> VocabNoteAppliers = JPNoteAppliers.Extend<VocabNote>(it => it
      .For<IVocabTevent.TextUpdated>(static (note, tevent) => note._text = tevent.Text)
      .Component(static note => note.Forms)
      .Entities(static note => note._senses));

   protected override TeventAppliers Appliers => VocabNoteAppliers;
}
```

The names are placeholders, for Magnus.

- **An applier gets the instance as a parameter instead of capturing `this`.** That's the main change for whoever
  writes a taggregate. A lambda declared `static` can't capture anything, which the compiler enforces. The table can
  also assert, when it's declared, that no handler holds state. A delegate whose target is `this`, or a closure over
  locals, has a target with instance fields; a lambda that captures nothing has a target with none.
- **Private members stay private:** a lambda declared inside the class can reach the private members of the instance
  it's given.
- **Inheritance:** `Extend<TSubclass>` adds a subclass's entries to its base class's table. Each level's lambdas are
  typed on their own class, so nothing needs a cast. Referring to the base class's static field makes its static
  initializer run first.
- **Components:** the parent declares the route, with `.Component(note => note.Forms)`, instead of each component
  registering itself on its parent. That's the other change for whoever writes a taggregate. Forgetting it fails
  loudly, as an unhandled tevent at the first dispatch, as it does today.
- **Entity collections and shared-tomponent slots** are declared the same way, on the parent. The per-instance parts
  that are state stay per instance: an entity collection's entities, and a slot's tomponent.
- **Tevent handlers** (today's `RegisterTeventHandlers()`) get a table of their own by the same mechanism, which lets
  every tevent go unhandled, as their dispatcher does today.

### New: tevent handlers in components

Today only `Taggregate` has `RegisterTeventHandlers()`: a component can register appliers, nothing else. That isn't
enough for JAStudio, whose reactions to a note's own changes live in its components. `PerfectSynonyms`, part of a
vocab's `RelatedVocab` component, pushes the vocab's answer to its synonyms, which are other notes, whenever the answer
shown changes. That's a tevent handler, not an applier.

So the tables of tevent handlers are composed through `.Component(…)`, `.Entities(…)` and the shared-tomponent slots,
the way the appliers' tables are. That's new behavior, not a move.

A tevent handler runs inside the publish cascade, before the invariants are asserted. One that publishes on other
taggregates changes them even if this taggregate's invariants then fail. A reaction on other taggregates can instead
go in the [per-cascade hook](3-told-once-per-publish-cascade.md), which runs after the assertion. That choice is
JAStudio's: Compze offers both.

### Resolving, and threads

- **A class's table resolves the handlers for each wrapper tevent type once,** and caches them. It does that the way
  `Subscriptions` does now: it resolves under a lock, publishes a copy of the resolved map, and reads that copy
  without a lock. Many threads dispatching on different instances of one class at once is therefore safe: JAStudio's
  full rebuild does that. An instance's own state stays one thread's at a time, as it is in that rebuild.
- **A resolved route into a nested component is composed once,** so a dispatch no longer wraps the tevent again at
  each component level.

### The matching rules are implemented once

The rules for which handler matches a wrapper tevent type are: subscriptions to inner and to wrapped tevent types,
handlers run before and after, and which tevents may go unhandled. They live in `Subscriptions` and `RegisteredHandler`
today. They move to where both the per-class tables and `IMutableTeventDispatcher` use them.
`IMutableTeventDispatcher` stays: it's public, and the right tool for subscriptions made at run time.

### `SelfGeneratingQueryModel` moves too

Compze.Teventive.TeventStore's `SelfGeneratingQueryModel` and its components copy the same per-instance model. They
move too, or Compze has two implementations of one abstraction. JAStudio doesn't need this part.

### Components typed on the class that declares them

`Component<…>`, `Entity<…>` and `RemovableEntity<…>` are nested in `Taggregate<TTaggregate, …>`, so their parent type
is `TTaggregate`: the class that closes the generic. In an inheritance hierarchy that's the root. A `VocabNote`
component declared as `VocabNote.Component<…>` would get a `JPNote` parent, and need a second, typed reference or a
cast. Compze's `AnimalTaggregate`/`CatTaggregate` specifications don't show it, because the cat has no components or
tevents of its own.

**JAStudio can do this today:** derive from the top-level classes, typed on the subclass. Checked with 0.6.0-alpha, by
compiling and running [this check](checks-against-0.6.0-alpha/README.md). Its applier reads a member only `VocabNote`
has, through a typed field, without a cast:

```csharp
class FormsComponent : TeventiveComponent<VocabNote, INoteTevent, NoteTevent, FormsComponent, IVocabTevent.Forms, VocabTevent.Forms>
```

`Tentity<…>` and `TeventiveRemovableEntity<…>` are top-level classes too, so they work the same way.

**In the redesign, typing on the declaring class should be the normal path,** not the long way around. The declaration
side already is: `.Component(static note => note.Forms)` on `VocabNote`'s table is typed on `VocabNote`. How the
component base classes get there with fewer generic arguments is a detail to settle when implementing.

### What's left per instance

The taggregate's `Id`, its `Version`, two lists (the uncommitted tevents, and the tevents of the current publish
cascade) and `TeventStream`, which [becomes lazy](3-told-once-per-publish-cascade.md). Each component holds a parent
reference. The estimate is about 200 B per note, plus 8 B per component, over plain objects, against 11 KB plus 7 KB
today. [The benchmark](benchmark/README.md) should confirm it once the change exists.

The redesign adds a specification of the bytes a taggregate retains per instance, with a bound, both loaded and after a
publish on each component. A regression then shows in Compze's own test run, not in JAStudio's heap. Bytes retained
after full collections are a measure that other work on the machine can't move, unlike times.

## Rejected

- **Creating the per-instance dispatchers lazily.** Restoring a note runs its appliers, so every instance would create
  them at startup anyway.
- **Recording the constructor's registrations on a class's first instance, and reusing them for the rest.** The
  closures capture that first instance, and rebinding delegates to other instances is fragile magic. Registrations that
  depend on constructor arguments would silently go wrong.
- **Appliers found by convention:** instance methods such as `void Apply(IVocabTevent.TextUpdated tevent)`, turned
  into open-instance delegates once per class. They're the nicest to write, since `this` is there as usual and there
  are no lambdas. But it's reflection magic, and Compze registers explicitly everywhere else. Both sessions recommend
  the explicit table. **This is the one taste call for Magnus.**

## What it touches

- **Compze.Teventive:** `Taggregate`, `TeventiveComponent`, `Tentity` and its `CollectionManager`,
  `TeventiveRemovableEntity`, `SharedTomponent`, `SharedTentity`, `SharedTentityCollection`, `SharedTomponentSlot`,
  `ITeventiveInternals`, and the dispatcher's matching rules.
- **Compze.Teventive.TeventStore:** `SelfGeneratingQueryModel` and its nested classes.
- **Everyone who writes a taggregate:** 56 `RegisterTeventAppliers()`/`RegisterTeventHandlers()` calls in 41 files.
  13 of those files are Compze's own base classes and query models, and the rest are tests and the AccountManagement
  sample. Mechanical. Compze is greenfield, so there's no compatibility to keep.
- **Unchanged:** the tevent store. It uses only `ITaggregate.LoadFromHistory`, `Commit` and `TeventStream`.
