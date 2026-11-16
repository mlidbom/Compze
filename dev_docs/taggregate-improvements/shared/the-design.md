# The design both teams agree on

**Status: agreed by all four sessions, who have stopped** (2026-11-16). Every name below is a placeholder for Magnus's pass
over the names. Where the teams first differed, the section says what was settled and why.

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
   static readonly TeventRouting<VocabNote> VocabNoteAppliers = JPNoteAppliers.Extend<VocabNote>(it => it
     .For<IVocabTevent.TextUpdated>(static (vocab, tevent) => vocab._text = tevent.Text)
     .Component(static vocab => vocab.Forms)
     .SharedTomponent(SeeAlsoSlot));

   protected override TeventRouting TeventAppliers => VocabNoteAppliers;
}
```

- **A static table per class,** reached by the base class through a virtual property, as it already reaches
  `WrapperTeventImplementation`. An instance keeps nothing of it.
- **A tevent applier takes the instance as a parameter,** instead of capturing `this`. Declared `static`, the compiler
  forbids capture; the table also asserts, when declared, that no handler holds state.
- **A part is an entry in its parent's table:** `.Component(…)`, `.Entities(…)`, `.SharedTomponent(…)`. The part's tevent
  type is inferred from what the entry's lambda returns. A part no longer registers itself on its parent.
- **Today's verbs and rules stay:** `For`, `ForWrapped`, `ForGenericTevent`, `BeforeHandlers`, `AfterHandlers`,
  `IgnoreUnhandled`, every matching entry called in the order declared, an unhandled tevent an error unless declared
  otherwise. The matching rules are implemented once, and shared with `IMutableTeventDispatcher`, which stays as it is
  for subscriptions made at run time.
- **Inheritance:** a subclass's table starts from its base class's, through `Extend<TSubclass>`, and its entries are
  typed on the subclass. A subclass that adds nothing still declares its table, an empty `Extend`.
- **Forgetting a subclass's table fails loudly:** on dispatch, the table asserts that the instance's class is exactly
  the class it was declared for. Without that assertion one case would be silent: a subclass that adds an applier for a
  tevent its base class already applies, and forgets its table, would lose that applier with no unhandled tevent.
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
  `[MeansImplicitUse]` the IDE doesn't call the methods unused. For Magnus to weigh against the declared table.
- **Creating the per-instance dispatchers lazily:** loading runs the appliers, so every instance would create them anyway.
- **Recording a class's first instance's registrations and reusing them:** the closures capture that first instance.

## 2. Loading a taggregate from a snapshot

**A snapshot** is the shortest list of tevents that gives a taggregate's state, each applied by the tevent appliers that
editing uses. It stands in for the history so far, as one step.

```csharp
///<summary>Restores this taggregate from a snapshot its own code has constructed from stored state.<br/>
/// Nothing is published, no tevent handler runs, and nothing is left uncommitted.</summary>
protected void LoadFromSnapshot(IEnumerable<TTaggregateTeventImplementation> snapshot)
```

- **Protected, and typed on the taggregate's class.** Its caller is the class's own factory, such as a static
  `Load(storedState)` beside its `Create`. Nothing is added to `ITaggregate`: Compze makes public only what a consumer
  needs, and the tevent store has no snapshots.
- **The first tevent carries a snapshot marker of its own,** `ITaggregateSnapshotTevent`, and no later tevent does. It
  establishes the taggregate's identity, as a creation tevent does. `Publish` still requires `ITaggregateCreatedTevent`
  of a taggregate's first published tevent, so a snapshot tevent can't be published as a creation.
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

**Settled between the teams:** whether the first tevent is a creation tevent or carries a marker of its own, and whether
the snapshot is numbered 1..k or given one version. JAStudio's representative decided the marker with two facts of its
own. A note is created in several ways, by a person, from the dictionary, by cloning or by an import, and each creation
tevent says what that creation set; one that must also carry the note's whole present state stops saying what a creation
is. And only a snapshot tevent must be complete, so only it can be checked against every property-updated interface its
class applies. With the marker, a snapshot isn't a history that starts with a creation, so one version, 1, fits it.

**A gap a snapshot can leave silently:** a property-updated tevent added later, which the snapshot tevent doesn't
implement, leaves that property at its default on load, and the next save loses the data. A consumer's round-trip
specification (load, save, compare) catches it. With the marker and the per-class tables, Compze could check it once per
class, if property-updated tevents get a marker of their own.

## 3. Told once per publish cascade

**A publish cascade** is one call to `Publish` together with every `Publish` that tevent handlers make from inside it.
`Taggregate` asserts the invariants at its end.

```csharp
///<summary>Called once at the end of each publish cascade, after the invariants are asserted,<br/>
/// with the cascade's tevents in the order published.</summary>
protected virtual void OnTeventsPublished(IReadOnlyList<TWrapperTeventInterface> publishedTevents)
```

- **The consumer's taggregate class overrides it,** once: JAStudio's `JPNote` refiles the note, commits, saves the note's
  file and dispatches the tevents on its corpus-wide dispatcher. No observer per instance, and no static state.
- **Its base implementation feeds `TeventStream`,** which is created only when someone subscribes.
- **Published is not committed.** The hook says what was published; `Commit` is where the consumer takes what it hasn't
  saved. `Commit` may be called from inside the hook.
- **A queue keeps the order:** a publish from inside the hook, or from inside an observer, adds its cascade to the queue,
  and the loop that is already handing over delivers it when the call in progress returns. The hook is never called from
  inside itself, and every tevent is handed over in the order published. Handing over a nested cascade at once would
  deliver tevents out of order (published A, B, C; handed over A, C, B), and the tevent store's session, which saves each
  tevent as it's handed over, would save version 3 before version 2. **Settled between the teams:** the opus prototype
  first nested the call, and the fable team's queue replaced it.
- **`Commit` takes the list out before it calls back,** so a tevent published from inside the commit callback stays
  uncommitted for the next `Commit`.
- **The two lists of tevents are created when first needed and handed on whole,** so a loaded taggregate that publishes
  nothing keeps neither.
- **One thread at a time:** `Publish`, `Commit` and the loads record the thread in their outermost call with a
  compare-exchange, and throw when another thread enters meanwhile. Not thread affinity: a note loaded on one thread and
  edited later on another is fine. It turns a silent corruption into a loud failure, at the cost of a field and one
  interlocked operation per publish cascade.
- **What an exception leaves behind:** a hook or an observer that throws leaves the taggregate usable, with the tevents
  not yet handed over still queued, in order, for the next publish. A tevent applier, a tevent handler or the invariants
  that throw leave state that can't be trusted: the proposal, for Magnus to decide, is that the taggregate then refuses
  publishing and committing. The combined prototype does that.

## 4. `ITaggregateTevent` as a plain `ITevent`

`ITaggregateTevent : IExactlyOnceTevent` declares, for every taggregate, delivery that is remote, durable and
transactional. A tevent's type is to state its delivery contract (decision D6 of the routing work), and a note's tevents
never leave the process.

- **The change:** `ITaggregateTevent : ITevent`. A taggregate delivered exactly-once says so at its root:
  `interface IAccountTevent : ITaggregateTevent, IExactlyOnceTevent`.
- **What it touches:** the tevent store reads each tevent's `TessageId`, so it must ask for one; about five of the 24
  taggregate hierarchies in the tests and the sample are delivered between endpoints and need the one-line declaration;
  nothing subscribes on a bus to `ITaggregateTevent`, `ITaggregateCreatedTevent` or `ITaggregateDeletedTevent` themselves.
- **What it saves a note:** a `TessageId` per tevent, 32 B and a Guid's creation, and a contract nothing honours.

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
