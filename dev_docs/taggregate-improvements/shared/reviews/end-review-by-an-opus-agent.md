# The end review of the taggregate-store spike, by an Opus agent

> Written at the start of the end review (2026-11-18), on the spike at 46cc881a6: some names and API here have changed
> since, by its decisions; see [../the-naming-pass.md](../the-naming-pass.md) and
> [end-review-consolidated.md](end-review-consolidated.md).

Reviewer: Opus 5.5 (claude-opus-5-5), 2026-11-18, on `taggregate-store-spike` at 46cc881a6. Written from the code, the
specifications, the sample, JAStudio's `main` (ff8f09aad) and the design documents, before reading the builders' views in
the README's table, `bugs-and-findings.md` and the earlier reviews; section 6 was written after reading them.

Five findings are shown by running code: specifications that pass and show the behaviour as built, on the branch
`end-review-opus` (8bcca9817, pushed), in `test/Compze.Tests.Unit/CQRS/Taggregates/SnapshotRoundTrips/EndReview_demonstrations.cs`
and `test/Compze.Tests.Integration/CQRS/StateStore/EndReview_demonstrations.cs`. Each finding below says whether it was
shown by running code or judged by reading. All `file:line` references are to the spike at 46cc881a6.

Two terms I use that the code doesn't have:

- **memory-resident:** a store that holds every taggregate it keeps in memory all the time, and uses its storage only to
  make commits durable and to load everything at its start. The state store as built is one.
- **the restore window:** the time in which an entity collection's `AddRestored` accepts an entity without a tevent: from
  `RestoreIdentity` until the taggregate's first publish, as built.

## 1. The headline

