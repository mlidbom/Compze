# The design both teams agree on

**Status: agreed by all four sessions, then reviewed by two independent agents and revised with their agreement** (2026-11-16). Every name below is a placeholder for Magnus's pass
over the names. Where the teams first differed, the section says what was settled and why.

**The taggregate store has since replaced sections 2 and 3,** on Magnus's direction: restoring is from a snapshot type
of the implementor's, with no tevents, and the store's session dispatches when the unit of work commits, so neither the
hook nor its queue is needed. They are kept below as the record of what was weighed. Section 1, routing per class, stands
as it is, and section 4 is now needed. See [the-taggregate-store.md](the-taggregate-store.md).

## 1. Tevent routing decided once per class

**Tevent routing** means which tevent applier, or which tevent handler, a tevent reaches: in the taggregate itself, and
through its components, entity collections and shared-tomponent slots to the appliers inside them.

### Why

Everything a taggregate registers today is decided by its class, never by its instance: its own appliers and handlers,
its inherited appliers, each component's route on its parent, entity collections, slots, even the dispatcher
configurations. Yet each instance builds `IMutableTeventDispatcher`s for it, with locks, lists, closures over `this`, and
handlers resolved again by each instance for each tevent type it meets. That is where a taggregate's memory goes, and
it's the wrong abstraction: a dispatcher is made for subscriptions that come and go at run time, and an applier never
goes.

### The shape

```csharp
class VocabNote : JPNote
{
   static readonly TeventRouting<VocabNote, INoteTevent> VocabNoteAppliers = JPNoteAppliers.Extend<VocabNote>(it => it
     .For<IVocabTevent.TextUpdated>(static (vocab, tevent) => vocab._text = tevent.Text)
     .Component(static vocab => vocab.Forms)
     .SharedTomponent(SeeAlsoSlot));

   protected override TeventRouting<INoteTevent> TeventAppliers => VocabNoteAppliers;
}
```

- **A static table per class,** reached by the base class through a virtual property, as it already reaches
  `WrapperTeventImplementation`. An instance keeps nothing of it.
- **A tevent applier takes the instance as a parameter,** instead of capturing `this`. Declared `static`, the compiler
  forbids capture; the table also asserts, when declared, that no handler holds state.
- **A part is an entry in its parent's table:** `.Component(…)`, `.Entities(…)`, `.SharedTomponent(…)`. The part's tevent
  type is inferred from what the entry's lambda returns. A part no longer registers itself on its parent.
- **The table is typed on the class and on its tevent root,** so `For` keeps today's compile-time constraint to the
  class's own tevents, and keeps `TessageTypeInspector.AssertValidForSubscription`.
- **The verbs are what taggregates need:** `For`, `IgnoreUnhandled` and the parts, with the slot's `ForWrapped` kept
  internal. `IgnoreAllUnhandled` becomes `IgnoreUnhandled` of the root tevent. `BeforeHandlers`, `AfterHandlers` and
  `ForGenericTevent` stay on `IMutableTeventDispatcher` only: no taggregate needs them, so by YAGNI the new type doesn't
  carry them. That changes what a consumer's taggregates can do, so it's on Magnus's list.
- **The rules stay:** every matching entry is called in the order declared, and an unhandled tevent is an error unless
  declared otherwise. Matching is built on the dispatcher's own code and its one translation rule, not a second
  implementation of it: that is the first increment of the work. `IMutableTeventDispatcher` stays as it is, for
  subscriptions made at run time.
- **Inheritance:** a subclass's table starts from its base class's, through `Extend<TSubclass>`, and its entries are
  typed on the subclass. A subclass that adds nothing still declares its table, an empty `Extend`.
