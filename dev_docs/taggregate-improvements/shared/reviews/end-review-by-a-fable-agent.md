# End review of the taggregate-store spike, by a Fable agent

> Written at the start of the end review (2026-11-18), on the spike at 46cc881a6: some names and API here have changed
> since, by its decisions; see [../the-naming-pass.md](../the-naming-pass.md) and
> [end-review-consolidated.md](end-review-consolidated.md).

**What was reviewed:** `taggregate-store-spike` at 46cc881a6 against `main`, every public type and member in
`public-api-changes-of-the-spike.md`, the stores' private code behind them, the specifications, the sample, and
JAStudio's use of each on its `main` (ff8f09aad). Sections 1 to 5 and 7 were written before I read the builders' column
of the README's table, `bugs-and-findings.md` or anything in `reviews/`. I have not read the Opus reviewer's file.

**How each finding was reached** is marked: **(run)** means a specification I wrote and ran shows it, **(read)** means
I judged it from the code. The specifications are 29, all passing while their findings stand, in
`test/Compze.Tests.Integration/CQRS/StateStore/Findings_of_the_end_review_by_a_fable_agent.cs` on the branch
`end-review-fable` (79fc29ab7, pushed), run on the two SqliteMemory combinations. I ran nothing else of the suite.

Paths are relative to `C:\Dev\Compze` unless they begin with `JAStudio`, which is `C:\Dev\JAStudio\src\src_dotnet`.

## 1. The headline

1. **The state store is an all-in-memory store by construction, and that shaped the interfaces it shares with the tevent
   store.** It is a store of its own, not a mode of something more general, and its name, its documentation and the
   shared interfaces' documentation should say so. Today `ITaggregateStoreReader`'s summary promises "no lock and no
   copy", and `ITaggregateReference.Current` is a property, where the tevent store answers both by reading a history and
   building a new instance at every call. Section 3.

2. **Three defects in what the public API promises, each shown by running:**
   - **Get-or-create waits 30 seconds for its own lock, then fails.** `TryGetForUpdate` of an id the store doesn't hold
     keeps the id's lock, as documented, "so no other unit of work can add it before this one completes". The same unit
     of work can't add it either: `Save` takes that lock again, and so does a second `TryGetForUpdate`. Both end in a
     `TimeoutException` that blames another unit of work. JAStudio's one call of `TryGetForUpdate` reads a false as "a
     note being created, not yet added", which is this pattern as soon as such a note is added after it.
   - **A new taggregate saved again after its unit of work rolled back is committed with no tevent dispatched.** No
     participation handler runs and no observer ever hears of its creation. The tevent store refuses such a save.
   - **Two references to one taggregate are unequal when asked for as two classes of its hierarchy,** though
     `ITaggregateReference` is covariant and their hash codes are equal. A set of references then holds one taggregate
     twice.

3. **Ten members I would take out of the public surface or move.** Each is either unused by its only consumer, the same
   as something already there, or a specification facility in a production package:
   - make private: `Taggregate.AssertCanBeChanged()` (JAStudio no longer calls it), and `Taggregate`'s constructor,
     which the spike made public by accident;
   - remove: `ITaggregateIndex.HasAny` (it is `Lookup(key).Count > 0`, at the same cost),
     `ITaggregateReference.ForUpdate()` (a second door to `GetForUpdate`, which JAStudio never uses),
     `TaggregateFiles.Of`'s `fileOf` (JAStudio passes a copy of the default), and `snapshotsAreEqual` (the storage can
     tell what changed, and JAStudio's comparison is "serialize both and compare the bytes");
   - move to a testing package, as Compze.Tessaging.Hosting.Testing is for endpoints:
     `ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()`, `TaggregateFiles.KeptInMemory()` and
     `ITaggregateFilesInMemory`;
   - move to the composition's builder: `AtRuntime<TTevent>()`, which the runtime registrar also has, and always
     throws from.

4. **`ITeventRouted` is the shape chosen to cost an instance nothing, and every consumer pays for it in other ways:** a
   class that forgets its routing fails when first constructed instead of when compiled, every class lists the
   interface again in its base list, and CA1033 fires on every public unsealed class. Handing the routing to the base
   constructor can't be forgotten and needs no interface, for 8 bytes an instance. `Taggregate` allocates about 120 bytes
   an instance that a committed instance never uses, so the bytes are there to take back. Section 2, under
   `ITeventRouted`.

5. **The state store takes part in exactly-once delivery, in both directions, and can't keep the promise.** In an
   exactly-once endpoint, the outbox row of a tevent its taggregate publishes, and the record that a tessage was
   handled, commit with the database; the taggregate's file is written by a volatile participant after that. A crash
   between the two delivers a tevent for a change the store then discards, or marks a tessage handled whose change is
   lost. The store should refuse what it can detect, and say the rest, until this is designed. (read) Section 4.4.

6. **What the store does when it fails or waits is not in its public documentation:** it stops the process
   (`Environment.FailFast`) when a commit's file operations fail, including a file another program holds for more than
   two seconds; it doesn't flush to the disk; getting for update, starting a unit of work during a reload, and a reload
   waiting for units of work each give up after a fixed 30 seconds; it loads everything at its first use, on whatever
   thread and inside whatever transaction that is.

7. **A taggregate's version restarts at 1 at every load,** so two of one taggregate's tevents carry the same
   `TaggregateVersion`. `RestoreIdentity(id)` gives a consumer no way to keep it. (run) That was right for JAStudio's
   diffs, and is a silent change of what `ITaggregate.Version` means for anyone else.

8. **An index is known by its taggregate class and key type alone, which nothing says or asserts.** JAStudio has ten
   wrapper types for it, for its vocab alone. A second index of one class and key type fails with the container's
   message, and an index of a base class beside one of a subclass can't be resolved at all. (run)

9. **Most of the spike is good API that Compze would have for any consumer,** and I would keep it as it is: the reader
   and updater split, the bulk reader as a reader of its own, deletion as a tevent, bulk reads that give references in
   no order, indexes defined by state, the snapshot round trip that compares instances, routing declared once per class
   and typed on the class and its tevent root, observers of a unit of work's tevents, observers created at runtime, the
   storage seam kept internal, and the MemoryPack cache as a package that plugs into an internal seam.

## 2. Every public API change

Verdicts: **keep**, **keep with another shape**, **rename**, **make internal** (or private), **remove**, **move**.
Names are in section 5; a verdict of "keep" says nothing about the name.

### Compze.Teventive, namespace `Compze.Teventive.TaggregateStores` (new)

- **`ITaggregateStoreReader`** (`src/Compze.Teventive/TaggregateStores/ITaggregateStoreReader.cs:10`): **keep.** Any
  consumer needs it, and it is minimal.
  - `Get`, `TryGet`: **keep.** Two things its documentation doesn't say truly:
    - `TryGet` "gives false if no store holds the taggregate". Asked for a taggregate as another class of the same
      store, it throws (run). JAStudio works around it: it gets as `JPNote` and casts
      (`JAStudio.Core/Note/Collection/StoredNotes.cs:40`). Throwing is defensible, since asking for a vocab's id as a
      sentence is nearly always a bug. Say so in the summary.
    - The interface's summary says that outside a unit of work a read "costs no lock and no copy". That is the state
      store. The tevent store reads the history and builds a new instance at each call
      (`src/Compze.Teventive.TeventStore/_private/TaggregateTeventStore.cs:40`).
  - `Reference`: **keep.** It makes a reference to any id, held or not (run); say so.