1. **The state store is a memory-resident store, a store of its own and not a mode of a general one, and its name and the
   shared interfaces' documentation should say so.** It loads every taggregate at its first use, its storage can only load
   everything (`ITaggregateStorage.LoadAll` is the seam's one read), and its indexes, its reload and its lock-free reads are
   correct only because everything is loaded. Magnus's intuition about the indexes is right. Meanwhile the shared
   `ITaggregateStoreReader` promises the state store's costs to every store: "costs no lock and no copy", which is false
   for the tevent store, which builds a new instance from the history at every read. Proposal: name the store for what it
   is (`MemoryResidentStateStore`, section 5), say in its documentation what that means for memory, start and durability,
   and keep the shared interfaces store-neutral. Judged by reading.
2. **Two taggregate-references to one taggregate are equal, or not, depending on which class they were first asked for
   as.** Asked for as a base class first, `Reference<ReviewAnimal>(id)` and `Reference<ReviewCat>(id)` are unequal, and a
   `HashSet` holds both; asked for as the cat first, they are the same object. `ITaggregateReference` says nothing about
   equality, and it is what code holds across units of work: 217 lines of JAStudio name it. Shown by running code.
3. **"State changes only by tevents" has two holes the spike's own API opens.**
   - `Taggregate.AssertCanBeChanged()` passes on a unit of work's copy, so a change made without a tevent is made, then
     silently lost at the commit, since a copy that published nothing isn't written; or saved with no tevent saying so,
     if anything else was published on the copy. JAStudio no longer calls it. Make it private. Shown by running code.
   - The restore window stays open after `RestoreFrom` returns, until the first publish, and a unit of work's copy got for
     update is exactly such an instance: code in the unit of work can `AddRestored` an entity without a tevent. Close the
     window when the restore returns. Shown by running code.
4. **A unit of work across a state store kept in files and anything durable, the tevent store or an exactly-once outbox,
   is not atomic across a crash, and no public documentation says so.** The state store is a volatile participant: the
   database commits first, then the files' journal is written. A crash, or the `Environment.FailFast` that any failure in
   the file commit causes, between the two leaves the database committed and the files not, and the next start deletes the
   unjournalled temporary files. Nor do the public docs say that a failed file commit stops the process, or that files
   aren't flushed to the disk. The spike made one unit of work across both stores a feature, so this is now a guarantee
   the API implies and doesn't give. Judged by reading.
5. **`ITeventObservation.ObserveTevents` hands runtime code a registrar whose `AtRuntime` always throws there.** Give the
   runtime a registrar type without it, so the mistake doesn't compile, and call the declaration `ObservableAtRuntime`.
   Judged by reading.
6. **`Taggregate` has a public parameterless constructor since the spike**: its protected constructor went, and the class
   isn't abstract. Add back a protected one. Shown by running code (reflection), and in the list of API changes.
7. **`TaggregateFiles.Of`'s `fileOf` takes the whole taggregate, so a file may depend on its state, which no consumer
   needs.** It brings moving a taggregate's file when its state changes, and a race the code names and doesn't guard: two
   concurrent units of work giving two taggregates the same new file. JAStudio's layout is the default. Take the id, or
   drop the parameter. Judged by reading.
8. **Smaller fixes to what the API promises:** `ReloadedTaggregates` lists with no order should be collections (rule 013);
   the 30-second waits of a reload and of the units of work during one are documented nowhere public; `Lookup` returns the
   index's own array, which a cast can change for every later lookup (shown by running code); an index exists once per
   pair of taggregate class and key type, which is documented nowhere; what a lookup costs inside a unit of work isn't
   documented; `IMutableTaggregateTeventWithIdentity` can be internal.

What I would add, against the grain of the rest: **an operation that rewrites every taggregate's storage**, as the tevent
store's `PersistMigrations` does for histories. JAStudio does it today with five lines of its own that must agree with
Compze's private layout and format (a `todo:compze:` at `CorpusNoteFiles.Write`), outside any unit of work, and to the disk
even when the files are kept in memory. A consumer can't do that as well itself: by rule 013 it belongs in Compze.

## 2. Every public API change

Verdicts: keep, keep with another shape, rename, make internal, remove, replace. "Plainly fine" entries get a line.

### Compze.Teventive, namespace `Compze.Teventive.TaggregateStores`

**`ITaggregateReference<out TTaggregate>`** (`src/Compze.Teventive/TaggregateStores/ITaggregateReference.cs:9`).
**Keep with another shape.** The right abstraction for every consumer of a store with private copies: it stands for the
identity, where an instance is one version, and it lets an index file references that a commit replaces only when keys
change. Three things to fix:
- **Equality.** The interface says nothing; the implementation compares only references of the same type argument
  (`src/Compze.Teventive/TaggregateStores/_internal/TaggregateReference.cs:25`), and the state store keeps the first
  reference made for an id and makes a new one when it can't be cast to the class asked for
  (`src/Compze.Teventive.StateStore/_private/TaggregateStateStore.cs:71`). So equality depends on the order in which code
  first asked. Shown by running code: `EndReview_demonstrations.two_references_to_one_cat`. Proposal: two references are
  equal when they stand for the same taggregate of the same store, whatever their type arguments, and the interface says
  so, since holders put them in sets and dictionaries.
- **What `Current` costs depends on the store, and the docs don't say.** In the state store it is a dictionary read. In the
  tevent store it builds an instance from the history at every read, so `reference.Current.A` and `reference.Current.B`
  are two instances and may be two versions.
- **The state store never forgets a reference** (`TaggregateStateStore.cs:22`): one for every id ever referenced,
  deleted taggregates included, for the life of the container. Bounded by the ids ever used; worth a line in the docs or
  a removal at the deletion's commit.

**`ITaggregateStoreReader`** (`ITaggregateStoreReader.cs:10`). **Keep, fix its documentation.** `Get`, `TryGet` and
`Reference`, shared by every store: right. Its summary (line 7) says a read outside a unit of work "costs no lock and no
copy", which is the state store's behaviour; the tevent store builds a new read-only instance from the history at every
`Get` (`src/Compze.Teventive.TeventStore/_private/TaggregateTeventStore.cs:48`). A shared interface describes what every
store gives: that the instance is read only, and that inside a unit of work it is the unit of work's own.

**`ITaggregateStoreUpdater`** (`ITaggregateStoreUpdater.cs:18`). **Keep, rename `Save` to `Add`, fix two guarantees.**
- `Save` only adds a new taggregate, in both stores; a change is saved by the commit. `Add` tells the truth
  (`ITaggregateStoreUpdater.cs:31`; the builders' own `review-naming:` says the same).
- The unit of work must have been begun by `ExecuteUnitOfWork`: a transaction begun by a `TransactionScope` of the
  caller's own is refused at the first get for update
  (`src/Compze.Teventive/TaggregateStores/_internal/TaggregateUnitsOfWork.cs:30`). That is a requirement on every caller,
  and only the exception message states it. Put it in the summary.
- `TryGetForUpdate` promises that "the unit of work holds its lock either way, so no other unit of work can add it before
  this one completes" (line 24). The state store keeps that promise. For the tevent store it depends on the SQL layer:
  SQL Server takes an application lock and PostgreSQL an advisory lock on the id, but MySQL's `FOR UPDATE` at read
  committed locks no row that doesn't exist. See section 7.
- Also: "one for each unit of work, resolved from its scope" is one for each scope; two scopes of one transaction share
  one unit of work. A word.

**`ITaggregateStoreBulkReader`** (`ITaggregateStoreBulkReader.cs:7`). **Keep; rename `All`** (section 5). An abstraction
any consumer has a use for, rightly apart from the reader, and rightly a collection with no order. In the state store
each call goes through `ConcurrentDictionary.Values` (`TaggregateStateStore.cs:106`), which takes every lock of the
dictionary and copies every committed taggregate of every class into a list before filtering by class; enumerating the
dictionary does neither. A `performance:` point, not an API one.

Internal and fine: `TaggregateUnitOfWork`, `TaggregateUnitsOfWork`, the routing, `TaggregateNotFoundException` (an
`ArgumentOutOfRangeException` is what a consumer can catch, and `TryGet` is what it should call).

### Compze.Teventive, namespace `Compze.Teventive`

**`ITeventRouted`** (`src/Compze.Teventive/ITeventRouted.cs:12`). **Keep.** Magnus chose the explicit implementation
(item 24) and it does what he asked: nothing shows in a taggregate's members. The costs to name once more for him, since
every consumer class pays them: CA1033 on every public unsealed consumer class, and a nullable `TeventAppliers` on an
interface whose null is only ever Compze's own "declares none". Neither is worth another shape.

**`ITeventRoutedMember<TTevent>`** (`ITeventRoutedMember.cs:9`). **Rename to `ITeventRoutedPart`.** Everything around it
says "part": its own summary ("A part of a class"), `TeventRouting.AssertRoutesToAPart`, `RoutesToAPartWhoseTeventsAre`,
the messages, the design. Its one member is `[Obsolete]` for internal use, the convention `ITeventiveInternals` already
follows; fine.

**`TeventRouting`, `TeventRouting<TInstance, TTevent>`, `Extend`, `Declaration` with `For`, `Component`, `Entities`,
`SharedTomponent`, `IgnoreUnhandled`** (`src/Compze.Teventive/TeventRouting.cs:28`, `:74`, `:104`, `:143`). **Keep.** A
declared, per-class table is general, cheap per instance, and loud about its mistakes. Its first table only from a base
class's protected `DeclareAppliers` is right. One gap for consumers: the published docs under
`src/Compze.Teventive/Taggregates/_docs` don't teach any of it yet (the `urgent:` at `TeventRouting.cs:20` says so).

**`IMutableTaggregateTeventWithIdentity`** (`src/Compze.Teventive/IMutableTaggregateTeventWithIdentity.cs:7`). **Make
internal.** Its one member is for internal use, and its one caller, `TeventStore.cs:112`, is in an assembly that
Compze.Teventive already grants its internals. `TaggregateTeventWithIdentity` can implement an internal interface
explicitly. What JAStudio does instead: nothing, it doesn't use it. (`IMutableTaggregateTevent` is the same, from before
the spike.)

### Compze.Teventive, namespace `Compze.Teventive.Taggregates`

**`ISnapshotable<TSelf, TSnapshot>`** (`src/Compze.Teventive/Taggregates/ISnapshotable.cs:18`). **Keep.** Snapshots of a
type of the class's own, restored into an empty instance with no tevents: general, and in Compze.Teventive rightly, since
a domain class shouldn't depend on a store. `TSelf` appears in no member; it exists so that `StateStoreBuilder.Taggregate`
can infer the class from the factory. Fine, but say so in the doc comment, or a reader looks for its use. Its remarks are
good and should keep the warning about shared objects changed in place.

**`SnapshotRoundTrip`** (`SnapshotRoundTrip.cs:20`). **Keep `DifferencesOf`, remove `AssertLosesNothing`.** The check
itself is something a consumer can't build as well: it makes the store's own copy through the internal
`SnapshotableTaggregateClass`, and compares instances field by field. But `AssertLosesNothing` is
`if(DifferencesOf(...).Any()) throw`, which rule 011 keeps out of a framework. JAStudio calls only `DifferencesOf` (its
corpus check, `CopiesOfTheNotes.cs:33`). What a consumer does instead: assert the list is empty with its own assertion
library, one line. Compze's own specifications would do the same. The differences are strings; a type of their own would
be better if a tool ever needs the path apart from the text, not now.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates.BaseClasses`

**`Taggregate<…>`** (`src/Compze.Teventive/Taggregates/BaseClasses/Taggregate..cs:13`):
- **The public parameterless constructor: replace with a protected one.** The protected constructor that took a
  `TeventDispatcherConfig` went, the class isn't abstract, so the compiler made a public one. Constructing it throws at
  run time (no routing declared), but it compiles. Shown by running code:
  `EndReview_demonstrations.the_taggregate_base_class`.
- **`AssertCanBeChanged()`** (line 73): **make private.** It exists for state changed other than by tevents, which a
  taggregate in a store with private copies can't keep safely: on a copy the assertion passes, the change is made, and the
  commit drops it if nothing was published, or saves it with no tevent if anything was. Shown by running code:
  `EndReview_demonstrations.a_change_made_without_a_tevent_on_a_copy_got_for_update`. Its only call left is in `Publish`.
  JAStudio no longer calls it (none in its `main`), so it costs JAStudio nothing. Magnus's rationale for rule 012 names it
  among the APIs added for JAStudio.
- **`RestoreIdentity(TaggregateId)`** (line 93): **keep, and close the restore window when the restore returns.** Today
  the window lasts from `RestoreIdentity` until the first publish (lines 100, 106, 182). The store's copy for a unit of
  work is built by `SnapshotableTaggregateClass.Restore` (`src/Compze.Teventive/Taggregates/_internal/SnapshotableTaggregateClass.cs:65`),
  and handed out in that window, so code in the unit of work can add an entity without a tevent, and the commit writes it
  only if something else was published. Shown by running code:
  `EndReview_demonstrations.adding_a_restored_entity.after_the_restore_has_returned_and_before_any_publish_is_accepted`.
  Simplest fix: `SnapshotableTaggregateClass.Restore` ends the window after `RestoreFrom` returns, through
  `ITaggregateStoreInternals`, which covers every load and every copy; a restore outside any store keeps today's rule.
- **`DeclareHandlers`** (line 37): **keep.**
- **Deriving from `TeventApplyingRoot`, implementing `ITeventRouted`: plainly fine.**

**`TeventApplyingRoot<TRoot, TTevent>`, `TeventApplyingComponent<TComponent, TComponentTevent>`**
(`TeventApplyingRoot.cs:15`, `TeventApplyingComponent.cs:11`). **Keep; rename in the naming pass.** Public only because
`Taggregate` and `SelfGeneratingQueryModel` derive from them; nothing outside Compze can (internal constructors). They do
share the code Magnus wanted shared. `AssertInvariantsAreMet` moved down to the root: callers unaffected.

**`IEntityCollectionManager.AddRestored(id)`** (`_IReadOnlyEntityCollection.cs:27`), **`SharedTentityCollection.AddRestored`**
(`Shared/SharedTentityCollection.cs:39`). **Keep,** with the restore window closed as above. It is fixing the broken API
Magnus filed, not adding to it: without it no taggregate with entities can be snapshotted.

**`Tentity`, `TeventiveComponent`, `TeventiveRemovableEntity`, the shared classes: their new constructors and protected
`DeclareAppliers`.** **Keep.** The per-instance registration they replace is gone, which is the point.
`SharedTentityCollection` sealed: right.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`

**`SharedTomponentSlotOfAClass<TOwner, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>`**
(`Shared/SharedTomponentSlotOfAClass.cs:33`). **Rename to `SharedTomponentSlot`, and type its constructor's second
parameter as what it asserts.** "Of a class" told it apart from the per-instance `SharedTomponentSlot`, which is gone, so
it now tells nothing. Its constructor takes `Func<TOwner, ISharedTomponentInternals<TTomponentTevent>>` (line 48) and
then asserts at run time that what it gets is a `SharedTomponent<TTomponentTevent>` (line 71): take
`Func<TOwner, SharedTomponent<TTomponentTevent>>`, and `ISharedTomponentInternals` leaves the signature consumers write.
Five type arguments per slot is a cost every slot pays; the design noted a nested slot type could cut it to two.

**`SharedTomponentSlotInAnOwner<TTomponentTevent>`** (line 11). **Keep.** A ref struct, allocates nothing, can't be kept.

**`ISharedTomponentInternals.ApplyAdoptedTeventInternal`**: **keep,** in the established `[Obsolete]` internals pattern.

### Compze.Teventive, namespace `Compze.Teventive.Taggregates.Tevents`

**`ITaggregateTeventWithIdentity`, `TaggregateTeventWithIdentity`; `ITaggregateTevent`, `ITaggregateCreatedTevent`,
`ITaggregateDeletedTevent`, `TaggregateTevent` no longer exactly-once, `TaggregateTevent.Id` moved.** **Keep.** Magnus's
decision 9, and the types now say what is true. One consequence for consumers to put in the changelog plainly: a hierarchy
that relied on being remotely delivered now isn't, silently, unless it declares its delivery guarantee itself; nothing
fails at compile time or at composition.

**`TaggregateIdentifyingTevent.WrapIn(Type, ITaggregateTeventWithIdentity)`** (`TaggregateIdentifyingTevent.cs:17`).
**Keep**: what a migration author needs now.

### Compze.Tessaging, namespace `Compze.Tessaging.TessageBus`

**`ITeventObservation.ObserveTevents(Action<TeventObservationRegistrar>)`** (`src/Compze.Tessaging/TessageBus/ITeventObservation.cs:30`).
**Keep with another shape; rename.** Runtime observation is the bus's own capability with a shorter lifetime; JAStudio's
views and Deskmancer's awaits both need it, and neither could do it as well without re-implementing routing by type. The
shape: it hands runtime code the composition's full `TeventObservationRegistrar`, whose `AtRuntime` always throws at
runtime (`TeventObservationRegistrar.cs:78`). Give the runtime a registrar interface with `ForTevent` and
`ForEachUnitOfWork` only, which the composition's registrar extends with the declaration. Declaration methods that don't
declare can then be typed on the smaller interface and used in both places, which is why the full registrar was handed
over. Names in section 5.

**`ITeventObservationRegistrar.AtRuntime<TTevent>()`, `TeventObservationRegistrar.AtRuntime<TTevent>()`**
(`ITeventObservationRegistrar.cs:30`). **Keep; rename to `ObservableAtRuntime<TTevent>()`.** It doesn't observe anything
at runtime: it declares a type observable then, and subscribes the endpoint to it. The rule that every runtime observer's
types are declared, in a plain container too, costs JAStudio four lines and keeps compositions alike; keep.

**`ForEachUnitOfWork<TTevent>`** on the interface and the class (`TeventObservationRegistrar.cs:57`). **Keep.** Nothing on
a tevent marks where its unit of work ends, so a consumer can't batch as well itself. An `IReadOnlyList`, since the order
published is promised: right by rule 013.

**The convenience overloads added to `ITeventObservationRegistrarCE` (four) and `TeventObservationRegistrarCE` (six).**
**Keep,** for symmetry with `ForTevent`'s, which have the same shapes. By rule 011 they are conveniences a consumer could
write, but having them for one verb and not its sibling would surprise more. Add no more.

### Compze.Tessaging, namespace `Compze.Tessaging.Engine`

**`ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()`** (`src/Compze.Tessaging/Engine/ILocalTessagingEngine.cs:26`).
**Keep.** Only the engine knows when its observers are done; every consumer's specifications that assert on an observer
need it. The name uses the existing term "in flight".

### Compze.Teventive.TeventStore

**`TeventStoreBuilder.Taggregate<TTaggregate>()`, `TeventStoreRegistrar.TeventStore(…, declare)` and
`TeventStoreForFlexibleTesting(…, declare)`** (`src/Compze.Teventive.TeventStore/Wiring/TeventStoreBuilder.cs:21`,
`TeventStoreRegistrar.cs:24`). **Keep.** The constraint `ITaggregate<ITaggregateTeventWithIdentity>` puts the store's
requirement in the type. `declare` being optional lets `TeventStore("connection")` compose a store whose updater keeps no
class, failing at the first `Get`; acceptable, since `ITeventStore` alone still has uses.

**`TeventStore`'s and `ITeventStore`'s signatures with `ITaggregateTeventWithIdentity`; `GetTaggregateHistory`,
`GetTaggregateHistoryForUpdate`, `StreamTevents`, `SaveSingleTaggregateTevents`, the migration interfaces,
`TeventMigration`'s constraint, `ListAllTeventsForTestingPurposes…`.** **Plainly fine**: they follow decision 9.

**The query models: `SelfGeneratingQueryModel` and its `Component`, `Entity`, `Entity.ICollectionManager`, the removed
nested classes and collections.** **Keep.** One abstraction implemented once, as Magnus wanted. A detail:
`Component(ITeventRouted parent)` (`SelfGeneratingQueryModel..Component.cs:18`) takes any routed object, only to assert
that it routes to the component; the old `TQueryModel` parameter at least said what the parent is. Minor.

### Compze.Teventive.TeventStore.Abstractions

**`ITeventStoreUpdater` removed, `GetReadonlyCopy` removed.** **Plainly fine.**

**`ITeventStoreReader : ITaggregateStoreReader`** (`src/Compze.Teventive.TeventStore.Abstractions/ITeventStoreReader.cs:11`).
**Keep with another shape: don't extend the shared reader.** Its `Get` "reads every store" (line 9), so an
`ITeventStoreReader` hands out the state store's taggregates: the name tells less than the truth, and there are two ways to
call the same `Get`. What only it can do is `GetHistory` and `GetReadonlyCopyOfVersion`; code that wants a taggregate
depends on `ITaggregateStoreReader`. The structure was Magnus's item 22, decided while the reader was the tevent store's
own; I'd ask him again now that the shared reader serves every store.

### Compze.Teventive.TeventStore.Typermedia

**The constraint changes on `SaveTaggregate`, `GetReadonlyCopyOfTaggregate`, `GetReadonlyCopyOfTaggregateVersion`,
`GetTaggregateHistory`, `TaggregateLink`.** **Plainly fine.**

### Compze.Teventive.StateStore, namespace `Compze.Teventive.StateStore` (new)

**`StateStoreRegistrar.StateStore(…)`** (`src/Compze.Teventive.StateStore/StateStoreRegistrar.cs:16`). **Keep; rename
with the store** (section 3).

**`StateStoreBuilder`** (`StateStoreBuilder.cs:17`):
- **`Taggregate<TTaggregate, TSnapshot>(createEmpty, snapshotsAreEqual)`** (line 35): **keep.** The factory is general;
  the inference from `ISnapshotable` is neat.
- **`Taggregate<TTaggregate, TSnapshot, TService>(…)`** (line 42) and **`StoredInFiles<TService>(…)`** (line 75):
  **keep, with a question for Magnus.** They are the only way for a declaration to use a service of the container, and
  their shape matches Compze's `CreatedBy((TService service) => …)`. But they are per member: every future declaration
  that needs a service would need its own overload. The question: would one way for the whole declaration to be given
  services be better? And behind `Taggregate(… TService)` is JAStudio's notes holding `NoteServices`, a wrapper of the
  root resolver through which a note reaches the collection, the dictionary and the configuration. A taggregate reaching
  other taggregates through a service is what the private copy makes stale ("a cache computed from other taggregates goes
  stale"), and the tevent store, which builds taggregates through a parameterless constructor, can't keep such a class at
  all. I would keep the overload, since a factory needing a service is general, but say in its doc that a taggregate's
  state must be its own.
- **`Index<TTaggregate, TKey>(keysOf)`** (line 56): **keep, document that it exists once per pair of class and key
  type.** It registers `ITaggregateIndex<TTaggregate, TKey>` in the container (line 59), so a second index of the same
  pair can't exist; JAStudio made a key type for each of its ten vocab indexes (`VocabIndexes.cs:23`). Typed keys are
  better code anyway, so the constraint is fine; it just has to be said.
- **`StoredInFiles(folder, options, declare)`** (line 69): **keep, document three guarantees:** a failure while
  committing the files stops the process (`src/Compze.Teventive.StateStore/_private/Files/PreparedFileWrite.cs:76`); the
  files aren't flushed to the disk, so a commit survives the process, not the machine; and a unit of work that also
  writes a database isn't atomic across a crash (section 4).

**`IStateStore`** (`IStateStore.cs:8`). **Keep with another shape: one reload member.**
`ReloadFromStorage()` is `ReloadFromStorageAfter(() => {})` (`TaggregateStateStore.cs:82`): rule 011's convenience a
consumer writes itself. One member, `ReloadFromStorage(Action? changeToTheStorage = null)`, or only the `After` form.
What JAStudio does: nothing, it calls only `ReloadFromStorageAfter` (`JPCollection.cs:314`). Two guarantees to document:
- a reload waits at most 30 seconds for the open units of work, and a unit of work that begins during a reload waits at
  most 30 seconds for it (`_private/UnitsOfWorkAndReloadsGate.cs:12`), each then throwing. JAStudio's cold start without
  its cache took 37 s;
- what it costs: two snapshots and a comparison of every taggregate, to report `Changed`, and a snapshot type without
  value equality or a declared comparison makes it throw.

**`ReloadedTaggregates`** (`IStateStore.cs:27`). **Keep with another shape:** `Added`, `Changed` and `Removed` are
`IReadOnlyList<TaggregateId>` in no order (dictionary order), which rule 013 says is an `IReadOnlyCollection`. JAStudio
only converts them (`JPCollection.cs:279`). Rename in section 5.

**`ITaggregateIndex<out TTaggregate, in TKey>`** (`ITaggregateIndex.cs:9`). **Keep, fix two things.**
- **What a lookup costs inside a unit of work isn't in its documentation:** in a unit of work holding any copy of the
  class, even one got for update and unchanged, each `Lookup` builds the key's whole group anew and runs the key function
  on every held copy, and `HasAny` builds the whole group to count it (`_private/TaggregateIndex.cs:62`). And each commit
  that adds a taggregate to a key or takes it out copies that key's whole array: for a key that files most of a store,
  such as a tag, every such commit costs in proportion to the store. The remarks say only that a lookup outside a unit of
  work allocates nothing.
- **`Lookup` returns the index's own array** (`_private/TaggregateIndex.cs:47`), so a cast back to an array lets a caller
  change what every later lookup finds. Shown by running code: `EndReview_demonstrations.a_lookup_outside_a_unit_of_work`.
  A consumer that casts a read-only collection is misbehaving, and wrapping each array costs an object per key, which the
  spike measured its way out of; I would leave it and say so in the doc comment, unless Magnus wants it closed.

**`ITaggregateFilesInMemory`** (`ITaggregateFilesInMemory.cs:10`) and **`TaggregateFiles.KeptInMemory()`**
(`TaggregateFiles.cs:41`). **Keep; a question about where.** Every consumer of the JSON store wants fast specifications,
and the files in memory run the real layout, journal and cache, which a fake storage wouldn't. But it is a testing
surface in the production package. Rule 005's shipped testing package (as `Compze.Tessaging.Hosting.Testing`) would hold
it, plugged in through the internal `IFileSystem` as the MemoryPack cache plugs in. The cost to JAStudio: its test
registrar lives in `JAStudio.Core` (`TestEnvironmentSpecificDependenciesRegistrar.cs:48`), so its production assembly
would reference the testing package, which weakens the case. Magnus's call; I lean to leaving it.

**`TaggregateFiles.Of<TTaggregate>(folder, fileOf)`** (`TaggregateFiles.cs:24`). **Keep with another shape:** `fileOf`
should take the id, not the taggregate, or go. A file computed from state brings moving the file when the state changes
it, and a race the docs name as not guarded: two units of work committing at once whose taggregates get the same new file.
JAStudio needs only the id, and its `FileIn` is the default layout written out again (`CorpusNoteFiles.cs:89`). Magnus's
"specified per note type but built per note" is met by a function of the id. What JAStudio does instead: drops its
`fileOf` arguments, or passes one of the id.

### Compze.Teventive.StateStore.MemoryPack (new)

**`TaggregateFilesMemoryPackCE.CachedWithMemoryPack()`** (`src/Compze.Teventive.StateStore.MemoryPack/TaggregateFilesMemoryPackCE.cs:16`).
**Keep; it reads as per class and is per store.** `files.Of<VocabNote>("vocab").CachedWithMemoryPack()`, as its own doc
and the package README write it, reads as caching the vocab; it caches every class. The same is true of `KeptInMemory()`.
Either put store-wide options before the classes in every example, or give them a place that can't read as per class
(section 5). The separate package, for a third-party dependency that needs `[MemoryPackable]` on the consumer's types: right.

### Nothing missing from the list, as far as I can see

I compared the list with `git diff main...taggregate-store-spike -- src`: every public change in the source is in it. What
it can't show, being a list of types: the behaviour changes behind unchanged signatures, which matter as much. `ExecuteUnitOfWork`
now records the unit of work that began a transaction, and the stores refuse transactions begun any other way; the tevent
store's updater is now one per transaction, not per scope; a failed publish now rolls a tevent store unit of work back even
if caught; a tevent store keeps a deleted taggregate's history and treats it as absent. And one stale text: the state
store's package description still says "or behind a narrow seam for durable storage"
(`src/Compze.Teventive.StateStore/Compze.Teventive.StateStore.csproj:5`); the seam is internal.

## 3. The all-in-memory question

**Yes: the state store is designed for an application that keeps its whole database in memory, and it is a store of its
own, not a mode of something more general.**

What makes it memory-resident, from the code:
- **It loads everything, and only everything.** The storage seam's one read is `LoadAll()`; there is no load by id. The
  store fills itself at its first use of any kind, a read, a lookup or a session (`TaggregateStateStore.EnsureLoaded`, line
  136), on whatever thread that is, which takes JAStudio 4 s from the cache, 7.7 s from warm files and 37 s cold.
- **Its reads are free because everything is loaded:** a committed instance is a dictionary read, lock-free.
- **Its indexes are correct only because everything is loaded,** as Magnus assumed. `FileExactly` files every loaded
  taggregate; a lookup returns references to resident instances; a commit refiles from the committed instance's keys. An
  index of a store that loads on demand would have to be kept by the storage, as a database keeps one.
- **`All` enumerates what it holds; a reload replaces everything it holds.**

Why not a mode: a store that loads on demand would need a seam that loads by id and by key, an index the storage keeps,
locks for taggregates that aren't loaded, and reads that cost a load. That is the tevent store's shape with snapshots in
place of histories, such as snapshots in Compze's document db: another store, sharing with this one only what is already
shared, `ISnapshotable`, `ITaggregateStoreReader`, `ITaggregateStoreUpdater` and `ITaggregateReference`. Making residency
a mode would split nearly every class of the state store in two for a store nobody has asked for.

What it means:
- **The name.** "State store" says what it stores, state rather than history; it hides how it holds it, which decides its
  memory, its start and which features it can have. Propose `MemoryResidentStateStore`, with `IMemoryResidentStateStore`
  and the registrar `MemoryResidentStateStore(...)`, keeping "state store" as the kind, so that a store of snapshots
  loaded on demand can later be another state store. "In-memory" is the obvious term but the wrong one: in .NET it reads
  as "not durable" (EF Core's in-memory provider, Compze's own `SqliteMemory`), and the store already uses "in memory" for
  having no storage and for `KeptInMemory`. If Magnus expects no second state store, keeping the name and saying it in the
  first line of every doc is the cheaper alternative.
- **The public surface.** The residency-only parts are already in the store's own package: `ITaggregateIndex` with
  `StateStoreBuilder.Index`, `IStateStore` and `ReloadedTaggregates`. What leaked into the shared interfaces is the
  reader's "costs no lock and no copy" (section 2). Nothing else needs to move.
- **The documentation.** The registrar's and the package's docs should say: every taggregate is held in memory for the
  container's life, so the memory is the whole data set (JAStudio: about 730 MB of heap for 80,000 notes); the store loads
  everything at its first use, on the thread of that use, so call a reload at start to choose when, as JAStudio does with
  `ReloadFromStorageAfter`; its storage makes commits durable and is never read again but by a reload.

## 4. The store design, topic by topic

### Taggregate-references

Sound, and the right thing to hold across units of work; the index's design rests on it. To fix: equality (section 2,
shown by running code), and the docs on what `Current` is in each store. Inside a unit of work, a reference gives the unit
of work's own copy; outside, the committed instance; after a deletion, it throws: all as documented, and the right
semantics. JAStudio's lookups and bulk reads turn references into instances at once (`NoteIndex.cs:43`,
`StoredNotes.cs:37`), so the reference costs it a call per result and buys it nothing there; the index's need for
references, not the caller's, is what justifies returning them, and it does.

