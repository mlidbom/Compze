# A Taggregate without a tevent store

Findings and proposals, 2026-11-16. Nothing is built in Compze. What sections 1 and 2 propose is built in a prototype,
and its behaviour run: see [what-was-run-and-measured.md](what-was-run-and-measured.md). The names in the code are
placeholders for Magnus's pass over the names.

## The headline

**`Taggregate` fits a consumer that stores state instead of history, apart from three things.** JAStudio would use
`Publish`, the tevent appliers, the invariants, and `ITaggregate.Commit` as what its `Flush` is today: the uncommitted
tevents are what isn't saved yet. What stands in the way:

1. Only `Publish` and the tevent store can give a tevent its version, so nobody else can make a history that loads.
2. The only way to hear that a taggregate has published is an observer on each instance, and neither that
   notification nor `Commit` stands a publish from inside it.
3. `ITaggregateTevent` declares the delivery of a tevent that is stored and sent to other processes.

## 1. Loading from a compacted-history

A compacted-history is a taggregate's history rewritten to the fewest tevents that give the same state. A note's file
holds the note's state; read back as tevents, that state is the note's compacted-history.

- **No new kind of tevent is needed.** "One tevent that is every property update at once" is what a creation tevent
  is in Compze's own examples: `IUserRegistered` implements the property-updated interfaces of everything a
  registration sets. Loaded and changed state then go through the same tevent appliers.
- **Compze already holds that a rewritten history is the truth:** `LoadFromHistory` applies "the stored wrapper … after
  a migration has rewritten history the stored wrapper is the truth" (`Taggregate..cs:134`), and the migrations
  replace and insert tevents.
- **It is a list, not one tevent.** A component's state fits in the root's creation tevent. A shared tomponent's
  doesn't: each is loaded by a tevent adopted through its own slot, since one tevent can't carry the state of two
  shared tomponents of one class. An entity in a collection is created by a creation tevent of its own. A vocab in
  JAStudio, with about ten slots, loads from about eleven tevents.
- **A slot adopts a tevent for loading as it does for publishing.** The root's code asks each slot for the adopted
  tevent, and constructs no adopting wrapper tevent itself.
- **What is missing:** a tevent constructed outside `Publish` has `TaggregateVersion` 0. `ApplyTevent` then sets the
  taggregate's `Version` to 0, and the next `Publish` refuses its tevent as not being a creation tevent. The setter is
  `IMutableTaggregateTevent.SetTaggregateVersionInternal`, marked for internal use.

The addition, a protected member of `Taggregate`:

```csharp
///<summary>Rebuilds this taggregate's state from a compacted-history that its own code has constructed:
/// each tevent is given this taggregate's id and the next version, and applied as if Publish had wrapped it.
/// Nothing is published, and nothing is left uncommitted.</summary>
protected void LoadFromCompactedHistory(IEnumerable<TTaggregateTeventImplementation> compactedHistory)
```

A taggregate's own factory calls it, such as a static `Load(storedState)` beside its `Create`. Each part's class
gives its share of the compacted-history from its stored data, and the root joins them.

**Loading is how JAStudio starts:** about 570,000 tevents for its 79,720 notes, each adopted one in two wrappers. What
a loaded tevent costs, in bytes and time, is to be measured with what an instance costs.

**A loaded tevent needs no wrapper object** (the JAStudio session, from its prototype). Which tevent appliers a tevent
reaches is decided by the type of its wrapper, and for a tevent the class itself publishes or loads, that type follows
from the class and the tevent's own type. So a class resolves it once for each tevent type, and the wrapper object is
made only for what receives it: a `ForWrapped` entry, and whoever the published tevent is committed and handed to. The
JAStudio session's prototype loads an adopted tevent that way in 232 bytes and 0.2 µs, where 0.6.0-alpha takes
1,220 bytes and 1.2 µs. A history from the tevent store comes with its wrappers, and is routed by them, as today.

- **Is such a tevent a tevent, when nothing happened?** Yes when it is named for what did happen: the note was
  created, or imported, with these values. No when it is named `Restored` or `State`. It reaches only tevent appliers,
  so no subscriber sees it either way.
- **Does the tevent store want snapshots of this kind?** It has no snapshots today, so I can't say that it does. If it
  gets them, a compacted-history is the form that needs no second way of setting state beside the tevent appliers.

## 2. Hearing that a taggregate has published

`Publish` notifies the observers of `ITaggregate.TeventStream` when its outermost call completes: the tevent and every
tevent that the tevent handlers published in response have been applied, and the invariants hold
(`Taggregate..cs:76-81`). The tevent store's session subscribes to each taggregate it tracks. For 80,000 notes that is
80,000 observers.

