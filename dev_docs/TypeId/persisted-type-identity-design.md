# Persisted type identity — storage design

How the SQL layer represents a .NET type as a small, durable, database-local `int` — stable across
reclassification (stable ↔ mapped) and renaming — and keeps a full, durable history of every fully-qualified
name each type has ever had, with the current name clearly marked.

Status: **implemented across all SQL engines** (SQL Server, PostgreSQL, MySQL, SQLite). Indexing strategy A
(below) is in use.

## Core idea: identity is separate from spelling

A type's persisted canonical string changes over its lifetime: a leaf takes a GUID form (`"<guid>, 0"`) when
its assembly is mapped and a name form (`"Namespace.Type, Assembly"`) when it is stable, and a stable name
form changes again if the type is renamed. So one conceptual type accumulates **several** spellings over the
life of the data.

Identity is therefore a separate thing from spelling. A conceptual type gets one database-local `int` that
never changes; every spelling it has ever been persisted as points back at that one `int`. Storage rows
reference the `int`, so a type-filtered query (`WHERE ValueTypeId = @id`) matches rows written under *any* of
the type's spellings, and a row written under an earlier spelling resolves through the shared id to the
type's current form — historical data is readable without rewriting it.

## Schema

```
TypeIds      ( Id            <int identity PK>,
               CurrentName    <unicode> )                 -- the type's fully-qualified name, right now

TypeStrings  ( TypeString     <unicode, UNINDEXED, any length>,
               TypeId         <FK -> TypeIds.Id>,
               FirstSeenUtc    <datetime> )               -- every persisted $type spelling -> its concept

TypeNames    ( TypeId        <FK -> TypeIds.Id>,
               Seq            <int>,                       -- 1..N within a type; ordering
               FullyQualifiedName  <unicode>,
               FirstSeenUtc   <datetime> )                -- append-only history of names
```

- **`TypeIds.Id`** is a 4-byte `int` — the database-local reference. It is what every storage row holds, so
  every referencing column is a plain `int`: `Tevent.TeventType`, `Store.ValueTypeId`, inbox/outbox `TypeId`.
- **`TypeStrings.TypeString`** is the persisted `$type` value (a GUID form for mapped types, a name form for
  stable types). Nothing on a read path looks a string up by value — resolution is by the `int` key — so the
  column is never indexed and carries no length ceiling: `nvarchar(max)` / `text` on every engine. Its
  `FirstSeenUtc` records when the type first appeared in this spelling form — i.e. when a classification
  change (stable ↔ mapped) took effect, which the name history does not capture when that change involves no
  rename.
- **`TypeIds.CurrentName`** is the denormalized current fully-qualified name, so the identity table is
  self-describing: *id 7 is `MyApp.Sales.PurchaseOrder`* in one row read.
- **`TypeNames`** is the append-only trail of every name the type has been known by; the current name is the
  highest `Seq` (and is mirrored in `CurrentName`). For a mapped type, whose persisted spelling is a
  rename-invariant GUID, this is where renames are recorded — including for constructed generics, whose
  resolved name changes even when their GUID-bearing spelling does not.

## In-memory model

Each process builds, once on first use, from a full load of `TypeIds` + `TypeStrings`:

- `int -> Type` for resolution, and `Type -> int` for interning.