### Indexes defined by state

Sound for a memory-resident store: correct inside and outside a unit of work, cheap outside, and the key function's
contract partly asserted at prepare. What the spike measured and fixed (no record of keys, no sort) was right. Left:
- the costs inside a unit of work and at a commit of a large key's group, not in the public docs (section 2);
- **between a commit's swap of instances and its refiling, a reader sees the new instance under its old keys**
  (`StateStoreSession.Commit` makes the copies committed, then applies the index changes). The design lists atomic
  visibility as open; a lookup by an old key then returns a taggregate whose current state no longer has that key. Worth
  a sentence in `ITaggregateIndex`'s remarks;
- one index per pair of class and key type, not documented.

### The storage seam

Internal now, which is right by rule 011: Compze's JSON store covers what a consumer's storage was for, and the seam was
shaped by JAStudio's files. Its one read, `LoadAll`, is the residency. The JSON store itself is careful: files written
whole under temporary names at prepare, a journal renamed into place as the commit, a start that finishes or discards. What
it doesn't give and doesn't say:
- **atomicity with anything durable.** A volatile participant commits after the durable ones; with the tevent store, or
  exactly-once outbox rows, in the same unit of work, a crash or a `FailFast` between the database's commit and the
  journal leaves the database committed and the files not, and the next start deletes the unjournalled files. For the
  exactly-once guarantee this means a peer can receive tevents about a change that was lost. Either refuse a unit of work
  that holds both a state store kept in files and a durable resource, or document it on `StoredInFiles` and on the shared
  updater. A real fix, a durable enlistment with recovery, is an effort of its own;