- **A protected virtual method called at that place** lets the taggregate's own class act instead, at no cost for an
  instance. `AssertInvariantsAreMet()`, on the line before, is such a method already.

  ```csharp
  ///<summary>Called when a call to Publish has completed: its tevent, and every tevent the tevent handlers
  /// published in response, are applied, and the invariants hold.</summary>
  protected virtual void OnTeventsPublished(IReadOnlyList<TWrapperTeventInterface> publishedTevents) {}
  ```

  JAStudio's notes would override it once, in `JPNote`: tell the collection that the note has uncommitted tevents.
  The collection then commits: it saves the note's file, and dispatches each tevent, in its wrapper, on its
  `IMutableTeventDispatcher`. By default at once, and at the end of a command that makes several changes.
- **Published is not committed.** The hook says that tevents were published. `Commit` is where the consumer takes
  what it hasn't saved.
- **Create on first use:** `_unCommittedTevents`, the list of the current call's tevents, and the `SimpleObservable`
  of the tevent stream. A loaded taggregate that publishes nothing then has none of them.

### Two places that don't stand a publish from inside them

JAStudio's subscribers will publish from inside both: a subscriber to a note's tevent may change that same note.

- **The notification of the tevent stream.** An observer that publishes on the same taggregate from `OnNext` makes the
  inner `Publish` notify the outer call's tevents a second time, after which the outer `foreach` throws. An observer
  that publishes every time it is notified of the tevent it reacts to never gets that far: each inner `Publish`
  notifies it of the same tevent again, and the process dies of a stack overflow. The hook would be called at the same
  place and have the same bug.
- **`Commit`.** It hands the callback the live list and clears it afterwards (`Taggregate..cs:125-129`). A tevent
  published on the same taggregate from inside the callback is cleared with the rest, uncommitted.

What the fixes must keep:

- **Notify in the order published.** Handing the observers a copy and clearing the list first stops the exception and
  the repeats, but a publish from inside a notification is then notified before the rest of the outer call's tevents:
  published as A, B, C, and notified as A, C, B. The tevent store's session saves each tevent as it is notified
  (`TeventStoreUpdater.OnTaggregateTevent`), so it would save version 3 before version 2. A queue keeps the order: a
  publish from inside a notification adds its tevents to the queue, and the loop that is already notifying delivers
  them after those published before. The hook is then never called from inside itself.
- **`Commit` takes the list out before it calls back,** so a tevent published from inside the callback is uncommitted
  for the next `Commit`.

### What an exception leaves behind

JAStudio's subscribers assert contracts, so they throw, and a note lives on after the exception. A taggregate from a
tevent store is thrown away with its unit of work.

- **From the hook, or from an observer of the tevent stream:** the taggregate's state is whole, since the tevent
  appliers have run and the invariants hold. What the queue and the assertion remember, that a notification is in
  progress and which thread is in `Publish`, must be reset on the way out. Otherwise every later publish is queued
  and never handed over. The tevents not yet handed over stay queued, and are handed over, in order, before those of
  the next publish. All of them are still uncommitted, so a consumer that commits loses none. A specification pins it.
- **From a tevent applier or a tevent handler: the taggregate isn't to be trusted afterwards.** The tevent may be
  half applied, or applied with the reaction of its tevent handler missing, and the invariants were never asserted.
  That is so today (`Taggregate..cs:53-74`): the tevent stays in the list of uncommitted tevents, and is notified
  with the next publish. A consumer whose taggregates live on has to load such a taggregate again, which JAStudio can
  do from the note's file. Compze could say so itself: a taggregate that an exception has left `Publish` from the
  tevent appliers, the tevent handlers or the invariants refuses every later use, with a message that says why.

### One thread at a time in `Publish`, asserted

The reentrancy level and the flag that tevents are being applied are plain fields: two threads publishing on one
taggregate corrupt it, and nothing says so. JAStudio has such a race today, between a background reparse and an edit
in the UI. An assertion makes it fail at its cause: `Publish` remembers the thread that is in its outermost call, and
throws when another thread enters meanwhile.

- **Not thread affinity:** a unit of work moves between threads, which is why the tevent store's session asserts one
  transaction, not one thread (`SingleTransactionUsageGuard`). Only two threads in `Publish` at the same time is wrong.
- **It catches a publish against a publish.** A thread that reads a taggregate while another publishes on it is still
  the consumer's to prevent.
- **The cost:** an integer in each instance, and one interlocked operation for each outermost call.

## 3. What `ITaggregateTevent` says about delivery

`ITaggregateTevent : IExactlyOnceTevent`, so every tevent of every taggregate is remotable, durable and transactional.
The delivery model's one rule is that a tevent's type states its delivery contract, on two axes it keeps apart
(`src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`). Here the base interface states it for every taggregate: a
taggregate whose tevents never leave the process can't be written, nor one whose tevents are delivered best-effort.
A note's tevents would declare a contract that nothing honors.