- **`ITaggregateStoreUpdater`** (`ITaggregateStoreUpdater.cs:18`): **keep.**
  - `GetForUpdate`: **keep.** Its 30-second wait and `TimeoutException` belong in its documentation.
  - `TryGetForUpdate`: **keep, and fix.** Holding the lock of an id the store doesn't hold is a good guarantee: it is
    what makes get-or-create safe. As built, the unit of work that holds it can't use it (run): the session takes the
    lock again at `Save` (`src/Compze.Teventive.StateStore/_private/StateStoreSession.cs:89`) and at a second try
    (`:70`), and the lock is a `SemaphoreSlim` its holder can't enter twice
    (`_private/TaggregateUpdateLocks.cs:29`). The fix is for a session to take each id's lock once.
  - `Save<TTaggregate>(TTaggregate)`: **rename to `Add`, and drop the type parameter.** It only adds: the builders say
    so in a `review-naming:` at it, and JAStudio's wrapper is already called `Add`
    (`JAStudio.Core/Note/Collection/NoteUnitOfWork.cs:35`). The type parameter is unused: the updater routes by
    `taggregate.GetType()`. And it must refuse an instance whose tevents an earlier unit of work took (run): see 4.4.
  - **Its lifetime is scoped in name only** (read). `TaggregateStoreUpdater` holds three singletons and finds the unit
    of work from the ambient transaction at each call (`_private/TaggregateStoreUpdater.cs:37`), so one resolved in a
    unit of work works in any later one. Registering it scoped (`_internal/TaggregateStoresRegistrar.cs:25`) only keeps
    it from being resolved from the root, while `ITaggregateReference.ForUpdate()` gives the same access from anywhere.
    Two shapes would be honest: a singleton that says it needs an ambient unit of work, as the reader is; or scoped,
    with `ForUpdate()` gone from the reference, so that a unit of work's scope really is the only way in. I would
    remove `ForUpdate()` either way: one way to get for update.
- **`ITaggregateStoreBulkReader`** (`ITaggregateStoreBulkReader.cs:7`): **keep**, with `All` as it is: a reader of its
  own, references, a collection with no order. It follows `IDocumentDbBulkReader`, and a consumer can't build it.
  JAStudio turns every reference into its instance at once (`StoredNotes.cs:37`), which is its own choice and costs it
  nothing it wouldn't pay anyway.