- the `FailFast`, and no flush to the disk (section 2);
- `fileOf` computed from state (section 2).
- Noticed by the builders and agreed: a commit of one file needs no journal.

### Dispatching tevents synchronously

The right call, as Magnus decided: handlers run in the domain call once the taggregate's cascade has ended, a failure comes
out of the call and marks the taggregate, and the unit of work then refuses to prepare. The hand-out queue and `Commit`
taking its tevents out first fix the reentrancy bugs without deferring. Two things to know:
- the order is depth first: a handler's tevents on another taggregate are dispatched before the first taggregate's next
  tevent of the same cascade. Deterministic, and what observers see; worth one sentence where the order is promised;
- an exactly-once tevent is published through a synchronous bridge, `PublishAsync(...).GetAwaiter().GetResult()`
  (`src/Compze.Teventive/TaggregateStores/_internal/TaggregateUnitOfWork.cs:63`), as the tevent store already does. Compze
  awaits with `caf()`, so it doesn't deadlock a UI thread, but it blocks one thread per publish while the outbox writes.
  Fine for now, deliberate, and documented at the code.

### The tevent store taking the same internals

Sound: one taggregate unit of work per transaction, a session per store, the routing by declared class, one updater and
one reader for both. The tevent store keeps its own dispatch, writes and database locks. To fix or say: the shared
reader's and updater's guarantees that only the state store keeps (sections 2 and 7); `ITeventStoreReader` extending the
shared reader (section 2); the atomicity across the two stores when the state store has files (above). That a tevent
store's taggregate classes must now be declared, and its transactions begun by `ExecuteUnitOfWork`, are changes every
consumer of the tevent store meets: the changelog should lead with them.