**The change:** `ITaggregateTevent : ITevent`. A taggregate whose tevents are to be delivered exactly-once says so at
the root of its tevents: `interface IAccountTevent : ITaggregateTevent, IExactlyOnceTevent`.

What I found by reading:

- **Publishing needs no change.** `UnitOfWorkTeventPublisher.RemoteDeliveryFor` picks the delivery by the tevent's
  type: `IPublisherTevent<IExactlyOnceTevent>`, then `IPublisherTevent<IRemotableTevent>`, otherwise delivery in the
  process only. That last is what the model says a plain `ITevent` gets. The outbox takes
  `IPublisherTevent<IExactlyOnceTevent>` and never sees another kind.
- **The tevent store reads the tevent's `TessageId`** for its `TeventId` column (`TeventDataRow`, and
  `TeventStore.HydrateTevent`). `ITaggregateTevent` has `Id` only through `IExactlyOnceTevent`. The store has to ask
  for it of the tevents it stores.
- **`TaggregateTevent` makes a `TessageId` for every tevent,** a version 7 Guid in an object. With this change that
  belongs to the tevents that declare an identity (`ITessageWithIdentity`), and a note's tevents would make none. Where
  the stored tevents then get theirs, such as a base class of their own, is part of this change's design.
- **Subscriptions change tier.** Whether a subscription demands exactly-once delivery is read from the subscribed type
  (`TessageBusHandlerRegistrar.SubscriptionDemandsExactlyOnceDelivery`, and the advertisement in
  `TessageHandlerRoster`). Every subscription to a taggregate's tevents demands it today, through the base interface.
  After the change, one whose hierarchy doesn't declare it fails its assert in `RegisterExactlyOnceTeventHandlers`, at
  setup. Each taggregate hierarchy in the tests and the sample that is delivered between endpoints needs the
  declaration: mechanical. By a search of them: 24 hierarchies have `ITaggregateTevent` as their root, none says
  `IExactlyOnceTevent` itself, and five endpoint declarations register exactly-once tevent handlers, three in the
  tests and two in the sample, for about five of the hierarchies. The rest are published in one process, or only
  dispatched, and need nothing.
- **A subscription to `ITaggregateTevent`, `ITaggregateCreatedTevent` or `ITaggregateDeletedTevent` itself** would no
  longer demand exactly-once delivery. Nothing in `src`, the tests or the sample subscribes to them on a bus: only
  `SingleTaggregateQueryModelGenerator` does, on a dispatcher of its own, which is unaffected.

It is a change to a public interface: Magnus's to decide. I recommend it whatever JAStudio does.

## What JAStudio's subscribers can subscribe to

Two questions of the JAStudio session's, answered from the code. Neither needs a change.

- **A subscription to an owner's tevent interface that a slot's adopting wrapper tevent implements** matches the
  tevents adopted through that slot, and receives the adopting wrapper tevent. The dispatcher matches an inner tevent
  type against the outermost wrapper by covariance (`RegisteredHandler<THandledTevent>.TryCreateHandlerFor`), and the
  adopting wrapper tevent is what the outermost wrapper wraps. No specification pins it: the shared tomponent
  specifications cover the full publication path, the slot whatever the owner's wrapper, and that the tomponent's own
  tevent type doesn't match. One should be added.
  **It says the same of every tevent through that slot:** the adopting wrapper tevent's type belongs to the slot, not
  to the tevent inside it. To tell the tevents of one slot apart, subscribe to the publication path with
  `ForWrapped`.
- **A generic, covariant tevent interface that isn't a wrapper,** such as `IMediaReferencesTevent<out TReference>`, is
  sound for the dispatcher: `TessageTypeInspector` has no rule against it, and matching is assignability, covariance
  included. Covariance holds for reference types only: with a struct as `TReference`, a subscription to the interface
  of a base type would match nothing, without an error. A tevent store or a bus would also need a `TypeId` for each
  closed type, which JAStudio doesn't use.

## Considered, and not recommended: a second root base class

A base class with `Publish`, tevent appliers and handlers and nothing of what the tevent store needs, with
`Taggregate` built on it. Not recommended:

- **JAStudio uses most of what would be left out:** `Commit` and the uncommitted tevents themselves, the rule that the
  first tevent creates, and loading from a history.
- **What it doesn't use is on the tevents,** not on `Taggregate`: the delivery contract and the `TessageId`, which
  section 3 takes away, and the version and time stamp, which cost a field each.
- **`TeventiveComponent`, `Tentity` and `SharedTomponentSlot` are constrained to `ITaggregateTevent` and
  `TaggregateTevent`.** A second root would need all of them rooted lower, for nothing that sections 1 to 3 don't give.