- **`ITaggregateReference<out TTaggregate>`** (`ITaggregateReference.cs:9`): **keep with another shape.** A typed id
  that resolves itself is a real abstraction, and what bulk reads should return: an index that held instances would
  rewrite a key's array at every commit of any taggregate filed under it. But:
  - **Equality is not what the type promises** (run). `TaggregateReference<TTaggregate>.Equals` requires the other to
    be the same closed generic (`_internal/TaggregateReference.cs:25`), so a reference asked for as `ITaggregate` and
    one asked for as `StoredVocab` are unequal with equal hash codes. The state store then also makes a new reference
    at each request for the second (`TaggregateStateStore.cs:71`), although its comment says "a reference is compared
    by id". Compare by store and id, whatever the class asked for, and say in the interface's documentation that
    references are equal when they stand for one taggregate.
  - **`ForUpdate()`: remove.** It is `updater.GetForUpdate<TTaggregate>(reference.Id)`. It was asked for so that no
    session is passed down to call sites; JAStudio then wrote a reference class of its own, because it wants the note
    got for update in its own unit of work (`JAStudio.Core/Note/NoteReference.cs:13`), and calls Compze's
    `ForUpdate()` nowhere. Without it a reference only reads, and code that changes taggregates depends on the
    updater, which says so. Cost to JAStudio: none in its code, one member fewer to implement.
  - **`Current`: keep, with its cost and its failure said.** It is a property that throws once the taggregate is
    deleted, with no way to ask first, and for the tevent store it is a database read and a replay at each read. Its
    documentation tells a view to "keep the last instance it got", and the only exception a consumer can catch is
    `ArgumentOutOfRangeException`, which is too wide to catch safely. Point to `ITaggregateStoreReader.TryGet` as the
    read that doesn't throw, or make `Current` a pair of methods like the reader's. Magnus's call.
  - **A consumer implements it** (JAStudio's `NoteReference<TNote>`), which is a sign: what JAStudio wanted was
    `note.Reference()`, from an instance. Compze is right not to offer that. It does mean the interface is a contract
    others implement, so every member added to it breaks them.
  - **The state store keeps every reference it ever made** (`TaggregateStateStore.cs:22`), also to ids it doesn't hold
    and to deleted taggregates (run). Forget a reference when its taggregate's deletion commits, and don't keep one for
    an id the store doesn't hold.

### Compze.Teventive, namespace `Compze.Teventive` (tevent routing)

- **`TeventRouting` and `TeventRouting<TInstance, TTevent>`** (`src/Compze.Teventive/TeventRouting.cs:28`, `:74`):
  **keep.** Routing decided once per class is a better abstraction than a dispatcher per instance for any consumer: it
  costs an instance nothing and a load no garbage. Classes are right here although the strategy prefers interfaces:
  they are closed types a consumer never constructs or implements.
  - `Extend<TSubclass>` (`:104`): **keep.** Asserting the direct base class is what makes a chain impossible to skip.
  - A first routing only from the base classes' protected `DeclareAppliers` and `DeclareHandlers`: **keep.**
- **`TeventRouting<TInstance, TTevent>.Declaration`** (`:143`): **keep**, with `For`, `IgnoreUnhandled`, `Component`,
  `Entities`, `SharedTomponent`.
  - `Component` and `Entities` are one implementation under two names (`Member`, `:189`). Fine: they read as what is
    declared. `Component`'s summary says it routes to the component's "appliers, or tevent handlers", which the code
    refuses (`:207`). Correct the sentence.
  - Without `BeforeHandlers`, `AfterHandlers` and `ForGenericTevent`: **agreed.** Nothing calls them on a taggregate.
- **`ITeventRouted`** (`src/Compze.Teventive/ITeventRouted.cs:12`): **keep with another shape**, Magnus's call, since he
  decided the present one (item 24). (read) What the explicit re-implementation costs a consumer:
  - a class that forgets it compiles, and fails at its first construction;
  - every class that declares a routing lists `ITeventRouted` again in its base list, a rule of C# few know;
  - CA1033 on every public unsealed class, which a consumer following Compze's own warnings policy must suppress, 17
    times in JAStudio by the builders' count;
  - a public interface, with two members anyone can call, that exists only for Compze's base classes to read.

  The alternatives, and what each costs:

  | How the base class gets the class's routing | A forgotten routing | Bytes an instance | A consumer class writes |
  |---|---|---|---|
  | Explicit `ITeventRouted` (built) | fails at first construction | 0 | the interface in the base list, and an explicit member |
  | A protected abstract property | doesn't compile, at the root | 0 | an override, visible among the class's protected members |
  | An argument of the base constructor | doesn't compile | 8 for each teventive | `: base(Appliers)` |

  The constructor argument is the plainest C#, and the only one that can't be forgotten in a subclass either. Its cost
  was put at about 200 bytes a vocab. Against that: every `Taggregate` allocates a list, a queue and an observable when
  constructed (`Taggregate..cs:43`, `:58`, `:177`), about 120 bytes by my arithmetic, not measured, which an instance
  served to readers never uses, and which the-design.md said would be "created when first needed". Creating them at the
  first publish takes back most of what the argument costs. I can't tell from the documents whether the constructor
  argument was put to Magnus in item 24, or only the virtual property.
- **`ITeventRoutedMember<TTevent>`** (`src/Compze.Teventive/ITeventRoutedMember.cs:9`): **keep the interface, make its
  member internal.** The public interface is needed: it is what lets `Component(...)` infer the tevent type. Its one
  member, `DispatchInternal`, is public and marked obsolete to keep callers off. Every implementer is Compze's own, in
  Compze.Teventive or in the tevent store, which has `InternalsVisibleTo`. An `internal` interface member does what the
  attribute only asks for, and closes the interface to implementers outside Compze.
- **`IMutableTaggregateTeventWithIdentity`** (`IMutableTaggregateTeventWithIdentity.cs:7`), and
  `IMutableTaggregateTevent` losing `SetTessageIdInternal`: **keep.** It follows the identity's move.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates`

- **`ISnapshotable<TSelf, TSnapshot>`** (`Taggregates/ISnapshotable.cs:18`): **keep**, in Compze.Teventive, so that a
  domain assembly needs no reference to a store. `TSelf` is there so that `Taggregate(() => new VocabNote(...))` needs
  no type arguments. It also makes each concrete class of a hierarchy declare its own snapshot type, which is worth
  having. Its documentation is good, and names the trap of shared objects.
- **`SnapshotRoundTrip`** (`Taggregates/SnapshotRoundTrip.cs:20`): **keep both members.** Comparing the instances and
  not only their snapshots is the right check, and a consumer couldn't build it: it must know which fields are Compze's.
  `DifferencesOf` is the primitive and `AssertLosesNothing` one line on it. JAStudio's tool that checks all 79,896 notes
  shows a use outside specifications, so it belongs in the production assembly. If `snapshotsAreEqual` goes (below),
  both lose that parameter.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates.BaseClasses`

- **`TeventApplyingRoot<TRoot, TTevent>`, `TeventApplyingComponent<TComponent, TComponentTevent>`**
  (`TeventApplyingRoot.cs:15`, `TeventApplyingComponent.cs:11`): **keep.** One implementation of applying tevents under
  taggregates and query models, at no cost to how a consumer declares a class. Their constructors are internal, so only
  Compze derives from them. `DeclareAppliers` and `AssertInvariantsAreMet`, protected: **keep.**
- **`Taggregate<...>`** (`Taggregate..cs:13`):
  - **Its constructor is public: make it protected.** (run) The spike removed the protected constructor that took a
    `TeventDispatcherConfig`, and the class isn't abstract, so the compiler gives it a public one. Code outside a
    taggregate class can now write `new Taggregate<...>()`, which compiles and throws.
  - **`AssertCanBeChanged()`** (`:73`): **make private.** Its documented use is a class that "changes its state other
    than by applying a tevent", which is what a taggregate is defined not to do. It was added for JAStudio's migration,
    and JAStudio calls it nowhere now (read: no match in `JAStudio`). `Publish` keeps using it. Cost to JAStudio: none.
  - **`DeclareHandlers`** (`:37`): **keep.** One wart to know of: it is typed on the root, so a root whose subclasses
    have tevent handlers declares an empty routing for them to extend.
  - **`RestoreIdentity(TaggregateId)`** (`:93`): **keep with another shape**, if Magnus wants a state store's
    taggregates able to keep their version: an optional version. See headline 7 and 4.5.
  - Now deriving from `TeventApplyingRoot`, implementing `ITeventRouted`; `RegisterTeventAppliers()`,
    `RegisterTeventHandlers()` and the `TeventDispatcherConfig` constructor removed: **agreed.**
- **`TeventiveComponent<...>`, its nested `Component`, `Entity`, `RemovableEntity`, `Tentity<...>`,
  `TeventiveRemovableEntity<...>`** now deriving from `TeventApplyingComponent`, with constructors that take only the
  parent, `DeclareAppliers` on `Tentity` and `TeventiveRemovableEntity` that put the base class's routes first, and
  `RegisterTeventAppliers()` removed: **keep.** A consumer's entity no longer writes an applier for its created tevent.
- **`Tentity<...>.ICollectionManager` now an `ITeventRoutedMember`:** **keep.**
- **`IEntityCollectionManager.AddRestored(id)`** (`_IReadOnlyEntityCollection.cs:27`): **keep.** Without it a taggregate
  with entities can't be snapshotable at all, and it is asserted to work only while restoring.
- **`ITeventiveInternals`** extending `ITeventRouted`, without `ApplyTeventInternal` and
  `RegisterTeventAppliersInternal`: **keep**, and it follows whatever `ITeventRouted` becomes.
- **`TaggregateIdentifyingTevent.WrapIn(Type, ITaggregateTeventWithIdentity)`** (`TaggregateIdentifyingTevent.cs:17`):
  **keep.** It spares a migration's author a cast to the type the modifier takes.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`

- **`SharedTomponentSlotOfAClass<...>`, `SharedTomponentSlotInAnOwner<TTomponentTevent>`**
  (`Shared/SharedTomponentSlotOfAClass.cs:33`, `:11`), and `ISharedTomponentSlot` and `SharedTomponentSlot` removed:
  **keep.** A slot that belongs to the class is the same idea as routing per class. One thing in its shape: the
  constructor takes `Func<TOwner, ISharedTomponentInternals<TTomponentTevent>>` (`:48`), the interface whose members are
  marked for internal use, and then asserts that what it gets is a `SharedTomponent<TTomponentTevent>` (`:71`). Take a
  `Func<TOwner, SharedTomponent<TTomponentTevent>>`, which is what it requires.
- **`SharedTomponent<TTomponentTevent>`:** the constructor taking a `SharedTomponentSlotInAnOwner`, and
  `RegisterTeventAppliers()` removed: **keep.** `DeclareAppliers<TTomponent>` (`SharedTomponent.cs:47`) needs its type
  argument written out, where every other base class's infers the class. A wart of `SharedTomponent` having no type
  parameter for itself; not worth changing the class for.
- **`SharedTentity<...>.DeclareAppliers`** (`SharedTentity.cs:53`): **keep.**
- **`SharedTentityCollection<...>`** sealed, constructed with a `SharedTomponentSlotInAnOwner`, with `AddRestored`
  (`SharedTentityCollection.cs:39`): **keep.**
- **`ISharedTomponentInternals.ApplyAdoptedTeventInternal`** replacing `ApplyTeventInternal`: **keep**, or make it
  internal as for `ITeventRoutedMember`.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates.Tevents`

- **`ITaggregateTevent`, `ITaggregateCreatedTevent`, `ITaggregateDeletedTevent`, `TaggregateTevent` no longer
  exactly-once tevents with an identity** (`Tevents/ITaggregateTevent.cs:13`): **keep.** The types stop claiming a
  delivery that most taggregates don't have. What a consumer upgrading must be told, loudly, in the changelog: a
  hierarchy that doesn't declare its delivery is from now on published within the process only, and nothing fails.
- **`ITaggregateTeventWithIdentity`, `TaggregateTeventWithIdentity`** (`ITaggregateTeventWithIdentity.cs:8`,
  `TaggregateTeventWithIdentity.cs:8`): **keep.** The tevent store's requirement becomes a compile-time constraint.
- **`ITaggregateDeletedTevent` as how a taggregate is deleted:** **keep.** One fact said once.

### Compze.Tessaging

- **`ITeventObservation`** (`src/Compze.Tessaging/TessageBus/ITeventObservation.cs:20`): **keep with another shape.**
  Observers that come and go are the bus's own capability with a shorter lifetime, and a consumer can only build it by
  routing by type outside Compze. What disposing does and doesn't promise is documented truly. The shape: it hands the
  callback a `TeventObservationRegistrar`, a class one of whose members, `AtRuntime`, always throws there. Hand it a
  type that has only what an observer may declare at runtime.
- **`AtRuntime<TTevent>()`** on `TeventObservationRegistrar` and `ITeventObservationRegistrar`
  (`TeventObservationRegistrar.cs:78`): **keep the declaration, move it.** Declaring at composition what may be observed
  at runtime is right for an endpoint, which must tell its peers. It isn't an observer, so it doesn't belong among
  `ForTevent` and `ForEachUnitOfWork`: put it on the builder, beside `ObserveTevents`. Then no member throws for
  where it is called, and the registrar is the same thing in both places. Requiring it in a plain container too is a
  fair price: the rule is explained in section 6, as Magnus asked. Cost to JAStudio: four lines move
  (`JAStudio.Core/AppBootStrapper.cs:176`).
- **`ForEachUnitOfWork<TTevent>(...)`** (`TeventObservationRegistrar.cs:57`, `:61`): **keep.** A consumer can't build
  it: nothing on a tevent marks where its unit of work ends. Giving no information about the unit of work is right
  until someone needs it.
- **The convenience overloads: four on `ITeventObservationRegistrarCE`, six on `TeventObservationRegistrarCE`:**
  **keep, noting what they show.** They mirror `ForTevent`'s, so leaving them out would surprise. But one new verb cost
  fourteen public members, because there are two registrars, a class and an interface, each with its own extensions.
  One registrar type would halve that for every verb to come. Not the spike's doing.
- **`ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()`** (`Engine/ILocalTessagingEngine.cs:26`): **move to a
  testing package.** Its own documentation says what it is for: "a specification that asserts on what an observer did,
  and goes on". For endpoints the same wait is in Compze.Tessaging.Hosting.Testing
  (`src/Compze.Tessaging.Hosting.Testing/TestingEndpointHost.cs:132`), through an internal tracker. The plain
  container's belongs beside it. It also gives up after 30 seconds for each queue, which its documentation doesn't say.
  Cost to JAStudio: one call, in its specifications' base class
  (`JAStudio.Core.Specifications/Specification_using_a_collection.cs:64`), and a package reference there.

### Compze.Teventive.TeventStore

- **`TeventStoreBuilder`, and `declare` on `TeventStoreRegistrar.TeventStore` and `TeventStoreForFlexibleTesting`**
  (`Wiring/TeventStoreBuilder.cs:13`, `Wiring/TeventStoreRegistrar.cs:24`): **keep**, as what several stores in one
  container cost: each store must know its classes for the updater to route. Two things:
  - `declare` is optional, and a tevent store that declares nothing keeps nothing, so every `Get` fails. It is
    optional only because the typermedia package declares later.
  - That package takes the builder out of its callback and declares on it afterwards
    (`src/Compze.Teventive.TeventStore.Typermedia/TeventStoreRegistrationBuilder.cs:16`). Tessaging's builders say that
    a registrar exists only inside its callback, and enforce it. Here the opposite is relied on.
- **`TeventStore`'s `GetTaggregateHistory`, `GetTaggregateHistoryForUpdate`, `SaveSingleTaggregateTevents`,
  `StreamTevents` typed on `ITaggregateTeventWithIdentity`; `TeventMigration<T>` constrained to it:** **keep.**
- **`SelfGeneratingQueryModel<...>`:** deriving from `TeventApplyingRoot`, its `Component` and two `Entity` classes from
  `TeventApplyingComponent`, parameterless constructors for the model and its entities, `Component(ITeventRouted
  parent)`, `CreateSelfManagingCollection(ITeventRouted parent)` returning the new `ICollectionManager` interface,
  `DeclareAppliers` on the entities: **keep.** Twelve public types removed and one added, among them six concrete
  collection classes, for which there is now one interface, which is the strategy's preference. A component takes its
  parent only to assert the parent's routing, typed as `ITeventRouted`: if that interface changes shape, this
  parameter needs a type of its own.
- **Removed:** `IQueryModelEntityCollectionManager`, `IReadonlyQueryModelEntityCollection`,
  `QueryModelEntityCollection`, `NestedComponent`, `NestedEntity`, `RemovableNestedEntity`, the `CollectionManager` and
  `QueryModelEntityCollectionManager` classes, `RegisterTeventAppliers()`, the per-instance constructors: **agreed.**

### Compze.Teventive.TeventStore.Abstractions

- **`ITeventStoreUpdater` removed:** **agreed.** One updater for every store, and the `IDisposable` that had an
  `urgent:` on it goes.
- **`ITeventStoreReader : ITaggregateStoreReader`, without `GetReadonlyCopy`** (`ITeventStoreReader.cs:11`): **keep with
  another shape**, a small matter. Inheriting makes "the tevent store's reader" a reader of every store: its `Get` gives
  a state store's taggregate too. What only the tevent store can read is `GetHistory` and `GetReadonlyCopyOfVersion`;
  an interface of just those two, beside the shared reader, would say what it is. Magnus decided the inheritance (item
  22), so this is for him to weigh again or not.
- **`ITeventStore`, `ISingleTaggregateInstanceHandlingTeventMigrator`, `ITeventModifier`,
  `TeventStoreTestingExtensions` typed on `ITaggregateTeventWithIdentity`:** **keep.**

### Compze.Teventive.TeventStore.Typermedia

- **`SaveTaggregate`, `GetReadonlyCopyOfTaggregate`, `GetReadonlyCopyOfTaggregateVersion`, `GetTaggregateHistory`,
  `TaggregateLink` constrained to taggregates and tevents with identity:** **keep.** `HandleTaggregate` also declaring
  the class for the store: **keep**, with the remark on the builder above.

### Compze.Teventive.StateStore (new)

- **`StateStoreRegistrar.StateStore(...)`** (`src/Compze.Teventive.StateStore/StateStoreRegistrar.cs:16`): **keep,
  rename** for what the store is: section 3.
- **`StateStoreBuilder`** (`StateStoreBuilder.cs:17`): **keep.**
  - `Taggregate<TTaggregate, TSnapshot>(createEmpty, snapshotsAreEqual)` (`:35`): **keep, without
    `snapshotsAreEqual`.** A factory is a fair seam for any consumer. Note that the two stores now differ: the tevent
    store builds a taggregate with its parameterless constructor, and can hand it nothing. Whether a taggregate
    should be handed services at all is in section 6.
  - The overload with a service (`:42`): **keep.** With a factory at all, a consumer whose taggregates take services
    has no other way to reach them: the container doesn't exist yet where the store is composed. It resolves the
    service at every instance built, 80,000 times in a load of JAStudio's; once for each store would do.
  - **`snapshotsAreEqual`: remove.** (read) It exists so that a reload can report which taggregates changed. Without it
    the snapshot type's own `Equals` is used, which is a trap for the commonest snapshot type: a record with a list in
    it has an `Equals`, so the check passes, and compares the list by reference, so every taggregate is reported
    changed. JAStudio found the trap from the other side and passes a comparison that serializes both snapshots and
    compares the bytes (`JAStudio.Core/Storage/Dto/CorpusNoteDto.cs:27`). The storage has those bytes: a store kept in
    files can tell that a file is as it last read or wrote it, as the MemoryPack cache already does by length and time,
    or compare what it would write with what the file holds. Then the storage says what changed, snapshot types need
    no equality, a reload reads only what changed, and four public signatures lose a parameter. Cost to JAStudio: it
    deletes the comparison and six arguments.
  - `Index<TTaggregate, TKey>(keysOf)` (`:56`): **keep, and say its rules.** (run)
    - An index is known by its class and key type alone: the container resolves `ITaggregateIndex<TTaggregate, TKey>`.
      So a second index of vocabs by `string` can't be declared, and each index needs a key type of its own.
      JAStudio's file of ten such types opens by explaining this
      (`JAStudio.Core/Note/Collection/VocabIndexes.cs:13`). That is a sound rule, in keeping with Compze routing by
      type, but `Index`'s documentation doesn't state it, and breaking it gives the container's "Service type
      'ITaggregateIndex`2[[...' is already registered".
    - The store finds an index with `Indexes.OfType<TIndex>().Single()` (`_private/TaggregateStateStore.cs:52`), and
      the interface is covariant in the class and contravariant in the key. With an index of every taggregate by
      `string` and one of the vocabs by `string`, resolving the first throws "Sequence contains more than one
      element". Match the exact types.
    - What a key function must be belongs here too: a function of the taggregate's own state alone, safe to call from
      any thread at any time on an instance many threads read. Only the changelog says the first half.
  - `StoredInFiles(folder, options, declare)` and the overload with a service (`:69`, `:75`): **keep.** JSON files of a
    consumer's own layout and format are worth having for any consumer whose data is reviewed as diffs, and writing
    several files as one is the part worth Compze doing. What happens when a commit fails, and that nothing is flushed,
    must be in its documentation: 4.3.
- **`IStateStore`** (`IStateStore.cs:8`): **keep.** Both members are this all-in-memory store's own. A consumer can
  build the second only for the units of work it begins itself, with a gate of its own around them; the store's gate
  also holds back those that Compze begins, such as a tessage handler's. (Corrected after I read the builders' column:
  see section 6.)
  - `ReloadFromStorageAfter(Action)` is the primitive and `ReloadFromStorage()` is it with nothing to run. Keeping
    both is fine.
  - **`ReloadFromStorage()` on a store with no storage removes every taggregate** and reports each as removed (run).
    It should refuse.
  - It is also the only way to load at a time of the consumer's choosing, and JAStudio's start uses it for that
    (`JAStudio.Core/Note/Collection/JPCollection.cs:314`). Say so: section 3.
  - Its waits give up after a fixed 30 seconds, in both directions: 4.2.
- **`ReloadedTaggregates`** (`IStateStore.cs:27`): **keep with another shape:** `IReadOnlyCollection<TaggregateId>` for
  `Added`, `Changed` and `Removed`. They have no order, and the spike's own rule is that a result with no order isn't a
  list.
- **`ITaggregateIndex<out TTaggregate, in TKey>`** (`ITaggregateIndex.cs:9`): **keep.**
  - `Lookup`: **keep**, as a collection of references in no order. One thing: outside a unit of work it returns the
    index's own array, which a caller can cast and write to, and the index then files what was written (run). Keeping
    each key's references in a read-only collection made when a commit replaces them costs a lookup nothing.
  - **`HasAny`: remove.** (read) It is `Lookup(key).Count > 0`, at the same cost: outside a unit of work both are one
    dictionary read and no allocation (`_private/TaggregateIndex.cs:43`, `:59`), and inside one `HasAny` calls
    `Lookup`. It dates from a prototype whose lookups allocated. Cost to JAStudio: one line
    (`JAStudio.Core/Note/Collection/NoteIndex.cs:51`). If a later store can answer "any?" cheaper than a lookup, it
    comes back then.
- **`TaggregateFiles`** (`TaggregateFiles.cs:11`): **keep.**
  - `Of<TTaggregate>(folder)`: **keep.**
  - **`fileOf`: remove.** (read) What it offers, a file that depends on more than the id and moves when the state
    changes, has no user. JAStudio passes a function that repeats the default exactly, "since `Write` writes the same
    file" (`JAStudio.Core/Storage/CorpusNoteFiles.cs:37`, `:52`): it needs to know a note's file, and to write every
    note anew, and has neither from Compze. For that one parameter the store carries the moving of files, an assertion
    that two taggregates don't share one, a hole the builders name (two units of work committing taggregates that get
    the same new file), and taggregate instances crossing a storage seam meant to know only snapshots. Cost to
    JAStudio: three arguments go. What it needs instead is in 4.3.
  - **`KeptInMemory()` and `ITaggregateFilesInMemory`** (`:41`, `ITaggregateFilesInMemory.cs:10`): **move to a testing
    package**, plugging into the internal `IFileSystem` as the MemoryPack package plugs into the cache seam. Their
    documentation says "for specifications". Running the real store over files kept in memory is the right way to
    specify a consumer, and worth shipping; it isn't part of the store. Cost to JAStudio: more than the others. Its
    `TestEnvironmentSpecificDependenciesRegistrar` is in JAStudio.Core, so that assembly would reference the testing
    package, or the registrar would move to a test assembly, where it belongs.
- **The package's description** (`Compze.Teventive.StateStore.csproj:5`) still offers "a narrow seam for durable
  storage", which is internal now.

### Compze.Teventive.StateStore.MemoryPack (new)

- **`TaggregateFilesMemoryPackCE.CachedWithMemoryPack()`**
  (`src/Compze.Teventive.StateStore.MemoryPack/TaggregateFilesMemoryPackCE.cs:16`): **keep.** A third-party serializer
  in a package of its own, plugged into an internal seam, is how such a thing should be offered. Its shape misleads a
  little: `files.Of<VocabNote>("vocab").CachedWithMemoryPack()` reads as caching the vocabs, and it caches every class.
  The same goes for `KeptInMemory()`. Both are settings of the files as a whole.

### What the list is missing

- **Constraints of generic methods.** The tool compared types' constraints, not methods'. Changed and not listed:
  `ITeventStoreReader.GetReadonlyCopyOfVersion<TTaggregate>`, `TeventStoreRegistrationBuilder.HandleTaggregate`,
  `TeventStoreApi.RegisterHandlersForTaggregate`, and `TueryApi.GetForUpdate`, `GetReadOnlyCopy`,
  `GetReadOnlyCopyOfVersion`, `GetHistory` and `TommandApi.Save`, all now requiring tevents with identity.
- **What changed without a signature changing,** which a comparison of assemblies can't show, and a consumer meets all
  the same: a tevent store keeps only the classes declared for it; a transaction must be begun by `ExecuteUnitOfWork`;
  `ITeventStore` is a singleton; a publish that fails rolls the unit of work back even when caught; a taggregate's
  tevents are no longer delivered to other processes unless their hierarchy says so.
- **`SharedTomponentSlotInAnOwner`** is a `readonly ref struct`; the list says `struct`.
- **The changelogs don't mention tevent routing per class at all:** not `TeventRouting`, `DeclareAppliers`,
  `ITeventRouted`, the slots, `AddRestored`, nor that `RegisterTeventAppliers()` is gone, which is the change that
  touches every taggregate class a consumer has.

## 3. The all-in-memory question

**Yes: the state store is designed for an application that holds everything in memory, and by construction, not by a
setting.**

- It loads every taggregate at its first use and holds them all (`_private/TaggregateStateStore.cs:136` to `:156`).
- Its storage can only load everything and write a unit of work's changes (`_private/Storage/ITaggregateStorage.cs`):
  there is no reading of one taggregate.
- A read without a lock, a private copy through a snapshot, an index built by running a function over every instance,
  `All` as a scan of a dictionary, a reload that loads and compares everything: each follows from every instance being
  there.
- The files are a copy written through at each commit. Nothing reads them between a load and a reload.

Magnus's assumption about indexes is right for this index and not for the idea. An index defined by a function of a
taggregate's own state needs every instance only to be built. Kept in a database, it could be maintained at each commit
from the taggregate that changed, without the others loaded. What can't survive that move are the promises this one
makes: a lookup with no lock and no allocation, and a key of any .NET type compared in memory.

**A store of its own, not a mode.** The decisions that make this store what it is are the ones above, and a store that
loaded taggregates as they were asked for would make each of them otherwise: reads would go to storage, something would
decide what to keep and what to let go, indexes would live in the storage, versions would be stored to detect a
concurrent change, and reloading everything would mean nothing. Little but the interfaces would be shared. Those
interfaces exist already: the reader, the updater, the bulk reader, `ISnapshotable`. That is the more general thing, and
the tevent store is its other member. A state store with modes would be designed from one example.

**What it means for the name.** "State store" says what is stored, state and not history, and pairs well with "tevent
store". It says nothing of everything being held, and it takes the general name: if Compze one day has a state store in
a database, this one has it. Renaming costs little now, with one consumer and a 0.1.0-alpha version. My proposal is that
the registrar's method and `IStateStore` say "in memory":

```csharp
registrar.InMemoryStateStore(store => store.Taggregate(() => new VocabNote(services))
                                           .StoredInJsonFiles(folder, options, files => files.Of<VocabNote>("vocab")));
```

It reads truly both ways: with files, an in-memory state store stored in JSON files; without, one that is in memory
only. The risk is that "in memory", in .NET, suggests a stand-in for specifications, as Entity Framework's provider
is. The pattern has a name of its own, Martin Fowler's [memory image](https://martinfowler.com/bliki/MemoryImage.html):
an application that holds its whole state in main memory and rebuilds it from what it stored. His has a log of events
behind it, and this one a snapshot of each taggregate. The term is exact and little known. Whether the package is
renamed too is Magnus's call.

The files that specifications keep in memory then need another name than `KeptInMemory`: "the files kept in memory"
beside "the in-memory store" is one phrase for two things. Section 5.

**What it means for the public surface.**

- `IStateStore` holds this store's own operations and nothing general. Right as it is.
- `ITaggregateIndex` and `Index` are in the state store's package, not in Compze.Teventive. Right as it is.
- **When it loads is part of what it is, and has no member.** A store that holds everything has one event that
  matters at its start, and here it happens at the first use: on whichever thread, and inside that caller's
  transaction if it is a get for update. JAStudio's cold start, before it had the cache, took 37 seconds to load its
  notes. A consumer that wants the load at a time of its choosing calls `ReloadFromStorage()`, which works and isn't
  said. Say it, or name the member for both: `LoadFromStorage`.
- **The shared interfaces carry this store's promises in their documentation** (read):
  - `ITaggregateStoreReader`: "the committed instance, which is read only and costs no lock and no copy".
  - `ITaggregateReference.Current`: a property, and "the committed instance". The tevent store has no committed
    instance: two reads give two instances, each built from the history.
  - `ITaggregateStoreUpdater`: "the unit of work's private copy" and "the taggregate's lock", with the tevent store in
    a remark.
  - `ITaggregateStoreBulkReader.All`: every reference in one collection, "the store as it was at the call". For the
    tevent store that is every id in memory, and one read of a history for each `Current` after it.

  Write the shared interfaces' documentation for any store: what is the same in every store, and nothing else. Put each
  store's costs and guarantees in that store's documentation.

**What it means for the documentation.** `StateStore(...)`'s summary should say, before anything else, that the store
holds every taggregate in memory: that its start reads all of its storage, that its memory grows with what it stores,
and that its indexes and `All` are fast because of it. A reader of `StoredInFiles` alone would take the files for where
taggregates are read from.

## 4. The store design

### 4.1 Taggregate-references

Sound as an idea, and the right thing for bulk reads to return. What I would change is in section 2: equality across
classes (run), `ForUpdate()`, what `Current` costs and throws, references kept for good. One more observation, about
the first consumer: JAStudio holds references in few places, makes all of those with a class of its own, and turns
every reference a lookup gives into an instance on the next line. So the claim that "the types say: don't hold an
instance across units of work" is so far carried by a consumer's own discipline, not by Compze's types. I don't think
that argues against references. It argues against adding anything more to them.

### 4.2 Indexes defined by state

The design is right: a function of state, kept by the store at prepare and commit, read by a unit of work as it left
things, with no order of handlers to get right. Keeping no record of each taggregate's keys, and asserting at prepare
that the committed instance is filed under the keys its state gives, is a good trade.

- **Identity and resolution:** section 2 (run).
- **Reading its own unit of work's changes costs every lookup inside a unit of work that holds a copy of the class:**
  the keys of each copy held are computed again, as the `performance:` at
  `_private/TaggregateIndex.cs:53` says. It is what a uniqueness check needs, and JAStudio asked for it. A caller
  inside a unit of work has no way to ask for the committed index alone. I would leave that until someone needs it.
- **A reader can see a taggregate's new state filed under its old keys for a moment:** the commit puts each copy in
  place, then applies the index changes (`_private/StateStoreSession.cs:153` to `:165`). The design lists atomic
  visibility as open. `ITaggregateIndex`'s documentation should say it.
- **The reload's gate has fixed waits that nothing documents** (read,
  `_private/UnitsOfWorkAndReloadsGate.cs:12` to `:22`). A unit of work that begins during a reload waits 30 seconds
  and then throws; JAStudio's cold load without the cache took longer than that. A reload waits 30 seconds for the
  open units of work and then throws, and since new units of work are held back only once it has started, a steady
  stream of them can keep it from ever starting.