### The bulk reader

Right: its own interface, so code that scans says so; a collection with no order; the unit of work's own view. The tevent
store's `All` is a scan of creation tevents plus a query of deleted ids at every call, as its doc says. Rename `All`
(section 5).

### Also

- **The lock for each taggregate** waits 30 seconds, then throws a `TimeoutException` naming the taggregate; kept as built
  by Magnus (item 17). The rule that a unit of work must not wait for the UI thread while holding a lock is JAStudio's to
  keep, and worth a sentence on `GetForUpdate`.
- **A copy through the snapshot costs in proportion to the taggregate's whole state,** at every get for update. Fine for
  JAStudio's notes; a taggregate with thousands of entities pays it at every change. Worth a sentence on `GetForUpdate`.

## 5. Names I would change

| Now | Proposal | Why |
|---|---|---|
| `StateStore(...)`, `IStateStore`, `StateStoreBuilder`, `StateStoreRegistrar` | `MemoryResidentStateStore(...)`, `IMemoryResidentStateStore`, `MemoryResidentStateStoreBuilder`, `MemoryResidentStateStoreRegistrar` | Section 3. Or keep, and say it in every first line. |
| `ITaggregateStoreUpdater.Save` | `Add` | It only adds. |
| `ITaggregateStoreBulkReader.All<TTaggregate>()` | `ReferencesToAll<TTaggregate>()` | It gives references, beside `Reference<TTaggregate>(id)`; `All` reads as instances. |
| `ITeventRoutedMember<TTevent>` | `ITeventRoutedPart<TTevent>` | Everything around it says part. |
| `SharedTomponentSlotOfAClass<…>` | `SharedTomponentSlot<…>` | The per-instance slot it was told apart from is gone. |
| `TeventObservationRegistrar.AtRuntime<TTevent>()` | `ObservableAtRuntime<TTevent>()` | It declares, it doesn't observe. |
| `ITeventObservation` | `IRuntimeTeventObservation` | A service for observing at runtime; "observation" alone is also what composition declares. |
| `ReloadedTaggregates` | `WhatTheReloadFound` | It is the differences a reload found, not taggregates. |
| `ReloadFromStorage()` and `ReloadFromStorageAfter(change)` | `ReloadFromStorage(Action? changeToTheStorage = null)` | One member (section 2). |
| `TaggregateFiles.KeptInMemory()`, `CachedWithMemoryPack()` | keep the names; write them first: `files => files.KeptInMemory().CachedWithMemoryPack().Of<VocabNote>("vocab")` | They are store-wide; after an `Of` they read as per class. |
| `SnapshotRoundTrip.DifferencesOf` | `SnapshotRoundTrip.WhatACopyLoses` | Says what the differences are. |
| `TeventApplyingRoot`, `TeventApplyingComponent` | keep | Plain and true; "applying" is what they share. |
| `ITaggregateFilesInMemory`, `TaggregateFiles`, `Of`, `fileOf`, `StoredInFiles` | keep | Each tells the truth. |

