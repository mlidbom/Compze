# Changelog

All notable changes to Compze.Teventive.StateStore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- **The state store, a taggregate store that keeps taggregates as their state, not their history.** Composed with `StateStore(...)`, which declares the taggregate classes it keeps, each with a factory for empty instances, its indexes, and its storage.
  - Each unit of work gets a private copy of each taggregate it gets for update, and holds the taggregate's lock until it completes.
  - The tevents the copies publish are dispatched synchronously as each publish cascade ends, once the taggregate's invariants hold, so participation handlers run inside the unit of work's transaction, and a handler's failure comes out of the domain call.
  - A committed taggregate is read only, and read without a lock.
  - `ITaggregateIndex<TTaggregate, TKey>` files taggregates under keys their state gives, and reads its own unit of work's changes. A lookup gives its references in the order of their ids, so the same taggregates filed under a key always come back in the same order.
  - A taggregate class is declared with a factory for its empty instances, which may take a service of the container: `Taggregate((NoteServices services) => new VocabNote(services))`.
  - `IStateStore.ReloadFromStorage` loads everything again while running, keeping every reference, and reports which taggregates were added, changed or removed. `ReloadFromStorageAfter(changeToTheStorage)` first runs a change to the storage, such as a fast-forward of a checkout, while no unit of work is open and new ones wait.
  - `ITaggregateStoreBulkReader.All<TTaggregate>()` gives a reference to every taggregate of a class the state store keeps, in id order, and inside a unit of work reads its own saves and deletions.
  - `ITaggregateStorage` is where a consumer keeps what is committed, in a form of its own: the store loads everything from it at its first use, and writes each unit of work in two steps, prepare and commit. `StoredIn` takes the storage, or a factory that builds it with a service of the container, when the store is first used.
