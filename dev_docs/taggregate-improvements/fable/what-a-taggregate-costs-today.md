# What a Taggregate costs today, and why

Findings from reading Compze.Teventive on `main` at `a6317da47` (0.6.0-alpha), 2026-11-16. The measured numbers are the
JAStudio session's, from 80,000 instances built against the package from nuget.org. The byte counts by hand are mine,
for x64, and rough.

## The headline

**How a tevent is routed depends only on the class of the teventive and the type of the tevent, and it is worked out
again for every instance.** A teventive is a taggregate, a component, an entity or a shared tomponent. Each one creates
a tevent dispatcher of its own, registers its tevent appliers on it as closures over itself, and resolves, the first
time it sees each tevent type, which of them match.

| | Kept | Allocated | Time |
|---|---|---|---|
| The same data as plain objects | 350 B | | |
| A Taggregate with 4 tevent appliers, loaded from one creation tevent | 1,632 B | 2,864 B | 3 µs |
| The same with 4 components of 2 tevent appliers each | 5,629 B | 8,504 B | 11 µs |
| The first tevent of a new type published on an instance | 528 B more | | |

For JAStudio's 80,000 notes, with 8 to 20 parts each, that is 0.5 to 1 GB on a heap of 733 MB.

## What one instance allocates

**A root with 4 tevent appliers,** before it has seen a tevent (`Taggregate..cs:29-45`,
`CallMatchingHandlersInRegistrationOrderTeventDispatcher.Subscriptions.cs:24-39`):

- two dispatchers, one for tevent appliers and one for tevent handlers, each with its `Subscriptions`: about 180 B;
- three lists of subscriptions for each dispatcher, and the backing array of the one that has entries: about 250 B;
- a `HashSet<Type>` for each dispatcher, of the tevent types that may go unhandled, built with LINQ from the
  `TeventDispatcherConfig` even when that is empty: about 130 B;
- the `TeventSubscriber` that `RegisterTeventAppliers()` returns, with its list: about 130 B;
- for each tevent applier a `RegisteredHandler` and the delegate: 88 B, so about 350 B for four;
- `_unCommittedTevents`, the list of the tevents of the current call to `Publish`, and the `SimpleObservable` of the
  tevent stream: about 90 B.

That is about 1.1 KB before the object's own fields. Loading one tevent adds the resolved handlers of its type: a
`Dictionary`, an array, and a closure for each tevent applier that matches, about 0.5 KB when all four match.

**A component with 2 tevent appliers** allocates the same again, less the handlers' dispatcher and the lists, and adds
its subscription on the parent: a `TeventSubscriber` with its list, a `RegisteredHandler` and a delegate on the parent's
dispatcher (`TeventiveComponent.cs:34-45`). About 0.9 KB, and about 0.4 KB more when it has seen its first tevent.

**A shared tomponent** has a dispatcher of its own as a component has, and its `SharedTomponentSlot` is one object
more, with a closure registered on the owner (`SharedTomponentSlot.cs:42-51`). JAStudio's vocab would have about ten.

**The first tevent of a new type** copies the dictionary of resolved handlers with one more entry, on the appliers'
dispatcher and on the handlers', and adds an array and the closures: `Subscriptions.PublishResolvedHandlers`.

A `TeventDispatcherConfig` given to the base constructor, such as
`Account() : base(TeventDispatcherConfig.Default.IgnoreUnhandled<...>())`, is also built again for every instance.

## Compze pays it too, on every load

The tevent store's session creates a taggregate and loads its history each time one is fetched, in every unit of work
(`TeventStoreUpdater.DoTryGet`, `LoadSpecificVersionInternal`). The 2.9 KB and 3 µs are paid there before the first
tevent of the history is applied. The dispatcher's own exception document says it:
`dev_docs/code_standard_exceptions/raw-monitor-in-the-tevent-dispatcher.md` gives "every taggregate creates dispatchers
of its own each time it is loaded" as the reason the dispatcher is Compze's hottest path, and measures 400 ns and
1,168 B for creating one and registering five handlers.

## What each published tevent allocates

Apart from the tevent and its wrapper, which are what is published:

- **`ScopedChange.Enter` twice,** in `Publish` and in `ApplyTevent` (`Taggregate..cs:53,94`): each time two delegates
  and a `Disposable`, about 150 B.
- **A new wrapper at each component level:** `TeventiveComponent.ApplyTevent` calls the dispatcher's overload for a
  tevent without a wrapper, which wraps it in a `PublisherTevent<>` through `Constructor.ForGenericType`, two
  dictionary lookups and a compiled constructor. An entity's collection does the same for the entity.