### 4.3 The storage seam, and the files

**Keeping the seam internal is right.** It was shaped by JAStudio's files, and a public seam is hard to take back. It
does mean that a state store is kept in JSON files of System.Text.Json or not at all, which the package's description
should say instead of offering a seam.

- **`fileOf` is what made the seam unclean** (read). "Narrow: it knows snapshots, not taggregates", and
  `TaggregateSnapshot` carries a taggregate out to the storage (`TaggregateToWrite`) and another back
  (`RestoredTaggregate`), because a file's place may depend on the instance. Without `fileOf` a file's place is a
  function of the class and the id, and the seam knows snapshots again.
- **What JAStudio needs that `fileOf` stands in for:** to rewrite every taggregate's file, to see that loading and
  saving changes no file after a change to the snapshot types or the options. It does that with five lines that must
  agree with the store's layout and format, marked `todo:compze:` (`JAStudio.Core/Storage/CorpusNoteFiles.cs:73`). Any
  consumer of a store kept in files whose snapshot types change will want it, since otherwise old and new formats
  stay mixed until each taggregate is next changed. I would weigh one member for it on `IStateStore` against
  `fileOf`: one parameter out, one method in, and the consumer's copy of the layout gone.
- **A failed commit stops the process** (`_private/Files/PreparedFileWrite.cs:76`), before the journal is written or
  after. I think that is the right simple version: the transaction can no longer fail, and the next start finishes or
  discards the write. But a library that ends its host is a guarantee to print in capitals, and it is in no public
  documentation. It also follows from a file another program holds for more than about two seconds, 20 tries 100
  milliseconds apart (`_private/Files/CommitJournal.cs:17`): a backup, a scanner, or the checkout's own git working on
  the corpus.
