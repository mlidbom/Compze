# Compze.Teventive.MemoryImageStateStore

The memory image state store: a [memory image](https://martinfowler.com/bliki/MemoryImage.html) of [Compze](https://github.com/mlidbom/Compze) taggregates. Every taggregate is held in memory for the container's life, and rebuilt at the store's start from a snapshot of each, which its storage keeps: it keeps taggregates as their state, not their history.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **The memory image state store** — composed with `MemoryImageStateStore(...)`, it keeps the taggregate classes declared for it in memory, and in JSON files if they are declared.
- **JSON files** — `StoredInJsonFiles(...)`: a file for each taggregate, in a folder for each class, written with the consumer's `JsonSerializerOptions`, and a unit of work's files written all or none. Compze.Teventive.MemoryImageStateStore.MemoryPack adds a cache to start from, so a start reads only the files changed since. `ITaggregateJsonFiles` gives the path of each taggregate's file, and the taggregate each file is the file of. For specifications, `OnAFileSystemInMemory()`, in the `Testing` namespace, puts the files on a file system in memory, read and changed through `IFileSystemInMemory`, with no disk I/O.
- **Reloads and rewrites** — `IMemoryImageStateStore.LoadFromStorage()` loads the files again and reports which taggregates another program added, changed or removed, comparing only the files changed since the store last read or wrote them, so snapshot types need no equality. `IMemoryImageStateStore.WriteAllToStorage()` writes every file anew, as one commit, after a change to the snapshot types or to the serializer's options.
- **Units of work with private copies** — each unit of work changes its own copy of a taggregate, holding its lock until it completes, and the copy becomes the committed instance when it commits.
- **Synchronous dispatch** — the tevents a unit of work's taggregates publish are dispatched as each publish cascade ends, once the taggregate's invariants hold, so participation handlers run inside the domain call and the unit of work's transaction.
- **Indexes defined by state** — `ITaggregateIndex<TTaggregate, TKey>` files taggregates under keys their state gives, in no particular order, and reads its own unit of work's changes. A store has one index for each pair of taggregate class and key type, and a key function reads only the taggregate's own state.
- **Every taggregate of a class** — `ITaggregateStoreBulkReader.ReferencesToAll<TTaggregate>()`, in no particular order.
- **What holding everything costs** — its memory is the whole data set, and it loads every taggregate at its first use, on that use's thread: call `LoadFromStorage()` at the application's start to choose when. A failure after a commit's journal is written stops the process (`Environment.FailFast`), and the next start finishes the commit. The files are written, not flushed to the disk. Its waits, for a taggregate's lock and between a load and the units of work, give up after 30 seconds.

The interfaces it implements, `ITaggregateStoreReader`, `ITaggregateStoreBulkReader` and `ITaggregateStoreUpdater`, are in Compze.Teventive.