- **A `TessageId` and two readings of the clock:** `TaggregateTevent`'s field initializers make a version 7 Guid and
  read the time, and `Publish` reads the time again and overwrites it.

## Nothing needs registration for each instance

What the registration through the instance is used for today, each of which is the same for every instance of a class:

- **Every call to `RegisterTeventAppliers()` and `RegisterTeventHandlers()` that I read is an unconditional statement
  in a constructor.** There are 56 calls in 41 files. I read the base classes and about ten of the others, not all.
- **`TeventDispatcherConfig`** is always a constant of the class, handed through the instance's constructor.
- **`registerTeventAppliers: false`** is passed only by `Tentity`, and by the entities of the query models: an entity
  is reached through its collection, by its id, so it doesn't subscribe to its parent itself.
- **`WrapperTeventImplementation`** is a virtual property that a subclass overrides: the class's.
- **Two shared tomponents of the same class in one owner,** such as a shipping and a billing address, are told apart
  by their `SharedTomponentSlot`'s `TAdoptingWrapperTevent`, a type argument.
- **Disposing an `ITeventSubscriber` to unsubscribe** is used by no teventive that I read. It is for applications that
  subscribe to a dispatcher of their own.

## The order that is load-bearing

The rule is that all matching handlers are called in the order they were registered. Where the code depends on it:

- **An entity's collection** registers three handlers on the parent, in this order: create the entity and add it,
  route the tevent to the entity by its id, and, for a removable entity, remove it (`TeventiveEntity.cs:70-83`,
  `TeventiveRemovableEntity.cs:38-47`). So an entity exists when its creation tevent is routed to it, and has applied
  its removal tevent when it is removed.
- **A base class's tevent appliers run before a subclass's,** because its constructor runs first. `Tentity` registers
  the applier that sets the entity's id before the subclass registers anything.
- **Every tevent applier in the whole tree runs before any tevent handler** (`Taggregate..cs:70-73`).
- **Between a teventive's own tevent appliers and its children,** the order is the order of the constructor's
  statements. In the classes I read the children come first and nothing depends on it. A tevent applier of a root that
  derives state from a component that the same tevent has just changed would depend on it.

## Smaller findings

- **Publishing from an observer of the tevent stream breaks the notification.** `Publish` notifies the observers in a
  `foreach` over the list of the call's tevents, at reentrancy level 0 (`Taggregate..cs:76-81`). An observer that
  publishes on the same taggregate from `OnNext` adds to that list. The inner `Publish`, at level 0 itself, notifies
  the whole list again, the outer call's tevents included, and clears it; then the outer `foreach` throws
  `InvalidOperationException`, after the state has changed. Found by the other session working on this question, whose
  `//bug:` comment is in `Taggregate..cs`; I confirmed it from the code and ran nothing. What the fix must keep is in
  [a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md).
- **`ITaggregate.Commit` loses a tevent published from inside its callback.** It hands the callback the live list of
  uncommitted tevents and clears the list afterwards (`Taggregate..cs:125-129`). A tevent published on the same
  taggregate while the callback runs is added to the list the callback is reading, and cleared with the rest: never
  committed, and nothing says so. Found by the JAStudio session; I confirmed it from the code.
- **An exception from a tevent applier or a tevent handler leaves a taggregate that looks usable.** The tevent may be
  half applied, the invariants were never asserted, and the tevent stays among the uncommitted tevents and is notified
  with the next publish (`Taggregate..cs:53-74`). A tevent store's unit of work throws the instance away. A consumer
  whose taggregates live on, as JAStudio's notes do, gets no sign.
- **Two threads publishing on one taggregate corrupt it quietly:** the reentrancy level and the flag that tevents are
  being applied are plain fields.
- **Only a root can have tevent handlers:** `RegisterTeventHandlers()` is on `Taggregate`, not on
  `TeventiveComponent`. A component can't react to a tevent by publishing another. JAStudio's notes don't need it.
- **The tree of tevent appliers is implemented twice.** `SelfGeneratingQueryModel` and its `Component`, `Entity` and
  `NestedEntity` families, in Compze.Teventive.TeventStore, repeat `Taggregate`, `TeventiveComponent` and `Tentity`
  with dispatchers of their own and the same `registerTeventAppliers` flag.
- **`TaggregateTypeValidator` scans whole assemblies from static constructors.** It runs once for each closed
  `Taggregate`, `TeventiveComponent`, `Tentity` and `TeventiveRemovableEntity`, so three times for a removable entity's
  class, and each run goes through all types of an assembly three times. That is start-up time, which nobody has
  measured. The JAStudio session doesn't expect it to matter there: its assembly has on the order of a thousand types.
  The validator also throws on any public setter or public field in the taggregate's and the tevents' types. JAStudio's
  notes and their parts have none.