- **Nothing is flushed to the disk,** so a commit survives the process dying and not the machine losing power. A fair
  default for files in version control. Not in the public documentation either.
- **What changed at a reload should come from the storage,** not from comparing snapshots: section 2, at
  `snapshotsAreEqual`.
- **The MemoryPack cache** validates by each file's length and time and a fingerprint of the snapshot type's fields.
  I read it and found nothing wrong. I didn't test it.

### 4.4 Dispatching tevents synchronously

As built it does what it says: a tevent reaches the handlers when its taggregate's cascade has ended and its invariants
hold, a handler's publish on the same taggregate queues behind what came before, a runaway gives up at 100, and a
publish that failed rolls the unit of work back even when caught. I found no fault in the hand-out queue by reading it.

- **A new taggregate saved again after a rollback** (run). `Save` takes the instance's uncommitted tevents through
  `ITaggregate.Commit`, which hands them over once. If that unit of work rolls back and the code saves the same
  instance again, as a retry around a unit of work does, there is nothing to dispatch, and the state store commits it
  silently: no handler, no observer, and a taggregate in the store whose creation nobody heard of. The tevent store
  refuses the same save (`src/Compze.Teventive.TeventStore/_private/TeventStoreSession.cs:63`). The state store should
  too, saying that the instance was saved in a unit of work that rolled back and must be created again. This is the
  kind of loss Magnus's `todo:` at `ITaggregate.Commit` is about.