- **A mistake in a table is loud.** The independent review showed that a snapshot makes several mistakes silent: a
  snapshot tevent implements every property-updated tevent, so it always matches some route, and "an unhandled tevent
  is an error" never fires on a load. A forgotten `.Component(…)` then loads the note without the component's state,
  and the next save loses it. So:
  - **on dispatch, the table asserts that the instance's class is exactly the class it was declared for,** with the
    property abstract at the root, so a subclass that forgets its table fails at its first tevent;
  - **a class's first table comes only from a protected static factory on Compze's base class, typed on the class,** and
    a subclass's only from `Extend`, so a subclass can't skip its base class's appliers;
  - **a part asserts, when it's constructed, that its parent's table has an entry for its class,** and a shared
    tomponent that its slot is entered in its owner's table. The check is per class, not per instance, since the parent's
    field doesn't hold the part yet while the part's constructor runs;
  - **a slot's route asserts that the shared tomponent it reaches was constructed with that slot.**

  Two gaps are named, not solved: two components of one class with one entry forgotten, and `Extend` skipping an
  intermediate class that has a table.
- **Resolved once per class and wrapper tevent type,** published in a dictionary that is never changed afterwards and is
  read without a lock. Many threads dispatching on different instances of one class at once is safe, which JAStudio's
  full rebuild does.
- **Components typed on the class that declares them:** a component of a subclass, such as `VocabNote`, has that
  subclass as its parent, not the root that closes the `Taggregate` generic. That works today through the top-level
  `TeventiveComponent<…>`, and should be the normal path.
- **Tevent handlers stay on roots only.** JAStudio needs none in components: one part follows another through one tevent
  that both apply, and reactions that reach other notes go to subscribers on the corpus's dispatcher, after the cascade.
- **The query models** (`SelfGeneratingQueryModel` and its families) move to the same tables, so that one abstraction is
  implemented once.

### Shared tomponents and their slots

JAStudio has the same class of part several times in one note as the normal case: six word sets in a vocab, three
reading lists in a kanji, media reference lists in every note type. So a slot must cost an instance nothing for routing.

- **A slot is a static of the owner's class:** the adopting wrapper tevent's implementation, and the lambda that gets
  the shared tomponent from the owner. It is entered in the owner's table, and handed to the shared tomponent as a
  struct of owner and slot when the owner constructs it.
- **A shared tomponent keeps its owner and its slot.** There's no slot object per instance and nothing registered on the
  owner.
- **The slot is the one place that wraps** a tevent in its adopting wrapper tevent: when the tomponent publishes, and when
  the owner loads a snapshot.
- **A slot in a component is the same as a slot in a root.** It's JAStudio's usual case.
- **A slot must be declared above the table that enters it,** since static fields initialize in the order written. The
  compiler warns of it under nullable reference types, and the table asserts it.
