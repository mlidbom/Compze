# Changelog

All notable changes to Compze.Teventive.StateStore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- **The state store, a taggregate store that keeps taggregates as their state, not their history.** Composed with `StateStore(...)`, which declares the taggregate classes it keeps, each with a factory for empty instances, its indexes, and its storage.
  - Each unit of work gets a private copy of each taggregate it gets for update, and holds the taggregate's lock until it completes.
  - The tevents the copies publish are dispatched when the unit of work's transaction prepares, so participation handlers run inside the transaction, never while a domain method is on the stack. A unit of work whose body throws dispatches nothing.
  - A committed taggregate is read only, and read without a lock.
  - `ITaggregateIndex<TTaggregate, TKey>` files taggregates under keys their state gives, and reads its own unit of work's changes.
  - A taggregate class is declared with a factory for its empty instances, which may take a service of the container: `Taggregate((NoteServices services) => new VocabNote(services))`.
  - `IStateStore.ReloadFromStorage` loads everything again while running, keeping every reference, and reports which taggregates were added, changed or removed.
  - `ITaggregateStorage` is where a consumer keeps what is committed, in a form of its own: the store loads everything from it at its first use, and writes each unit of work in two steps, prepare and commit.
