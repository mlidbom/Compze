# The files of a state store: no fileOf, no snapshotsAreEqual, and WriteEverythingAnew

**Decided by Magnus, 2026-11-18** (the end review's fifth question, item 70), in three parts:

- **`fileOf` is removed.** Magnus asked whether a taggregate's file computed by the consumer would let a store spread its
  files across several drives for disk throughput. It can't as built: its file must be under the class's folder, and the
  commit journal records paths relative to the store's one folder; placement across drives is a function of the id, not
  of the state; and adding a placement option later breaks no one. Magnus: "Remove it then."
- **`IStateStore.WriteEverythingAnew()` is added**, for what JAStudio did with a copy of Compze's private layout. Magnus:
  "OK".
- **`snapshotsAreEqual` is removed.** Magnus asked whether Compze could match JAStudio's comparison by MemoryPack without
  requiring MemoryPack, where and how often the comparison runs, and whether a hand-written comparison would be a
  realistic optimization. The answer: a reload need compare only the taggregates whose files changed since the store last
  read or wrote them, which costs little whatever the serializer, and a hand-written comparison must name every field,
  where a forgotten one reports a changed taggregate as unchanged. Magnus: "Yes."

Specified here before it is built. Every new name is a placeholder for the naming pass.

## What a consumer sees

### A taggregate's file is its class's folder and its id

`TaggregateFiles.Of<TTaggregate>(string folder)` takes the class's folder alone. Each taggregate's file is
`<class folder>/<first two hex digits of the id>/<id>.json`, as it was by default. A taggregate keeps its file for its
whole life, so a commit never moves one.

### A reload tells what changed without a comparison of the consumer's

`StateStoreBuilder.Taggregate<TTaggregate, TSnapshot>(createEmpty)` takes the factory alone, and the overload
`Taggregate<TTaggregate, TSnapshot>(registration, snapshotsAreEqual)` is gone: `Taggregate<TTaggregate>(registration)`
serves every class built through the container. A snapshot type needs no `Equals`: a class without one, and a record
holding a list, work as well as any.

`IStateStore.ReloadFromStorage()` reports a taggregate the store held as changed when the state its file holds now isn't
the state the store held, as the store's own serializer and the consumer's options write state:

1. **A file unchanged since the store last read or wrote it holds what the store holds,** and is compared with nothing.
   Whether it changed is told by its length and last write time, which listing the class's folder gives without opening
   the file: the store records them for each file at every load and at every commit. A reload of files that no other
   program changed serializes no snapshot.
2. **A file that changed is compared with what the store would write for the taggregate it held:** its text with the
   held snapshot serialized. The same text is the same state.
3. **Where the text differs, the snapshot loaded from the file is serialized too,** and compared with the held one's. The
   same text is the same state, written otherwise, such as indented differently or with its properties in another order;
   another text is another state, and the taggregate is reported changed.

A taggregate whose file now holds another class than the store held is changed. The costs: nothing for an unchanged
file, and one or two serializations for each changed one, so a sync that changes a few hundred of a hundred thousand
files costs a few hundred comparisons.

Two caveats, both in the documentation:

- **It trusts a file's length and last write time,** as the MemoryPack cache does: a program that rewrites a file with
  another state of the same length and gives it back its last write time isn't seen.
- **It compares what the serializer writes.** Where equal states can serialize differently, such as a `HashSet`
  whose order depends on the order its items were added in, a taggregate may be reported changed whose state is the same:
  never the other way round.

Files kept in memory (`KeptInMemory`) work the same way: each write gives a file a later write time than any before. The
MemoryPack cache is unchanged: what it gives for a file is what the file held when the cache was written, and the
comparison above starts from what the store last read or wrote.

### `SnapshotRoundTrip` checks the instances alone

`SnapshotRoundTrip.AssertLosesNothing(taggregate, createEmpty)` and `DifferencesOf(taggregate, createEmpty)` take no
comparison. Their check is the field-by-field comparison of the taggregate and its copy, as before; the last check, that a
snapshot of the copy equals a snapshot of the original by the comparison or by `Equals`, is gone with them, since a
reload no longer compares snapshots so.

### `IStateStore.WriteEverythingAnew()`

Writes every taggregate's file anew, as the store's serializer and the consumer's options write its snapshot now, whether
the taggregate changed or not: for a consumer that has changed its snapshot types or its serializer's options, so that the
files don't stay a mix of the old form and the new until each taggregate next changes.

- **One commit, through the journal:** every file is written under a temporary name first, then the journal, then the
  renames, as a unit of work's commit is. A failure before the journal is written, such as a snapshot that can't be
  serialized, writes nothing, and comes out of the call. A failure after it stops the process, as a commit's does: the
  next start finishes it from the journal.
- **While no unit of work is open,** as a reload: it waits for the units of work open in the store to complete, and units
  of work that begin meanwhile wait for it.
- **It changes no taggregate,** nor any version, and dispatches no tevent. A reload afterwards reports nothing changed.
- **Refused** for a store composed with no storage, and inside a unit of work that has changed the store's taggregates, as
  a reload is.
- Every file has then changed, so the next start reads every file, and writes a load cache anew.

## How it is built

- **`TaggregateFiles.Of`** loses `fileOf`. `FilesOfAClass` gives a taggregate's file from its id alone, and the store's
  record of which file holds each taggregate (`StoredFiles`) keeps no index from files back to taggregates. Gone with
  them: deleting a taggregate's old file when a commit gives it another, the assertions that two taggregates of one unit
  of work don't share a file and that a taggregate doesn't get another's, the assertion that a declared file is a `.json`
  file under the class's folder, and `TaggregateSnapshot.TaggregateToWrite`.
- **`StoredFiles`** records, for each taggregate, its file and the file's length and last write time when the store last
  read or wrote it: at each load from the listing, and at each commit from the temporary file, which renaming leaves as it
  was. The file system gives a single file's length and last write time.
- **The storage seam** tells, for each snapshot it loads, whether its file is unchanged since the store last read or wrote
  it, and for one that changed and that the store held, the text it read; and it compares a loaded snapshot with a held
  one as above. The store takes a snapshot of a held taggregate only for a changed file.
- **`SnapshotableTaggregateClass`** loses the comparison and its `Equals` assertion; `SnapshotRoundTrip` loses its last
  check.
- **`WriteEverythingAnew`** prepares a write of every committed taggregate's snapshot and commits it through the journal
  outside any transaction, behind the gate that keeps a reload and units of work apart.

## Specifications

1. A reload after another program changes a vocab's file reports exactly that vocab changed.
2. A reload after another program rewrites a vocab's file with the same state, as the store writes it, reports nothing
   changed; and one after it writes the same state otherwise, not indented, reports nothing changed.
3. A reload of files that no other program changed serializes no snapshot, counted by a converter in the consumer's
   options; and one after a unit of work changed a vocab reports nothing changed.
4. A sentence, whose snapshot type has no `Equals` and which is declared with no comparison, is reported changed when
   another program changes its file, and not when it doesn't.
5. `SnapshotRoundTrip` checks a book whose snapshot type has no `Equals`, with no comparison given.
6. After another program writes a vocab's file otherwise, `WriteEverythingAnew` leaves every file holding exactly what the
   consumer's options make of its taggregate's snapshot; the taggregates and their versions are as before; no tevent is
   dispatched; and a reload afterwards reports nothing changed.
7. When a snapshot can't be serialized, `WriteEverythingAnew` throws, and every file is as before, with no temporary file
   left.
8. `WriteEverythingAnew` waits for a unit of work open on another thread, so the files hold what that unit of work
   committed; it is refused for a store with no storage, and inside a unit of work that changed the vocab.
