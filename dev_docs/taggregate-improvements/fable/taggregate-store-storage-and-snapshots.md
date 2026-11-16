# The taggregate store: the storage seam, the JSON store, and the snapshot API

2026-11-16, jastudio-rep-fable, for jastudio-rep-opus to fold into the shared folder. A design, with nothing built.
What it says of JAStudio is from reading `JAStudio.Core/Storage` in `C:\Dev\JAStudio.wt2`, not from its sessions.

## The headline

**JAStudio already has all three parts, by other names, and the store can take them nearly as they are.** Its
`INoteRepository` is the storage seam. Its `VocabDto`, `SentenceDto` and `KanjiDto` are the snapshot types, and its
converters between a note and its DTO are the taking and the restoring. What it lacks is what a store would give it:
a write that is all or nothing. Today a note's file is written with `File.WriteAllText`, so a crash in the middle
leaves half a file.

## The snapshot API on a taggregate

```csharp
///<summary>A taggregate whose whole state can be taken as a snapshot, and restored from one.</summary>
public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>
{
   TSnapshot TakeSnapshot();
   static abstract TSelf RestoreFrom(TSnapshot snapshot);
}
```

- **The restoring is a static member of the class,** which the compiler demands of every class a store holds. Nothing
  is registered, and a concrete class in a hierarchy, such as the vocab under `JPNote`, restores from its own snapshot
  type.
- **`Taggregate` gets a protected way to say that it was restored:** its id, and version 1. Without it the first tevent
  a restored taggregate publishes is refused as not being a creation tevent.
- **No version is stored.** With a lock for each taggregate, nothing compares versions. JAStudio stores none today.
- **A snapshot holds the taggregate's whole state, not only what is stored.** The store makes the private copy for an
  update from a snapshot, so what a snapshot lacks is lost at the next update. What is stored is the storage's choice,
  through the serializer. Whether JAStudio's DTOs hold what Anki says of a note's cards I haven't confirmed.
- **A part gives its share:** a component's and a shared tomponent's state are nested in the root's snapshot, as they
  are in JAStudio's DTOs.
- **It costs about what constructing the note costs.** A committed instance isn't changed, so a snapshot can share
  every immutable value with it.

**Two ways of setting state now exist,** the tevent appliers and the restoring, which both teams had argued against and
Magnus has decided for. What guards it: a specification helper from Compze that, for any taggregate, takes a snapshot,
restores from it, takes another, and compares the two. Since every update goes through a snapshot, a property the
snapshot misses fails the first specification that updates twice.

## The storage seam

```csharp
///<summary>Where a taggregate store keeps what it has committed. Narrow: it knows snapshots, not taggregates.</summary>
public interface ITaggregateStorage
{
   IReadOnlyList<StoredSnapshot> LoadAll();
   IPreparedWrite Prepare(IReadOnlyList<SnapshotToSave> toSave, IReadOnlyList<TaggregateId> toDelete);
}

public interface IPreparedWrite
{
   void Commit();   // must not fail for a reason Prepare could have found
   void Discard();
}
```

- **Two steps, because a transaction has two.** The session calls `Prepare` in its `OnPrepare`, after the unit of
  work's queue is empty and it has taken the snapshots of its copies. It calls `Commit` in its `OnCommit`, then swaps
  the copies in as the committed instances, then releases the locks. `Discard` in `OnRollback`.
- **In memory:** `Prepare` does nothing, and the committed instances are the store.
- **Everything that can fail belongs in `Prepare`:** serializing, writing, a full disk, a file another program holds.

## The JSON store

Two layers, so that JAStudio can keep its files as they are:

- **The files' layout and format are the consumer's:** which file a taggregate's snapshot is in, and how a snapshot
  becomes text. JAStudio's corpus is reviewed as diffs, so neither may change with the store. Compze ships a plain
  default: a folder for each type, a file for each id, System.Text.Json.
- **Writing several files as one is Compze's:** the part worth sharing, since it is the part that is easy to get wrong.

### Several files, all or nothing

1. **At prepare,** each file is written whole under a temporary name, in a folder of the store's own on the same
   volume, which a rename needs to be atomic.
2. **At commit, the journal is written first:** one small file listing each temporary file and the name it is to get,
   and each file to delete. **The journal is the commit:** once it exists, the write has happened.
3. **Then the renames and deletions, then the journal is deleted.**
4. **At start, before anything is loaded:** a journal that is there is carried out to its end, and deleted. Temporary
   files without a journal are deleted.

- **The journal is written at commit, not at prepare.** Written at prepare, a crash before a rollback has removed it
  would make the next start carry out a write whose transaction was rolled back.
- **A failure after the journal is written is fatal:** the process stops, and the next start finishes the write. To go
  on would be to run with memory that disagrees with the disk, which Magnus has named the durable truth.
- **How durable:** by default nothing is flushed to the disk, so a commit survives the process dying, and not the
  machine losing power. Flushing every file costs milliseconds each, which is minutes for a rebuild of 80,000 notes.
  JAStudio's corpus is in git, which is its recovery from a lost machine. A setting for consumers that need more.
- **The store's folder for temporary files and the journal is not the consumer's data:** for JAStudio, ignored by git.

### What I haven't settled

- **A transaction with a database in it too,** such as an outbox: the JSON store is a volatile participant, so the
  database can commit and the JSON store's process die before its journal is written. JAStudio has no database. A
  store that had would need the storage enlisted durably.
- **`SaveSnapshot` and `LoadSnapshot` of all notes** in JAStudio's `FileSystemNoteRepository`: I take them for a cache
  of the whole corpus for a fast start, and haven't confirmed it. If so, it is the consumer's own, beside the store.

## What it means for JAStudio's start

Loading is `LoadAll`, then `RestoreFrom` for each snapshot, on many threads. No tevent is applied, so the garbage a
start made in both teams' prototypes, about 0.4 GB, isn't made, and the time is that of constructing the notes.