- **Exactly-once delivery, in both directions** (read). It concerns a state store composed in an exactly-once
  endpoint, which has a database; a plain container with a LocalTessagingEngine has no outbox, and isn't touched.
  - **Out:** `TaggregateUnitOfWork.Dispatch` sends an `IExactlyOnceTevent` through `PublishAsync`
    (`src/Compze.Teventive/TaggregateStores/_internal/TaggregateUnitOfWork.cs:59`), for either store. For the tevent
    store the outbox row and the tevent are in one database transaction. For the state store the row is in the
    database, and the taggregate's file is written by a volatile participant once the transaction has committed. A
    process that dies between the two has promised its peers a change that the next start discards with its
    temporary files.
  - **In:** the spike has the exactly-once inbox run each handler in a unit of work that the updater accepts, and its
    changelog says that the updater "works in an exactly-once handler". The record that the tessage was handled
    commits with the database, and a state-store taggregate the handler changed is written after it. Between the two,
    the tessage is handled for good and its change is lost.

  The design names "a database and the JSON store in one transaction" as not settled; the code accepts both anyway.
  Per-peer exactly-once in-order delivery is Compze's cornerstone, and holding by construction is the bar. So until it
  is designed, the state store should refuse what it can detect where it is composed, a class whose tevents are
  exactly-once, and state the rest as a limit in its documentation. I didn't demonstrate either: that takes killing a
  process at the right instant.
