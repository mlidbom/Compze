# The memory image state store: the state store's name, and its package's

**Decided by Magnus, 2026-11-18** (the end review's second question, item 78). Both reviewers had found that the state
store holds every taggregate in memory by construction, and that it is a store of its own, not a mode of a more general
one. Asked for its name, Magnus first reached for an intuition: "It is not strictly the same thing, but memory mapped files
just keeps popping up in my mind. You have everything in ram, but it is backed by an actual file.... MemoryMappedJsonStore?"
jastudio-rep-opus answered that "memory mapped" names an exact mechanism of .NET with the opposite costs, that "Json" names
one storage option of a store that can have none, and that the term for the intuition is Martin Fowler's
[memory image](https://martinfowler.com/bliki/MemoryImage.html). Magnus: "MemoryImage*Store does sound right to me. A design
pattern being less than super well known does not stop it from being the correct term and instantly understandable by those
that know the pattern, and searchable, and likely instanty intuitive to LLMs..." Then, after the answer below on what
"State" adds: "OK. Go with that name." Specified here before it is built.

## Why "memory image"

A memory image holds an application's whole state in main memory, as the system of record, and keeps behind it what that
state is rebuilt from when the application starts. That is what this store is:

- It holds every taggregate in memory, as an instance, for the container's life.
- It loads everything at its first use, and its storage can only load everything and write a unit of work's changes.
- Its reads without a lock, its indexes over every instance, `ReferencesToAll` as a scan, and a load that compares
  everything all follow from holding everything.
- Its JSON files are a copy written through at each commit, read again only by a load.

## Why "state" stays

Fowler's memory image is, in its usual form, rebuilt from a log of events, and Compze has a tevent store: a reader who knows
the pattern would expect tevents behind a "memory image store". This one is rebuilt from the state of each taggregate, a
snapshot in a file of its own, and keeps no history. "State" says which, and keeps the pairing with the tevent store, state
against history. A memory image rebuilt from tevents would be another store, with a name of its own.

## The renames

| Before | After |
|---|---|
| package and project `Compze.Teventive.StateStore` | `Compze.Teventive.MemoryImageStateStore` |
| package and project `Compze.Teventive.StateStore.MemoryPack` | `Compze.Teventive.MemoryImageStateStore.MemoryPack` |
| namespaces `Compze.Teventive.StateStore…` | `Compze.Teventive.MemoryImageStateStore…` |
| `IStateStore` | `IMemoryImageStateStore` |
| `StateStoreBuilder` | `MemoryImageStateStoreBuilder` |
| `StateStoreRegistrar`, and its registrar method `StateStore(...)` | `MemoryImageStateStoreRegistrar`, `MemoryImageStateStore(...)` |
| internal types, files and specifications named for the state store, such as `TaggregateStateStore` and the `CQRS/StateStore` specifications | named for the memory image state store |

What stays: `KeptInMemory` and `ITaggregateFilesInMemory`, whose names wait for the end review's sixth question, on the
testing surfaces. The phrase "state store" stays in prose where it means the kind of store, state rather than history; where
it means this store, the prose says "memory image state store".

## What a consumer writes

```csharp
registrar.MemoryImageStateStore(store => store.Taggregate(() => new VocabNote())
                                              .StoredInJsonFiles(folder, jsonOptions, files => files.StartingFromAMemoryPackCache()
                                                                                                    .FolderOf<VocabNote>("vocab")));

var store = container.Resolve<IMemoryImageStateStore>();
var changes = store.LoadFromStorage();
```

with `using Compze.Teventive.MemoryImageStateStore;`, and package references to `Compze.Teventive.MemoryImageStateStore`
and, for the cache, `Compze.Teventive.MemoryImageStateStore.MemoryPack`.

## The documentation

The first line of the registrar method's, the interface's and the builder's documentation, and the package's description,
say that the store is a memory image: every taggregate held in memory for the container's life, rebuilt at its start from a
snapshot of each, which its storage keeps.

## Releasing

Neither package has been released: no `Compze.Teventive.StateStore` tag exists. So the renamed packages are simply new
packages at their first versions, with no rename bullet to write; their changelogs' headers name the new packages.

## Specifications

Renaming changes no behaviour, so the specifications are the existing ones, renamed with the store: the full suite passes.
