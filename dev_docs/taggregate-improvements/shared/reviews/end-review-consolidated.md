# The end review, consolidated: what is fixed without asking, and the questions for Magnus

By jastudio-rep-opus, 2026-11-18, from [the Opus reviewer's review](end-review-by-an-opus-agent.md) and
[the Fable reviewer's](end-review-by-a-fable-agent.md), both on the spike at 46cc881a6. "Run" means a reviewer's
specification shows it, on `end-review-opus` (8bcca9817) or `end-review-fable` (79fc29ab7); "read" means judged from
the code.

**Both reviewers agree on the large things:** most of the spike is API Compze would have for any consumer; the state
store holds everything in memory by construction and is a store of its own, not a mode of a general one; the shared
reader's and updater's documentation promises what only the state store keeps; and a unit of work across a state store
kept in files and a database isn't atomic across a crash.

## 1. Defects: fixed without asking, each with a specification

Each is behaviour that contradicts what the API says or the spike intended, shown by running code. None changes a
signature but the first, which undoes an accident.

| # | Defect | Found by | The fix |
|---|---|---|---|
| D1 | `Taggregate<…>` has a public parameterless constructor: the spike removed its protected one, and the class isn't abstract. `new Taggregate<…>()` compiles, and throws. | both, run | A protected parameterless constructor. |
| D2 | Two taggregate-references to one taggregate are equal or not depending on which class was asked for first; a `HashSet` holds both. | both, run | References are equal when they stand for the same taggregate of the same store, whatever class they were asked for as. `ITaggregateReference` says so. |
| D3 | `AddRestored` accepts an entity after the store's restore has returned, until the first publish: a copy got for update is in that window. | Opus, run | The store ends the window when `RestoreFrom` returns, for every load and every copy, and `SnapshotRoundTrip` does the same for its copy. |
| D4 | `TryGetForUpdate` of an id the store doesn't hold, then `Save` or a second `TryGetForUpdate` of it in the same unit of work, waits 30 seconds for its own lock and throws, blaming another unit of work. | Fable, run | A unit of work takes each id's lock once. |
| D5 | A new taggregate saved again after its unit of work rolled back is committed with no tevent dispatched: no handler or observer hears of its creation. | Fable, run | The state store refuses that save, as the tevent store does. |
| D6 | `ReloadFromStorage()` on a state store with no storage removes every taggregate and reports each as removed. | Fable, run | Refused. |
| D7 | An index of a base class beside one of a subclass with the same key type: resolving the first throws "Sequence contains more than one element". | Fable, run | The store finds an index by its exact types. |
| D8 | `All` reads `ConcurrentDictionary.Values`, which takes every lock of the dictionary and copies every taggregate of every class before filtering. | both, read | Enumerate the dictionary. |
| D9 | Two sentences that are false: `Declaration.Component`'s summary says it routes to tevent handlers, which the code refuses; the state store's package description offers "a narrow seam for durable storage", which is internal. | Fable; both | Corrected. |
| D10 | Unverified: `TryGetForUpdate` of an id with no history, in the tevent store on MySQL, may lock nothing, since InnoDB at read committed locks no gap. | Opus, read | Checked with a specification on MySQL; reported, not fixed. |

**Done, 2026-11-18, on the spike (8a68e51e0 to 6433580ca, 11 commits):** D1 to D9, each with a specification; the full
suite passes, 5131 specifications, and the integration CQRS specifications on MySQL, SQL Server and PostgreSQL too. D7
also refuses, at declaration, a second index of one class and key type. **D10 is confirmed by running:** on MySQL a
second unit of work isn't held by the first's `TryGetForUpdate` of an id with no history, so two units of work adding one
id fail on the duplicate version instead of waiting. SQL Server and PostgreSQL keep the promise. Its specification is
skipped on MySQL, with `bug:` comments at it and at `MySqlTeventStoreSqlLayer.CreateLockHint`. **D11: fix it,** with a
lock table in the MySQL tevent store's schema (jastudio-rep-opus's call, 2026-11-18: internal, and Compze is greenfield):
`INSERT … ON DUPLICATE KEY UPDATE` on the id at a read for update takes a row lock InnoDB releases at commit or rollback,
under either isolation level. Rejected: `GET_LOCK`, which lasts for the session and outlives the transaction in the
pool; and documenting the weaker promise. **Done, 899546156:** the D10 specification passes on MySQL; the integration
CQRS specifications pass on MySQL, SQL Server and PostgreSQL, 960. The lock table keeps a row for each taggregate ever
got for update.

**Not a defect, documented instead:** `Lookup` outside a unit of work returns the index's own array, which a caller can
cast back and write to (both, run). A caller that casts a read-only collection to write to it is misbehaving, and a
read-only wrapper per key costs an object per key held, which the spike measured its way out of.

## 2. Documentation that doesn't tell the truth: fixed after the questions, since some depend on them

- **Store-neutral shared interfaces.** `ITaggregateStoreReader`'s "costs no lock and no copy", `ITaggregateReference.Current`
  as "the committed instance", the updater's "private copy" and "lock": the state store's behaviour, in interfaces both
  stores implement. The shared interfaces say what every store gives; each store's documentation says its costs.
- **What the state store does when it fails or waits:** `Environment.FailFast` when a commit's file operations fail,
  including a file another program holds for two seconds; nothing flushed to the disk; fixed 30-second waits, for a lock,
  for a reload, and for units of work during one; loading everything at its first use, on that use's thread and inside
  its transaction, with `ReloadFromStorage` as the way to choose when.
- **An index's rules:** one per pair of taggregate class and key type; what a key function must be; what a lookup costs
  inside a unit of work; that a reader can see a taggregate's new state under its old keys for a moment during a commit.
- **The updater's requirements:** a transaction begun by `ExecuteUnitOfWork`; `TryGet` of an id as another class of the
  same store throws; what `Current` does after a deletion.
- **The changelogs:** they don't mention tevent routing per class, the change that touches every taggregate class a
  consumer has. And they should lead with what changed without a signature changing: a taggregate's tevents are no
  longer delivered to other processes unless their hierarchy says so; a tevent store keeps only its declared classes;
  transactions must be begun by `ExecuteUnitOfWork`; a failed publish rolls the unit of work back even when caught.
- **Stale after Q11:** nearly every design document in `shared/` uses names the naming pass changed; the documentation pass
  after the questions updates them all.
- **Stale after Q10:** in `shared/`, the README, `bugs-and-findings.md`, `snapshots-of-every-taggregate-shape.md`,
  `step-5-the-rest-of-the-teventives.md`, `the-design.md`, `the-files-of-a-state-store.md`, `the-taggregate-store.md`
  and `what-was-measured.md`; in `opus/`, the README, `1-loading-a-taggregate-from-a-snapshot.md`,
  `3-told-once-per-publish-cascade.md` and `6-routing-by-convention-compared.md`; and
  `fable/a-taggregate-without-a-tevent-store.md`.
- **Stale after Q9:** the README, `step-5-the-rest-of-the-teventives.md`, `the-design.md` and
  `opus/5-prototype-of-per-class-routing.md` still show the explicit `ITeventRouted`.
- **Stale after Q5:** the README, `the-taggregate-store.md` and `taggregates-with-dependencies.md` still show `fileOf`,
  `snapshotsAreEqual` or the overload that took it.
- **Stale design documents:** the README's "The answer" and `bugs-and-findings.md` still describe dispatch at the commit;
  `the-taggregate-store.md` says the store keeps one reference per taggregate, and that `HasAny` avoids resolving a
  reference.

**Code left by the builds, to clean up with the documentation:** the specifications that build taggregate tevents without a
taggregate (the tevent store's, the migrations', the serializer's performance) set their identity through
`TaggregateTeventCE` in Compze.Tests.Common, by reflection on private setters, since Q10 made the mutable tevent
interfaces internal. That is a hole into the library the black-box rule doesn't allow: they should publish their tevents
on taggregates, or move to an internal specifications project.

## 3. The questions for Magnus, one at a time, most important first

| # | Question | The reviewers | Status |
|---|---|---|---|
| Q1 | A unit of work across a state store kept in files and a database isn't atomic across a crash. That breaks exactly-once delivery when the state store is in an exactly-once endpoint. Refuse what can be detected and document the rest, or make it atomic? | both, read: refuse and document now | **decided by Magnus: refused by default, with an opt-out the exception names; built, 0ecee1268,** [specified here](../a-state-store-in-files-and-a-database-in-one-transaction.md) |
| Q2 | The all-in-memory store: its name. `MemoryResidentStateStore`, `InMemoryStateStore`, or keep `StateStore` and say it in every first line? | both: a store of its own; Opus `MemoryResident…`, Fable `InMemory…`, each unsure | **decided by Magnus, 2026-11-18: `MemoryImageStateStore`,** with its package, namespace and types; built, 9f21d93d5, [specified here](../the-memory-image-state-store.md) |
| Q3 | `StateStoreBuilder.Taggregate(…)` with a service: it spares JAStudio the refactoring of notes that reach other notes through `NoteServices`, 31 times. Keep, and say what Compze recommends? | both: keep and document; Fable: JAStudio's notes would be better for the refactoring || **decided by Magnus: any number of dependencies, through a registration of the container; built, 2a05dcbd5,** [specified here](../taggregates-with-dependencies.md) |
| Q4 | `AtRuntime` and its rule (explained plainly in both reviews). Keep the rule; rename it `ObservableAtRuntime`; and take it out of the registrar runtime code gets, where it always throws: onto the builder (Fable) or the runtime gets a smaller registrar type (Opus)? | both: keep the rule, rename, move || **decided by Magnus: `ObserveFromOtherEndpoints`, on endpoints only; built, 0255c7231,** [specified here](../observing-tevents-from-other-endpoints.md). **Reopened for the names, 2026-11-18:** the declaration also permits observers created after composition, the endpoint's own tevents included, which the name didn't say. **Decided by Magnus: the refusal stays; "dynamic observer" and "composed observer", and `SubscribeForDynamicObservers<TTevent>()`; built, e01905b14,** [specified here](../dynamic-and-composed-observers.md) |
| Q5 | The storage seam's shape: remove `snapshotsAreEqual` (the storage knows what changed) and `fileOf` (no user; brings a race), and add an operation that rewrites every taggregate's storage, which JAStudio does today with a copy of Compze's private layout? | Fable: remove both; Opus: narrow `fileOf` to the id, keep `snapshotsAreEqual`; both: the rewrite || **decided by Magnus, 2026-11-18: remove `fileOf`; remove `snapshotsAreEqual`, a reload comparing only the taggregates whose files changed since the store last read or wrote them; add `WriteEverythingAnew`; built, 6680a23c7,** [specified here](../the-files-of-a-state-store.md) |
| Q6 | Testing surfaces in production packages: `AwaitNoTessagesInFlightAsync`, `TaggregateFiles.KeptInMemory()` and `ITaggregateFilesInMemory` into testing packages? | Fable: move; Opus: keep the first, lean to leaving the others | **decided by Magnus, 2026-11-18:** the files kept in memory into a `Testing` namespace of the package, as `OnAFileSystemInMemory()` and `IFileSystemInMemory`; `ITaggregateJsonFiles` with `PathOf` and its reverse; `AwaitNoTessagesInFlightAsync` stays, documented for applications too. Being built, [specified here](../the-testing-namespace-and-the-json-files.md) |
| Q7 | A state store's taggregate restarts at version 1 at every load. `RestoreIdentity(id, version)` for a consumer that stores it? | Fable, run | **decided by Magnus, 2026-11-18: document it; documented,** 091807172, on `ITaggregate.Version`, `ITaggregateTevent.TaggregateVersion` and the store's registrar |
| Q8 | `ITeventStoreReader` extends the shared reader, so its `Get` gives any store's taggregates. Make it an interface of only `GetHistory` and `GetReadonlyCopyOfVersion`? (Item 22 decided the inheritance.) | both: don't extend | **decided by Magnus, 2026-11-18: it no longer extends the shared reader; built, 56a764ac1,** [specified here](../the-tevent-store-reader-alone.md) |
| Q9 | `ITeventRouted`: CA1033 on every public unsealed consumer class (17 in JAStudio), and a forgotten routing fails at first construction. Revisit item 24, or keep? | Fable: a second opinion for the constructor argument; Opus: keep | **decided by Magnus, 2026-11-18: a protected abstract `TeventAppliers` property replaces the explicit `ITeventRouted`; built, d53f30a0d,** [specified here](../tevent-routing-as-a-protected-property.md) |
| Q10 | Small trims, as a batch: `AssertCanBeChanged` private; `Save` → `Add` without its type parameter; `HasAny` removed; `ITaggregateReference.ForUpdate()` removed; `AssertLosesNothing` removed or kept; `IMutableTaggregateTeventWithIdentity` internal; `ITeventRoutedMember` → `ITeventRoutedPart` with its member internal; `SharedTomponentSlotOfAClass` → `SharedTomponentSlot` taking `Func<TOwner, SharedTomponent<…>>`; `ReloadedTaggregates` as collections; one reload member or two. | mostly both || **decided by Magnus, 2026-11-18: "OK to all"; built, 843a7bebc,** [specified here](../ten-trims-of-the-public-api.md) |
| Q11 | The naming pass: the rest of the two reviews' name tables. | two fresh sessions, with all our rules and with nothing of ours | **decided by Magnus, 2026-11-18:** jastudio-rep-opus's picks, and `DifferencesBetweenItAndItsCopy`; built, 1a0d766bb,** [specified here](../the-naming-pass.md) |
| Q12 | `SubscribeForDynamicObservers` on `LocalTessagingEngineBuilder` too, doing nothing there, so that declarations shared by a plain container and an endpoint run unchanged in both, as the builder's documentation promises: today a dynamic observer that works in a plain container is refused once the same declarations compose an endpoint. | raised by the naming agent without our rules, 2026-11-18 | **decided by Magnus, 2026-11-18: not added.** Applications share registrar callbacks, not a builder, and the subscription is the endpoint's |

**Kept as built, both agreeing:** the reader and updater split, the bulk reader of its own, deletion as a tevent,
references in no order, indexes defined by state, the snapshot round trip comparing instances, tevent routing declared
per class and its shape with `DeclareAppliers` hidden by subclasses, `ForEachUnitOfWork`, runtime observation, the
storage seam internal, the MemoryPack cache as its own package, `TeventApplyingRoot`, `AddRestored`.