## 6. Where I disagree with the builders' views in the README's table

Written after sections 1 to 5, having read the table's last column, `bugs-and-findings.md` and the earlier reviews.

- **`Taggregate.AssertCanBeChanged()`: "a transitional API".** It goes now, and not because JAStudio has finished with it
  (it has: its `main` no longer calls it), but because it isn't safe even as a transition: on a unit of work's copy it
  passes, and the change is then lost or saved without a tevent, whichever else the unit of work does. Shown by running
  code.
- **`AddRestored`: "allowed only while the taggregate is being restored, from `RestoreIdentity` until its first
  publish".** That window is too wide: a copy got for update is in it until it first publishes. It should close when the
  restore returns. Shown by running code.
- **`StoredInFiles`, `TaggregateFiles.Of(folder, fileOf)`: the row treats the JSON store as settled once the seam went
  internal.** `fileOf` computed from the taggregate's state is speculative, with a race the code names and doesn't guard;
  narrow it to the id.
- **`ITeventObservation.ObserveTevents`: approved as it is.** Approved as a capability, rightly; its shape hands runtime
  code a declaration that always throws there. Give the runtime a registrar without it.
- **`IStateStore.ReloadFromStorage()`: "should a consumer whose storage another program changes compose a new store?"**
  No: for a memory-resident store, reloading is its job. A new store means new references, new indexes and, since the store
  is a singleton of its container, a new container: everything a consumer holds would be replaced, which references exist
  to avoid. Keep the reload, as one member with an optional change, which is where taggregates-opus's view of
  `ReloadFromStorageAfter` leads too. `snapshotsAreEqual` stays with it, since reporting `Changed` needs it.
