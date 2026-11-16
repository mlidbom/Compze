# Compze.Teventive.StateStore

The state store: a store that keeps [Compze](https://github.com/mlidbom/Compze) taggregates as their state, not their history.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **The state store** — composed with `StateStore(...)`, it keeps the taggregate classes declared for it in memory, and in a storage of the consumer's if one is declared.
- **Units of work with private copies** — each unit of work changes its own copy of a taggregate, holding its lock until it completes, and the copy becomes the committed instance when it commits.
- **Dispatch at the commit** — the tevents a unit of work's taggregates publish are dispatched when its transaction prepares, so participation handlers run inside the transaction.
- **Indexes defined by state** — `ITaggregateIndex<TTaggregate, TKey>` files taggregates under keys their state gives, and reads its own unit of work's changes.
- **A narrow storage seam** — `ITaggregateStorage` loads snapshots of the taggregates' state and writes them in two steps, prepare and commit.

The interfaces it implements, `ITaggregateStoreReader` and `ITaggregateStoreUpdater`, are in Compze.Teventive.
