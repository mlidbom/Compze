# Changelog

All notable changes to Compze.Teventive.StateStore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- **The state store, a taggregate store that keeps taggregates as their state, not their history.** Composed with `StateStore(...)`, which declares the taggregate classes it keeps, each with a factory for empty instances, its indexes, and its storage.
  - Each unit of work gets a private copy of each taggregate it gets for update, and holds the taggregate's lock until it completes.
  - The tevents the copies publish are dispatched synchronously as each publish cascade ends, once the taggregate's invariants hold, so participation handlers run inside the unit of work's transaction, and a handler's failure comes out of the domain call.
  - A committed taggregate is read only, and read without a lock.
  - A taggregate that publishes an `ITaggregateDeletedTevent` is taken out of the store, its indexes and its storage when the unit of work commits. One saved and deleted in the same unit of work writes nothing.
  - `ITaggregateIndex<TTaggregate, TKey>` files taggregates under keys their state gives, and reads its own unit of work's changes. A lookup gives its references in no particular order, as an `IReadOnlyCollection`: a caller that needs an order sorts what it finds. An index keeps no record of each taggregate's keys: a commit takes a changed taggregate's old keys from its committed instance, so a key function must give keys from the taggregate's own state alone; one that doesn't fails the unit of work, which rolls back.
  - A taggregate class is declared with a factory for its empty instances, which may take a service of the container: `Taggregate((NoteServices services) => new VocabNote(services))`.
  - `IStateStore.ReloadFromStorage` loads everything again while running, keeping every reference, and reports which taggregates were added, changed or removed. `ReloadFromStorageAfter(changeToTheStorage)` first runs a change to the storage, such as a fast-forward of a checkout, while no unit of work is open and new ones wait.
  - `ITaggregateStoreBulkReader.All<TTaggregate>()` gives a reference to every taggregate of a class the state store keeps, and inside a unit of work reads its own saves and deletions.
  - `ITaggregateStorage` is where a consumer keeps what is committed, in a form of its own: the store loads everything from it at its first use, and writes each unit of work in two steps, prepare and commit. `StoredIn` takes the storage, or a factory that builds it with a service of the container, when the store is first used.
  - `StoredInFiles(folder, options, files => files.Of<TTaggregate>(folder, fileOf))` keeps the store in JSON files: a folder for each class, a file for each taggregate, `<first two hex digits of the id>/<id>.json` unless the class declares where each taggregate's file is. A file holds exactly what the consumer's `JsonSerializerOptions` make of the snapshot, in UTF-8 without a byte order mark. A unit of work's files are written all or none: each is written whole under a temporary name when the unit of work prepares, and at the commit a journal of the renames and deletions is written before any is made, so a start after a crash finishes the commit or discards it. A taggregate whose file changes with its state moves to its new file. The store's own files are in `.state-store` under the folder.
  - A store kept in files can start from a cache that a package of its own plugs in, such as Compze.Teventive.StateStore.MemoryPack's `CachedWithMemoryPack()`: it reads only the files that are new, or whose length or last write time changed since the cache was written.
  - `files.KeptInMemory()` keeps a store's files in memory instead of on the disk, for specifications: the store works as on the disk, with the same layout, the consumer's JSON options, the journal and any cache, and touches no file on the disk. A specification reads and changes the files by path through `ITaggregateFilesInMemory`, such as to change a file behind the store before a reload.