- **`StateStoreBuilder.Taggregate(...)` with a service, and `StoredIn<TService>` (now `StoredInFiles<TService>`): kept
  as DI factories are.** Agreed, with two additions: one `TService` overload per member multiplies as members are added,
  which is worth a decision on one way for a whole declaration to get services; and a factory's service shouldn't be how a
  taggregate reaches other taggregates, which the doc should say.
- **`TeventRouting` made only through the base classes' `DeclareAppliers`, hidden with `new`: the row asks whether it is
  the right shape.** It is: it is the one way a class's first table can start with its base classes' routes, a static
  can't be virtual so hiding is the tool, and the call site reads the same in every class. `SharedTomponent`'s type
  argument is the price of its having no self type, and smaller than adding one.
- **`AtRuntime` and the rule that every runtime observer's types are declared: Magnus asked for it explained plainly.** An
  endpoint tells its peers, when it starts, which tevent types to send it, from what its composition declares. An observer
  created later can't add to that without a round trip, and would miss what comes meanwhile. So an observer of a type
  nobody declared would get the tevents published in its own process and, silently, none from other processes. The rule
  makes `ObserveTevents` fail at once for such a type, in every composition, a plain container's too, so the mistake shows
  in the first specification rather than in a deployment with endpoints. The cost is a line per root type, four in
  JAStudio. Keep the rule; rename the declaration `ObservableAtRuntime`, since it declares and observes nothing.
