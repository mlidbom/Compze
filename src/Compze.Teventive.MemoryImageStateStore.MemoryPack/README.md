# Compze.Teventive.MemoryImageStateStore.MemoryPack

A [MemoryPack](https://github.com/Cysharp/MemoryPack) cache for the JSON files of [Compze](https://github.com/mlidbom/Compze)'s memory image state store, so that a memory image state store kept in files starts fast.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **`StartingFromAMemoryPackCache()`** — declared with a store's files, `files.StartingFromAMemoryPackCache().FolderOf<VocabNote>("vocab")`, it has the store start from a cache of the snapshots its files held at an earlier start. The store then reads only the files that are new, or whose length or last write time changed since, and drops those that are gone.
- **A cache for each class**, kept in the store's own folder and written whole after a start that read any file or found one gone.
- **A changed snapshot type is caught:** the cache records a fingerprint of the snapshot type's fields, and a cache written for another shape of the type is not used.

Each class's snapshot type must be one MemoryPack serializes, such as a `[MemoryPackable]` class.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Teventive.MemoryImageStateStore](https://www.nuget.org/packages/Compze.Teventive.MemoryImageStateStore) | The memory image state store, and its JSON files |

## License

Apache-2.0