- **What the real generics showed** (jastudio-rep-fable's static slots in the real base classes): a slot's owner can only
  be constrained to a class, since the table's instance type has no more, so the slot asserts when it is made that its
  owner publishes the owner's tevents. And a slot's declaration has five type arguments, which a slot type nested in the
  base class could cut to two. Both are details for the real design.

### What is built, what is designed, and what isn't yet

- **Built and measured** in the real base classes: the root, components, entity collections, slots per instance and
  slots of the class, inheritance through `Extend`, and the check that a handler holds no state.
- **Designed, not built:** the exact-class assertion and the other safeguards above, the table typed on the tevent root,
  and matching shared with the dispatcher. The prototypes keep a fallback to per-instance registration, and their
  `TeventRouting` has a second implementation of matching.
- **Not designed yet,** and needed before per-instance registration can be removed: removable entities, which throw under
  a declaring parent in the prototype; shared tentities and their collections; how Compze's own base classes, `Tentity`
  and the rest, add their appliers to a subclass's table; and the query models, which are a decision of their own.
- **The prototype routes tevent handlers into components,** which the design doesn't want: not to be carried over.

### Settled between the teams

- **How an instance reaches its class's table:** the fable team first proposed handing it through the base constructor,
  which can't be forgotten but costs a reference in every instance, about 8 B per teventive, or some 200 B for a vocab of
  about 25 teventives. A per-class registry was also considered, and rejected: a lookup at every construction, for an
  argument used once per class. Settled: the virtual property, with the exact-class assertion, which makes forgetting
  loud at no bytes per instance.
- **Slots:** the opus prototype kept a slot object per instance, about 48 B per slot. Settled: the fable team's static
  slot.

### Rejected

- **Routing by convention,** with `Apply(…)` and `Handle(…)` instance methods and parts found in fields by reflection. It
  was prototyped in full and costs the same: the same bytes and load time, plus about 0.4 ms of reflection per class,
  once. Its plain instance methods read best. Rejected for its mistakes: a misspelled or mis-shaped tevent handler is
  silently never called, since handlers may go unhandled; a field that refers to another taggregate's part would be
  routed to; and the order of several matching appliers rests on metadata order, which C# doesn't promise.
- **A third way, not prototyped:** instance methods marked `[TeventApplier]` and `[TeventHandler]`, found by reflection,
  with the parts still declared. It keeps plain instance methods, fails loudly on a wrong signature, and with
  `[MeansImplicitUse]` the IDE doesn't call the methods unused. But several matching appliers in one class would still
  run in metadata order, the convention's flaw, unless the attribute carries an order. For Magnus to weigh against the
  declared table.
- **Declaring the wrapper tevent implementation in the table:** which publisher a class is isn't tevent routing, and
  there are two tables. Today's virtual `WrapperTeventImplementation` stays, its default from the generic argument
  meaning a root needs nothing. One per-class declaration holding appliers, handlers and the wrapper is an option for
  the design.
- **Creating the per-instance dispatchers lazily:** loading runs the appliers, so every instance would create them anyway.
- **Recording a class's first instance's registrations and reusing them:** the closures capture that first instance.

## 2. Restoring a taggregate from its stored state

> **Superseded by the taggregate store's snapshots,** which carry no tevents: [the-taggregate-store.md](the-taggregate-store.md).

**Two ways, the consumer's choice.** Magnus has said, to JAStudio's representative, that a restore needn't go through
the tevent appliers: one that sets state directly, at about what loading costs today, is fine.

- **Setting each part's state directly,** as JAStudio's constructors do today. Compze then only needs a supported,
  protected way to give the restored taggregate its id and version: today that takes the `[Obsolete]` internal `Id`
  setter of `Entity`. It would also assert the invariants, and take part in the thread assertion and the refusal.
- **A snapshot, through the appliers,** described below, for a consumer that wants one code path for loading and
  editing. Designed and prototyped.

### A snapshot

**A snapshot** is the shortest list of tevents that gives a taggregate's state, each applied by the tevent appliers that
editing uses. It stands in for the history so far, as one step.

```csharp
///<summary>Restores this taggregate from a snapshot its own code has constructed from stored state.<br/>
/// Nothing is published, no tevent handler runs, and nothing is left uncommitted.</summary>
protected void LoadFromSnapshot(TaggregateId id, IEnumerable<TTaggregateTeventImplementation> snapshot)
```

- **Protected, and typed on the taggregate's class.** Its caller is the class's own factory, such as a static
  `Load(storedState)` beside its `Create`. Nothing is added to `ITaggregate`: Compze makes public only what a consumer
  needs, and the tevent store has no snapshots.
- **The taggregate's id is a parameter,** and no tevent of the snapshot is a creation tevent. Compze needs no marker
  interface: it would use one only to take the id from the first tevent and to assert its place. The load asserts that
  the snapshot isn't empty and that no tevent carries another id. `Publish` still requires `ITaggregateCreatedTevent`
  of a taggregate's first published tevent.
- **The whole snapshot is version 1,** with no version parameter. `Version` after a load is then 1 whatever the note's
  shape, and above 1 once it has changed. A version-taking overload, on `ITaggregate`, comes if the tevent store gains
  snapshots.
- **One tevent covers the taggregate and its components:** a component's tevents are subtypes of its owner's tevent
  root, so one snapshot tevent can implement every property-updated tevent of the taggregate and its components.
- **A shared tomponent gets a tevent of its own,** made by its slot, which wraps it in the slot's adopting wrapper tevent.
  One tevent can't carry the state of two shared tomponents of one class. A vocab in JAStudio loads from about eleven
  tevents.
- **An entity is created by its own created tevent,** since its collection creates one entity for each. JAStudio has no
  entities.
- **The thread assertion, below, covers loading too.**

**Settled between the teams, and then by the review:** whether the first tevent is a creation tevent, and whether the
snapshot is numbered 1..k or given one version. JAStudio's representative ruled out the creation tevent with two facts
of its own. A note is created in several ways, by a person, from the dictionary, by cloning or by an import, and each
creation tevent says what that creation set; one that must also carry the note's whole present state stops saying what
a creation is. And only a snapshot tevent must be complete, so only it can be checked against every property-updated
interface its class applies. The teams then chose a marker interface of Compze's. The independent review showed that
an id parameter meets both facts without a new public interface: the loaded tevents aren't creation tevents, JAStudio
may give them a marker of its own, and completeness can be checked because the load knows which tevents are the
snapshot. A snapshot that isn't a history beginning with a creation fits one version, 1.

**A gap a snapshot can leave silently:** a property-updated tevent added later, which the snapshot tevent doesn't
implement, leaves that property at its default on load, and the next save loses the data. A consumer's round-trip
specification (load, save, compare) catches it. With the per-class tables, Compze could check it once per class and tevent
type, at the first load, if property-updated tevents get a marker of their own.

## 3. Told once per publish cascade

> **Superseded by the taggregate store's deferred dispatch,** in which the store's session is told and dispatches at the
> commit: [the-taggregate-store.md](the-taggregate-store.md). What an exception leaves behind becomes a rollback of the
> unit of work, and the thread assertion gives way to the lock for each taggregate.

**A publish cascade** is one call to `Publish` together with every `Publish` that tevent handlers make from inside it.
`Taggregate` asserts the invariants at its end.

```csharp
///<summary>Called once at the end of each publish cascade, after the invariants are asserted,<br/>
/// with the cascade's tevents in the order published.</summary>
protected virtual void OnTeventsPublished(IReadOnlyList<TWrapperTeventInterface> publishedTevents)
```

- **The consumer's taggregate class overrides it,** once: JAStudio's `JPNote` refiles the note, commits, saves the note's
  file and dispatches the tevents on its corpus-wide dispatcher. No observer per instance, and no static state.
- **`Taggregate` feeds `TeventStream` itself, then calls the hook,** which is empty by default. An override that forgets
  its base call can then not silence the tevent store's session, which saves through `TeventStream`. The stream is
  created only when someone subscribes.
- **Published is not committed.** The hook says what was published; `Commit` is where the consumer takes what it hasn't
  saved. `Commit` may be called from inside the hook.
- **A queue keeps the order:** a publish from inside the hook, or from inside an observer, adds its cascade to the queue,
  and the loop that is already handing over delivers it when the call in progress returns. The hook is never called from
  inside itself, and every tevent is handed over in the order published. Handing over a nested cascade at once would
  deliver tevents out of order (published A, B, C; handed over A, C, B), and the tevent store's session, which saves each
  tevent as it's handed over, would save version 3 before version 2. **Settled between the teams:** the opus prototype
  first nested the call, and the fable team's queue replaced it.
- **`Commit` takes the list out before it calls back,** so a tevent published from inside the commit callback stays
  uncommitted for the next `Commit`. **`Commit` asserts it isn't called inside an unfinished cascade:** today a tevent
  handler that commits hands over tevents whose invariants then fail.
- **The two lists of tevents are created when first needed and handed on whole,** so a loaded taggregate that publishes
  nothing keeps neither.
- **One thread at a time:** `Publish`, `Commit` and the loads claim the taggregate for their thread with a compare-exchange,
  and throw when another thread enters meanwhile. Not thread affinity: a note loaded on one thread and edited later on
  another is fine. It catches only overlapping publishes, never a reader against a publisher. How far the claim reaches
  depends on decision 6, since the tevent store's session blocks on the bus while a handler may continue on a pool
  thread (`TeventStoreUpdater.OnTaggregateTevent`):
  - **If a store handler may not change the taggregate it handles** (recommended): the claim covers the whole of
    `Publish`, hand-over included, which is what JAStudio needs against its race between a background reparse and an
    edit in the UI, whose window is mostly the hand-over. The tevent store asserts against the re-entry itself, and the
    thread assertion's message names that case, since it trips first on another thread.
  - **If it may:** the claim is released around each call out to an observer or the hook, and held while the queue is
    taken from. JAStudio's race is then mostly uncaught.
  - **Keying the claim on the flow of execution was rejected:** a task started inside a publish carries the same flow,
    and could publish concurrently without being caught.
- **What an exception leaves behind: one rule.** An exception that escapes `Publish`, from a tevent applier, a tevent
  handler, the invariants, the hook or an observer, makes the taggregate refuse publishing and committing from then on,
  and so does a load that throws. No tevent is then dropped silently, and none is handed over twice. Precisely:
  - **the mark is set where the exception escapes,** at any nesting level, by a catch that rethrows or a finally on a
    success flag. A publish from inside the hook is an outermost publish, so a flag stretched over the hand-over would
    refuse it;
  - **checks that run before anything changes don't set it:** the thread claim, and the refusal itself;
  - **only what escapes counts:** a hook that catches inside itself sets the consumer's own policy for a failing
    subscriber;
  - **reads aren't refused,** though what they read may be half applied;
  - **for JAStudio,** every reference to a refused note, such as indexes and view models, is to be replaced by a
    reloaded note.

  Rejected: keeping the failed cascade queued for re-delivery, which hands the tevent again to observers that may have
  acted on it in part. Decision 5 is Magnus's.

## 4. `ITaggregateTevent` as a plain `ITevent`

> **Now needed, not only truthful:** Tessaging accepts only async handlers for an `IExactlyOnceTevent`, and the store's
> consumers, such as JAStudio, are synchronous.

`ITaggregateTevent : IExactlyOnceTevent` declares, for every taggregate, delivery that is remote, durable and
transactional. A tevent's type is to state its delivery contract (decision D6 of the routing work), and a note's tevents
never leave the process.

- **What the tevent store needs is identity, not exactly-once delivery:** it identifies every stored tevent by its
  `TessageId`, when reading tevents back, in migrations, and in its rows. So every stored hierarchy needs
  `ITessageWithIdentity`, whether or not it is delivered between processes.
- **This needs design before it's a decision,** in one of two variants:
  - **the interface only,** most simply `ITaggregateTevent : ITevent, ITessageWithIdentity`, which the tevent store needs
    nothing new for: truthful, and saving nothing measurable;
  - **the identity moved too,** to the tevents that need it: `TaggregateTevent` loses its `Id`, which touches every stored
    taggregate's tevent classes and the migrations' replacement tevents, and saves JAStudio a `TessageId` per tevent,
    about 18 MB of garbage per start.
- **Either way, a hierarchy that forgets its delivery declaration silently stops being delivered remotely:**
  `UnitOfWorkTeventPublisher.RemoteDeliveryFor` returns null for a tevent that declares no remotability, and nothing fails
  at compile time. A specification, or an assertion at setup, should catch it.
- **Nothing subscribes on a bus** to `ITaggregateTevent`, `ITaggregateCreatedTevent` or `ITaggregateDeletedTevent`
  themselves.

## Later

- **Loading without a wrapper object:** a tevent the class itself loads could be routed by the wrapper type it would
  have, with the wrapper made only where a `ForWrapped` entry receives one. It saves garbage at startup, not retained
  bytes: about 120 B per loaded tevent in the fable prototype, about 24 B per wrapper in the opus prototype. To decide
  when the performance specification measures a real start.
- **A source generator for tevent implementation classes,** which mirror the tevent interfaces mechanically: an effort
  of its own, after one note type's tevents are written by hand.
- **A subscription across every owner to a shared tomponent's inner tevents** needs no new routing: a subscription to a
  wrapper of a wrapper, `ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent>>>`, already matches
  every slot of every owner, checked against 0.6.0-alpha. At most a convenience method could come later.