- **Agreed as the table has them:** the bulk reader as its own interface; `Lookup` with no order; `TaggregateToDelete`
  gone; `ForEachUnitOfWork` with no information about the unit of work; `Delete` removed; registration per instance
  removed; the query models' parts keeping no parent; `TeventApplyingRoot` and `TeventApplyingComponent` (I would keep
  those names).
- **Rows with no view yet:** `SnapshotRoundTrip.DifferencesOf`: keep it, and remove `AssertLosesNothing` instead;
  `AwaitNoTessagesInFlightAsync`: keep.
- **Not in the table, raised here:** the equality of taggregate-references; the atomicity of a unit of work across a store
  kept in files and a durable resource; `Taggregate`'s public constructor; the shared reader's documented costs;
  `ITeventStoreReader` extending the shared reader; `ReloadedTaggregates` as lists; the 30-second waits;
  `IMutableTaggregateTeventWithIdentity`; and an operation to rewrite every taggregate's storage.

**Where the documents and the code differ**, since the brief asks: the README's "The answer" still says the store
"dispatches tevents ... when the unit of work commits, never from inside a domain method" and that the reentrancy bugs
"can't arise: nothing calls out from inside `Publish` or `Commit`" (`README.md:104`, `:108`); dispatch is synchronous,
inside the domain call, and the bugs are fixed by the hand-out queue and `Commit` taking its tevents out first.
`bugs-and-findings.md` still says the work is "design, with no change to Compze" (line 4) and that the bugs are fixed by
dispatching "at the commit ... from the unit of work's queue" (line 49), and proposes that a taggregate refuse further use
after an exception, which the rollback replaced. `the-taggregate-store.md` says the store "keeps one reference for each
taggregate" (its section on derived state); it keeps one for each id, and makes another each time the id is asked for as
a class the first reference can't be cast to.

## 7. What I am unsure of, and what I couldn't verify

- **The atomicity gap across a durable resource and a store kept in files** is judged by reading: by System.Transactions'
  order, in which a volatile participant's commit comes after the durable resource's, and by the code's own `FailFast` and
  its deleting of unjournalled files at the start. I didn't crash a process to show it.
- **`TryGetForUpdate`'s lock for a taggregate that doesn't exist, in the tevent store on MySQL:** by reading the SQL layers
  (SQL Server takes `sp_getapplock`, PostgreSQL `pg_advisory_xact_lock`, MySQL only `FOR UPDATE`) and from what I know of
  InnoDB at read committed, which locks no gap. Not run: the database servers are on threadripper and other sessions use
  them. The consequence would be a failure on the duplicate version when two units of work add the same id, not
  corruption.
- **`ConcurrentDictionary.Values` taking every lock and copying** is from what I know of its implementation, not measured
  here.
- **That `ISnapshotable`'s `TSelf` is needed for `StateStoreBuilder.Taggregate`'s inference** I believe, and didn't test by
  removing it.
- **The synchronous bridge for exactly-once tevents not deadlocking a UI thread** rests on Compze awaiting with `caf()`
  throughout; I didn't check every await under `PublishAsync`.
- **Names** in section 5 are proposals of taste; `MemoryResidentStateStore` is the one I'd argue for, and only if Magnus
  expects another state store one day.
- **What I didn't do:** run the full suite (my specifications ran on the two default combinations, SQLite in memory with
  Microsoft's container, over ASP.NET Core and over named pipes); run JAStudio; read `what-was-measured.md` and
  `generating-tevent-classes.md`; read the earlier reviews beyond their headings and conclusions.
