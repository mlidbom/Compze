# Compze.Teventive.StateStore

The state store: a store that keeps [Compze](https://github.com/mlidbom/Compze) taggregates as their state, not their history.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **The state store** — composed with `StateStore(...)`, it keeps the taggregate classes declared for it in memory, and in JSON files if they are declared.
- **JSON files** — `StoredInFiles(...)`: a file for each taggregate, in a folder for each class, written with the consumer's `JsonSerializerOptions`, and a unit of work's files written all or none. Compze.Teventive.StateStore.MemoryPack adds a cache to start from, so a start reads only the files changed since. For specifications, `KeptInMemory()` keeps the files in memory, read and changed through `ITaggregateFilesInMemory`, with no disk I/O.
- **Units of work with private copies** — each unit of work changes its own copy of a taggregate, holding its lock until it completes, and the copy becomes the committed instance when it commits.
- **Synchronous dispatch** — the tevents a unit of work's taggregates publish are dispatched as each publish cascade ends, once the taggregate's invariants hold, so participation handlers run inside the domain call and the unit of work's transaction.
- **Indexes defined by state** — `ITaggregateIndex<TTaggregate, TKey>` files taggregates under keys their state gives, in no particular order, and reads its own unit of work's changes.
- **Every taggregate of a class** — `ITaggregateStoreBulkReader.All<TTaggregate>()`, in no particular order.

The interfaces it implements, `ITaggregateStoreReader`, `ITaggregateStoreBulkReader` and `ITaggregateStoreUpdater`, are in Compze.Teventive.