- **Handlers run between two publishes of one domain method,** and see the taggregate between them. That is what
  synchronous dispatch is, and Magnus chose it knowing. It belongs in the updater's documentation as a rule for whoever
  writes a taggregate: each publish must leave it fit to be read.
- **A handler that gets another taggregate for update can wait on a unit of work that waits on it,** until one gives up
  after 30 seconds, with a message that names the taggregate and not the holder. Known, and Magnus has a `todo:` for a
  lock owned by a transaction.

### 4.5 The tevent store taking the same internals

One unit of work across both stores is a real gain, and so is deleting `ITeventStoreUpdater` with its `urgent:`. The
price is that two stores now stand behind one set of interfaces and differ in what those interfaces do:

| | State store | Tevent store |
|---|---|---|
| `Get` outside a unit of work | the one committed instance, no lock | a new instance built from the history at each call |
| `ITaggregateReference.Current` | a dictionary read | a read of the history and a replay |
| `Get<TBase>` for a subclass's id | gives the subclass | throws when several declared classes derive from `TBase` |
| `TryGetForUpdate` of an id not held | holds the id's lock | reads with the database's update lock; what that holds for an id with no history I didn't check |
| `Save` of an instance already saved in a unit of work that rolled back | commits it, dispatching nothing | throws |
| `Save` with a deleted taggregate's id | adds it | fails: the id stays used |
| `ITaggregate.Version` | counted from the last load | the number of tevents in its history |
| `All<TTaggregate>()` | a scan of memory | a scan of every history's creation tevent, and one more query for the deleted |

The first two rows and the last are what the stores are. The five between are where a consumer who moves a class from
one store to the other, or writes code for both, is surprised. Each should either be made the same or be stated in the
shared interface's documentation as depending on the store.

- **The version** (run). A state store's taggregate is at version 1 after every load, and its next tevent gives it
  version 2 again. For JAStudio a stored version would be noise in every diff. For another consumer it breaks whatever
  reads `TaggregateVersion` as a count: a query model that expects each tevent to follow the last, a peer that orders
  by it. Since the snapshot is the consumer's own type, the consumer could decide: `RestoreIdentity(id)` for one who
  doesn't store it, `RestoreIdentity(id, version)` for one who does. Whatever is decided, `ITaggregate.Version`'s
  documentation should say what a store may do to it.
- **A transaction must be begun by `ExecuteUnitOfWork`,** now for the tevent store too. The refusal is loud and its
  message is good. It is in the changelog and not in the updater's documentation.
- **The record of which unit of work began which transaction is a static of Compze.DependencyInjection**
  (`src/Compze.DependencyInjection/_internal/AmbientUnitOfWork.cs`), shared by every container in the process. The
  builders' `todo:` names the case it gets wrong: two containers in one transaction.

### 4.6 The bulk reader

Right in every part: apart from the reader, shared by the stores, giving references, in no order, as a collection
taken at the call. One small thing in the state store's `All` (read, `_private/TaggregateStateStore.cs:103`): it reads
`ConcurrentDictionary.Values`, which takes every lock of the dictionary and copies every value, of every class, so a
read of all the kanji copies all the notes first and holds up commits while it does. Enumerating the dictionary does
neither.

## 5. Names

In the order I care about them. Where the code already has a word, I have used it.

| Now | Proposal | Why |
|---|---|---|
| `StateStore(...)`, `IStateStore` | `InMemoryStateStore(...)`, `IInMemoryStateStore`, or as Magnus decides in section 3 | It holds everything in memory, and "the state store" is the general name |
| `ITaggregateStoreUpdater.Save` | `Add` | It only adds, and throws for a taggregate the store holds |
| `ITeventRoutedMember<TTevent>` | `ITeventRoutedPart<TTevent>` | The code's word is "part" everywhere else: `AssertRoutesToAPart`, `RoutesToAPartWhoseTeventsAre`, `partTevent`, and the interface's own summary |
| `TeventApplyingComponent<,>` | `TeventApplyingPart<,>` | Components and entities both derive from it, and both are parts |
| `SharedTomponentSlotOfAClass<...>` | `SharedTomponentSlot<...>` | The slot of an instance is gone, so "of a class" tells it apart from nothing |
| `StoredInFiles` | `StoredInJsonFiles` | The files are JSON, and its signature takes `JsonSerializerOptions` |
| `TaggregateFiles` | `TaggregateFilesBuilder` | It declares the files, as `StateStoreBuilder` and `TeventStoreBuilder` declare stores; `ITaggregateFilesInMemory` is the files |
| `KeptInMemory()`, `ITaggregateFilesInMemory` | `OnAFileSystemInMemory()`, `IFileSystemInMemory`, in the testing package | "Kept in memory" is what the store does with every taggregate; this is a file system that isn't the disk |
| `CachedWithMemoryPack()` | `StartingFromAMemoryPackCache()` | It is a cache of the start's load, as the internal `StartFrom` and `ITaggregateFilesLoadCache` say; nothing is cached while running |
| `ITaggregateStoreBulkReader.All<T>()` | `ReferencesToAll<T>()` | It gives references; `All<VocabNote>()` reads as giving vocabs |
| `IStateStore.ReloadFromStorage`, `ReloadFromStorageAfter` | `LoadFromStorage`, `LoadFromStorageAfter` | They also make the first load, and a consumer starts with them |
| `ITeventObservation` | `IRuntimeTeventObservation` | Runtime is all that sets it apart from the builder's `ObserveTevents` |
| `AtRuntime<TTevent>()` | `ObservableAtRuntime<TTevent>()`, on the builder | It declares what may be observed; the code's own word is `DeclareObservableAtRuntime` |
| `ForEachUnitOfWork<TTevent>(...)` | `ForTeventsOfEachUnitOfWork<TTevent>(...)` | Beside `ForTevent`, it should say that what is observed is still tevents |
| `GetReadonlyCopyOfTaggregate` (the tuery) | `GetTaggregate` | As the builders' `review-naming:` says: inside a unit of work it isn't a read-only copy |

Names I would keep: `ITaggregateStoreReader`, `ITaggregateStoreUpdater`, `ITaggregateStoreBulkReader`,
`ITaggregateReference`, `Current`, `GetForUpdate`, `TryGetForUpdate`, `ITaggregateIndex`, `Lookup`, `ISnapshotable`,
`TakeSnapshot`, `RestoreFrom`, `RestoreIdentity`, `SnapshotRoundTrip`, `AssertLosesNothing`, `DifferencesOf`,
`TeventRouting`, `Declaration`, `Extend`, `DeclareAppliers`, `DeclareHandlers`, `ITeventRouted`, `TeventApplyingRoot`,
`AddRestored`, `ITaggregateTeventWithIdentity`, `ReloadedTaggregates`, `AwaitNoTessagesInFlightAsync`,
`SharedTomponentSlotInAnOwner`, `TeventStoreBuilder`, `StateStoreBuilder`, `Of<TTaggregate>`.

## 6. Where I disagree with the builders' views

Written after everything above. The last column of the README's table is mostly questions, so this answers each, and
says where I differ from a view it states. Reading it made me correct one sentence, marked where it stands: I had
written that a consumer can't build `ReloadFromStorageAfter`.