Both are produced by resolving each `TypeStrings` spelling to a `Type` and collapsing by `TypeId`: every
spelling of one concept resolves to the same `Type`, folding into a single entry. Stable spellings resolve by
name ([StableLeafTypeIdentifier.cs:12-14](../../src/Compze.TypeIdentifiers/StableLeafTypeIdentifier.cs#L12-L14)); mapped spellings resolve by GUID
([MappedTypeIdentifier.cs:14](../../src/Compze.TypeIdentifiers/MappedTypeIdentifier.cs#L14)). A spelling that no longer resolves is skipped (see
*Deleted and unresolvable types*).

At runtime, interning and resolution are normally cache hits. The database is touched by the first-use load,
the reconciliation writes below, minting a never-before-seen type, and a cache-miss re-check — an `int` or
`Type` absent from the model triggers a fresh load, since another process may have interned it since.

## Renaming and reconciliation

Two spellings are known to denote the same concept when they resolve to the **same** `Type` at the same time.
That equivalence is recordable only while both forms still resolve, which fixes the operational rule:

> A type may be renamed safely only while it is **mapped** (the GUID is the rename-proof anchor). To switch a
> type to mapped and then rename it, deploy the two steps separately: deploy the mapping with the type under
> its existing name and let the application run once, then deploy the rename. The two cannot be combined in a
> single deployment.

The first deployment runs with both the old name form and the new GUID form resolving to the same `Type`, so
the reconciliation pass links them under one id. From then on the GUID carries the identity, and the rename in
the second deployment leaves every prior row resolving correctly through the shared id. (A type that stays
stable has no rename-proof anchor and cannot be renamed safely — another reason to map before renaming.)

## Reconciliation (first use)

On first use the interner reconciles every already-persisted type it can currently resolve, writing any
corrections under its cross-process write lock, so the database records this deployment's current view:

1. **Link the current spelling.** Compute the type's current canonical spelling under this deployment's
   mappings; if `TypeStrings` does not already hold it, insert it pointing at the same `TypeId` (stamped with
   `FirstSeenUtc`). This is what links a reclassified type's new spelling — e.g. a type persisted as a stable
   name that is now mapped — to its existing id.
2. **Record a rename.** Compute the type's current normalized fully-qualified name; if it differs from the
   recorded current name, append a `TypeNames` entry (next `Seq`, `FirstSeenUtc`) and update
   `TypeIds.CurrentName`.

The pass is idempotent and cheap, and a no-op once a deployment's spellings and names are recorded. A type
seen for the very first time is minted on demand the first time it is interned — its `TypeIds` row, first
spelling, and first name entry written together under the same lock.

## Deleted and unresolvable types

A stored spelling that no longer resolves to a .NET type — a removed type, or one renamed in a deployment that
skipped the map-first step — is simply skipped when the model loads. It contributes no `int -> Type` entry;
its rows sit inert.

Nothing fails at load. A failure surfaces only if something calls `GetTypeId(id)` for such an id, and the only
callers are read paths resolving a stored row (`Tevent.TeventType`, `Store.ValueTypeId`, inbox/outbox
`TypeId`). So a throw means surviving data still references the type — it is not truly dead — and failing
loud, naming the id and its last-known name (*"interned type id 7, last known as
`MyApp.Sales.PurchaseOrder`, no longer resolves…"*), is the correct response.

This is why deleted types need no special handling: a genuinely dead type has no surviving rows pointing at
its id, so it is never looked up; a type that is looked up has surviving rows, so it is not dead. There is no
eager scan and no decommission flag to maintain.

## Indexing strategy

`TypeString` is not indexed for reads — resolution is by the `int` key. Dedupe-on-insert (one spelling maps to
exactly one `TypeId`) is guaranteed by a cross-process lock rather than a unique index:

All interner writes — the reconciliation pass and any runtime mint — run under a DB advisory lock
(`sp_getapplock` on SQL Server, `pg_advisory_lock` on PostgreSQL, `GET_LOCK` on MySQL; SQLite is single-writer
and joins the ambient transaction instead). Inside the lock the in-memory `string -> id` map is the dedupe
authority. The only indexes are the `TypeIds.Id` PK, the `TypeStrings.TypeId` FK index, and the
`TypeNames(TypeId, Seq)` key — none on any string, so the string is genuinely unindexed payload of any length
on every engine.

(The considered alternative — a `binary(32)` hash column with a unique index as a database-level dedupe
backstop — was rejected: the lock the reconciliation pass already needs makes the extra index and its
per-spelling round-trips redundant.)

## Referencing tables

The interned `int` is the type reference in `Tevent.TeventType`, `Store.ValueTypeId`
([Store schema](../../src/Compze.DocumentDb/Internal/SqlLayer/IDocumentDbSqlLayer.cs)), and the inbox/outbox
`TypeId` columns. All are `int` referencing `TypeIds.Id`.

The interner pieces — [`ITypeIdInternerPersistence`](../../src/Compze.TypeIdentifiers.Interning/ITypeIdInternerPersistence.cs)
and [`TypeIdInterner`](../../src/Compze.TypeIdentifiers.Interning/TypeIdInterner.cs) — live in the standalone
`Compze.TypeIdentifiers.Interning` package and carry the three-table operations, the in-memory maps, and the
reconciliation pass. The engine-specific `ITypeIdInternerPersistence` implementations stay in each SQL engine
project. A single `TypeIdInterner` serves every engine;
its caching of the `Type -> id` direction is gated by the persistence's `MintsAreImmediatelyDurable` flag
(true for the MVCC engines, false for single-writer SQLite, where a mint can roll back with the business
transaction).
