# Taggregates fetched by a key of the consumer's own

Decided by Magnus 2026-11-26, on tessaging-and-taggregates' question whether a consumer could key a taggregate with an id
type of its own: "I will probably never try to support another type as the id itself. But an automatically managed
surrogate ID that consumers can fetch an aggregate via is entirely feasibel I think." Not started.

## Why

A taggregate's id is a `TaggregateId`, a Guid. It runs through everything that names a taggregate: the tevents'
`TaggregateId`, the tevent store's rows and the memory image state store's keys. A consumer whose things have ids of
their own has to map them to Guids. The tooling's `Session` does: its `SessionId` is ten characters of base 32, which it
writes into a Guid reversibly, behind eight bytes that say the id is a session's (its commit 99150082).

## The change

The `TaggregateId` stays the only id, and becomes a surrogate the consumer need never see: Compze makes it, and the
consumer fetches the taggregate by its own key.

For the memory image state store, most of it is there: `ITaggregateIndex<TTaggregate, TKey>` files taggregates under
keys their own state gives, so `Index<Session, SessionId>(it => [it.Id])` finds a session by its `SessionId` today. What
fetching by it would add:

- **A unique index:** a commit that would file a second taggregate under a key already taken fails, so a lookup gives at
  most one taggregate.
- **Getting the taggregate by its key,** for read and for update, rather than a collection of references.
- **Adding a taggregate by its key alone,** with Compze making its `TaggregateId`.

For the tevent store, which replays a taggregate's tevents by its id, it would take a table from key to `TaggregateId`,
written in the same transaction as the taggregate's first tevent.

A starting point: the unique index in the memory image state store, the store the consumer who asked uses.