- **`Taggregate.AssertCanBeChanged()`: "Does it go once JAStudio's classes change state only by tevents?"** Yes, and
  they do now: JAStudio calls it nowhere. Make it private in this pass, not later.
- **`StateStoreBuilder.Taggregate(...)` with a service: "Is a factory with a service the right way for a taggregate to
  get its services?"** It is the right way if a taggregate gets services. Whether it should is the question behind it,
  and Compze's own tevent store answers no: it builds a taggregate with its parameterless constructor. JAStudio's notes
  are each built with `NoteServices`, which resolves from the container, and they reach the collection of all notes
  through it 31 times (`Services.Collection` in `JAStudio.Core/Note`). A note that was handed what a method needs as
  that method's arguments would need no factory, and that is the refactoring this overload spares JAStudio. I think
  JAStudio's notes would be better for it: reaching other notes from inside a note is what the move to participation
  handlers has been taking out. It is a large refactoring, and I would not hold the spike for it. So: keep both
  overloads, and write down that they are there for taggregates built in a way Compze wouldn't recommend, so that the
  next request to hand a taggregate more is weighed against that.
- **`ReloadFromStorage()`: "Is reloading everything a store's job, or should a consumer compose a new store?"** The
  store's job, for this store. The store, its indexes and the readers are singletons of the container, so a new store
  is a new container, and every reference and every observer held dies with the old one. A reload that keeps every
  reference is what makes a reference worth holding. Where I differ: `snapshotsAreEqual` is not the consumer's to
  supply. Section 2.
- **`ReloadFromStorageAfter(change)`: taggregates-opus would keep it, though JAStudio could build it in 20 to 30 lines
  with a gate of its own.** I agree with keeping it, and it stands the test of the rule on what callers can do as well
  themselves: they can't do it as well. A consumer's gate holds back only the units of work the consumer begins. The
  store's holds back every one, also those a tessage handler begins.
- **`ITaggregateStoreBulkReader.All`: "the stores' to give, or an index's with a single key?"** The stores'. I agree
  with taggregates-opus: one constant key is an index in name only.
- **`StoredIn<TService>(...)`, now `StoredInFiles<TService>(...)`: keep.** With the seam internal there is no wrapper
  a consumer could write instead.
- **`Lookup` in id order: dropped, agreed.** The same rule hasn't reached `ReloadedTaggregates`, whose three lists have
  no order.
- **`SnapshotRoundTrip.DifferencesOf` and `AwaitNoTessagesInFlightAsync()`:** the column says nothing. Mine: keep the
  first, move the second to a testing package.
- **Runtime observation and `ForEachUnitOfWork`: agreed,** with the shape remark on the registrar handed to the
  runtime callback.
- **`AtRuntime<TTevent>()` and its rule, which Magnus deferred to this review as unclear. Put plainly:**
  - An endpoint is sent a tevent by another endpoint only if it has asked for that tevent type. It asks once, when it
    starts, from what its composition declares.
  - An observer created later can't add to what was asked for, short of a round trip to every peer, during which
    tevents would be missed.
  - So a type that observers created at runtime are to get from other endpoints must be asked for at the start, before
    any such observer exists. `AtRuntime<TTevent>()` is that: ask our peers for `TTevent` from the start, because
    observers of it will come and go. Only an endpoint needs this much.
  - **The rule goes one step further:** an observer created at runtime is refused unless its type is declared so, in
    every composition, also a plain container, which has no peers to ask.
  - **What the step buys:** in an endpoint, a forgotten declaration can't leave an observer getting its own process's
    tevents and silently none of the others'. And the same code runs the same in a plain container and in an endpoint.
  - **What it costs:** a line for each tevent root observed at runtime, four in JAStudio. And forgetting one is found
    when the first view that observes the type opens, not when the container is built.
  - **It can't be made narrower by looking at the type.** An observer of a base tevent type also gets its subtypes, and
    a subtype may be remotable, so Compze can't tell from the type that nothing from another endpoint could match.

  **My view: keep the rule, and give it the reason that holds in every composition,** which
  `LocalTessagingEngineBuilder` already states: everything that can get into the engine is declared where it is
  composed. Observers created at runtime come and go; the tevent types they may observe stay declared. Told that way,
  a consumer with no endpoints isn't asked to understand peers in order to understand one line. And declare it on the
  builder, not on the registrar: section 2.
- **`StoredInFiles` and `TaggregateFiles.Of(folder, fileOf)`:** the column records that the seam went internal, which
  I agree with. It doesn't question `fileOf`, which I would remove.
- **`TeventRouting`: "Is a factory on each base class, hidden with `new` by the subclasses that add routes, the right
  shape?"** Yes. A consumer writes `DeclareAppliers(...)` and gets the most derived base class's, with its routes
  first, and never sees the hiding. `SharedTomponent`'s type argument is a wart I would leave. The larger question of
  shape, how the base class finds the routing, isn't in the column: `ITeventRouted`, in section 2.
- **`TeventApplyingRoot`, `TeventApplyingComponent`:** the second as `TeventApplyingPart`. Section 5.
- **`ITaggregateStoreUpdater.Delete` removed, the query models' parts, what step 5 removed, `AddRestored`:** agreed.

Where I differ from what the design documents state, outside the table:

- **"`ITaggregateStoreUpdater`: scoped, one for each unit of work"** (the-taggregate-store.md). Registered so, and
  nothing in it is of a unit of work.
- **"`HasAny(key)` checks a key without resolving a reference"** (the same). `Lookup` resolves none either, since it
  stopped allocating.
- **"Lookups and indexes return references, so the types say: don't hold an instance across units of work"** (the
  same). True of the types. The first consumer turns each reference into an instance at once and holds references of
  its own making, so it is the consumer's discipline that carries the rule.
- **"`ArgumentOutOfRangeException`, which is what a consumer can catch"** for a taggregate no store holds (the same).
  It can, and shouldn't have to: a deleted taggregate behind a reference is an ordinary thing for a view to meet, and
  that exception is thrown for many other reasons.
- **"Not guarded: two units of work committing at the same time whose taggregates get the same new file. Only a
  declared layout can produce it."** (the same.) Then the hole goes with `fileOf`.
- **"The two lists of tevents are created when first needed"** (the-design.md, section 3, superseded as a design).
  As built, every taggregate has its list, its queue and its observable from construction.

## 7. What I am unsure of, and what I couldn't verify

- **I ran only my own 29 specifications,** on the two SqliteMemory combinations. Not the suite, and nothing on a
  database server.
- **The exactly-once finding is from reading, in both directions.** I didn't kill a process between the database's
  commit and the journal, and I didn't compose a state store in an exactly-once endpoint to see what it does today.
- **The 120 bytes an instance** is arithmetic on .NET's object layout for an empty list, an empty queue and a
  `SimpleObservable`, not a measurement. That CA1033 fires on a consumer's classes I took from
  step-5-the-rest-of-the-teventives.md, and didn't reproduce.
- **Whether Magnus weighed the constructor argument in item 24** I can't tell from the documents. If he did, my remark
  on `ITeventRouted` is only a second opinion.
- **JAStudio I read, on `main`, and didn't build or run.** Whether any of its flows gets a new note for update before
  adding it, and so meets the 30-second wait, I didn't find out: its suite passes, so presumably none does today.
- **"In memory" as the name** is a proposal I'm not sure of: it tells the truth and may mislead.
- **`snapshotsAreEqual` and `fileOf`:** I judged what could replace them from the code. I built neither replacement.
  A storage that says what changed is more of a change to the reload than removing a parameter suggests.
- **Not reviewed again:** the isolation by a private copy, which has had its review. Not read closely: the SQL layers'
  new query, the observation dispatcher's queue, the MemoryPack cache's fingerprint beyond a reading, and the
  specifications the spike changed, except to see how the API is meant to be used.
- **The list of changes** I checked against the source by reading, not by running the comparison again.
