# The tevent store's reader alone: ITeventStoreReader no longer extends ITaggregateStoreReader

**Decided by Magnus, 2026-11-18** (the end review's eighth question): "OK". Both reviewers had found that
`ITeventStoreReader` extends `ITaggregateStoreReader`, so its `Get` gives any store's taggregates, a memory image state
store's included. Recommended by both: an interface of only what the tevent store adds. Specified here before it is built.

**This supersedes the part of item 22 (2026-11-16) that made `ITeventStoreReader` extend the shared reader,** and what
[the-taggregate-store-build-log.md](the-taggregate-store-build-log.md#step-2b-specified-the-tevent-store-joins-the-shared-updater-and-reader)
and [the README](README.md) say of that inheritance. The rest of item
22's structure stands: one `ITaggregateStoreReader` and one `ITaggregateStoreUpdater` for every store of a container.

## Why it stops extending the shared reader

- **The shared reader reads every store.** There is one `ITaggregateStoreReader` per container, and it routes each class to
  the store that keeps it. Item 22 decided the inheritance while the reader was the tevent store's own.
- **So the name told less than the truth.** An interface named for the tevent store read any store, and its own remarks
  had to say so.
- **And there were two ways to call the same `Get`:** through `ITeventStoreReader`, and through `ITaggregateStoreReader`.
- **What only the tevent store can read is its history:** `GetHistory`, and `GetReadonlyCopyOfVersion`, which builds the
  taggregate from its history up to a version. An interface of just those two says what it is.

## What changes

- **`ITeventStoreReader` has two members:** `GetHistory` and `GetReadonlyCopyOfVersion`, as they were. `Get`, `TryGet`
  and `Reference` are no longer on it.
- **Its documentation says what it is:** what only the tevent store can read, since it keeps each taggregate as its
  history. The remark that its `Get` reads every store goes, with the `Get`.
- **Its implementation needs only the tevent store:** it no longer takes the container's `ITaggregateStoreReader` to
  forward `Get`, `TryGet` and `Reference` to. It stays a singleton.
- **Compze's own code that read a taggregate through it takes `ITaggregateStoreReader` instead:** the tevent store's
  typermedia tuery `GetTaggregate`, whose handler gets the taggregate as the shared reader does, as before.
- **The query model generators are unchanged:** `SingleTaggregateQueryModelGenerator` reads only histories.

## What a consumer writes

Code that read a taggregate through `ITeventStoreReader` takes `ITaggregateStoreReader` beside it, resolved from the
container as before.

Before:

```csharp
var reader = container.RootResolver.Resolve<ITeventStoreReader>();
var user = reader.Get<User>(userId);
var history = reader.GetHistory(userId);
```

After:

```csharp
var user = container.RootResolver.Resolve<ITaggregateStoreReader>().Get<User>(userId);
var history = container.RootResolver.Resolve<ITeventStoreReader>().GetHistory(userId);
```

`TryGet` and `Reference` move the same way. Code that only reads histories or versions changes nothing.

## Releasing

The changelog of Compze.Teventive.TeventStore.Abstractions already says, in its top section, that the spike made
`ITeventStoreReader` extend `ITaggregateStoreReader`. That isn't released, so the line is rewritten to say what is
released instead: `GetReadonlyCopy` is gone, `ITaggregateStoreReader.Get` replaces it, and `ITeventStoreReader` keeps only
`GetHistory` and `GetReadonlyCopyOfVersion`. No other package's public API changes.

## Specifications

The behaviour doesn't change, so the specifications are the existing ones, each reading a taggregate through
`ITaggregateStoreReader` and a history through `ITeventStoreReader`:

1. With a tevent store and a memory image state store in one container, the `ITaggregateStoreReader` gets a taggregate of
   each, and the `ITeventStoreReader` gives the user's history and its version before a deletion.
2. After a taggregate of the tevent store is deleted, in the unit of work that added it or in a later one, its history
   ends with the deleted tevent, and the `ITaggregateStoreReader` gets no such taggregate.
3. `GetReadonlyCopyOfVersion` gives the taggregate as it was at each version.
4. A query model generator builds its query model from the history the `ITeventStoreReader` gives.

The full suite passes.
