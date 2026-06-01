# Persisted type identity — storage design

How the SQL layer represents a .NET type as a small, durable, database-local `int` — stable across
reclassification (stable ↔ mapped) and renaming — and keeps a full, durable history of every fully-qualified
name each type has ever had, with the current name clearly marked.

Status: **design agreed; one sub-decision open (indexing strategy, below).** No code yet.

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

Each process builds, once at startup from a full load of `TypeIds` + `TypeStrings`:

- `int -> Type` for resolution, and `Type -> int` for interning.

Both are produced by resolving each `TypeStrings` spelling to a `Type` and collapsing by `TypeId`: every
spelling of one concept resolves to the same `Type`, folding into a single entry. Stable spellings resolve by
name ([StableLeafTypeIdentifier.cs:12-14](../../src/Compze.TypeIdentifiers/StableLeafTypeIdentifier.cs#L12-L14)); mapped spellings resolve by GUID
([MappedTypeIdentifier.cs:14](../../src/Compze.TypeIdentifiers/MappedTypeIdentifier.cs#L14)). At runtime, interning and resolution are pure cache hits; the database is
touched only by the startup load and by the reconciliation writes below.

## Renaming and reclassification

Two spellings are known to denote the same concept when they resolve to the **same** `Type` at the same time.
That equivalence is recordable only while both forms still resolve, which fixes the operational rule:

> A type may be renamed safely only while it is **mapped** (the GUID is the rename-proof anchor). To switch a
> type to mapped and then rename it, deploy the two steps separately: deploy the mapping with the type under
> its existing name and let the application start once, then deploy the rename. The two cannot be combined in
> a single deployment.

The first deployment runs with both the old name form and the new GUID form resolving to the same `Type`, so
the reconciliation pass links them under one id. From then on the GUID carries the identity, and the rename in
the second deployment leaves every prior row resolving correctly through the shared id. (A type that stays
stable has no rename-proof anchor and cannot be renamed safely — another reason to map before renaming.)

## Reconciliation pass (startup)

On startup, after type registration and inside the interner's write serialization, the interner walks each
distinct `Type` reachable from the existing `TypeStrings` rows (this is exactly "every type that has data",
leaf and constructed alike — a finite set):

1. **Resolve** the type from its stored spelling.
2. **Reconcile its spelling:** compute the type's current canonical persisted spelling; if it is not already
   in `TypeStrings`, insert it (pointing at the same `TypeId`, stamped with `FirstSeenUtc` from the ambient
   time source). A type that has no id yet (first time seen) gets a freshly minted `TypeIds` row.
3. **Reconcile its name:** compute the type's current normalized fully-qualified name; if it differs from the
   type's latest `TypeNames` entry, append a new entry (next `Seq`, `FirstSeenUtc` from the ambient time
   source) and update `TypeIds.CurrentName`.

The pass is idempotent and cheap (one resolve per distinct type, once at startup), and is a no-op once a
deployment's spellings and names are all recorded. Runtime persistence of a never-before-seen type follows the
same mint-id / record-spelling / record-name steps.

## Guardrail

A stored spelling that fails to resolve at startup is the signature of a rename deployed without its mapping
step first — which would otherwise strand the data referencing it. The reconciliation pass treats an
unresolvable spelling as a hard startup failure with an actionable message that names the affected id and its
last-known name (*"id 7, last known as `MyApp.Sales.PurchaseOrder`, no longer resolves…"*).

The check is **scoped to types whose assemblies this process registers** (mapped or stable): an unresolvable
spelling for a wholly-unregistered assembly is simply "not this process's type" and is ignored, keeping the
check free of false positives in multi-context deployments. A deliberately decommissioned type — removed on
purpose, its data abandonable — passes via an explicit acknowledgement escape hatch.

## Indexing strategy (open: A vs B)

`TypeString` is not indexed for reads. Dedupe-on-insert — the guarantee that a spelling maps to exactly one
`TypeId` — is provided one of two ways:

- **A (working design):** all interner writes (the reconciliation pass and any runtime mint) run under a
  cross-process lock — a DB advisory lock (`sp_getapplock` / `pg_advisory_lock` / `GET_LOCK`) or a
  `Compze.Threading` interprocess primitive. Inside the lock the in-memory `string -> id` map is the dedupe
  authority. The only indexes are the `TypeIds.Id` PK, the `TypeStrings.TypeId` FK index, and the
  `TypeNames(TypeId, Seq)` key — none on any string. The string is genuinely unindexed payload on every
  engine, and the serialization point the reconciliation pass needs already exists.
- **B (alternative):** add `TypeStringHash binary(32)` with a UNIQUE index and dedupe on the bounded hash,
  keeping a database-level uniqueness backstop. `TypeString` stays unindexed. Trades a derived index and
  per-spelling round-trips for not relying on the lock.

## Referencing tables

The interned `int` is the type reference in `Tevent.TeventType`, `Store.ValueTypeId`
([Store schema](../../src/Compze.DocumentDb/Internal/SqlLayer/IDocumentDbSqlLayer.cs)), and the inbox/outbox
`TypeId` columns. All are `int` referencing `TypeIds.Id`.

The interner pieces — [`ITypeIdInternerPersistence`](../../src/Compze.Internals.Sql.Common/Abstractions/ITypeIdInternerPersistence.cs),
[`TypeIdInterner`](../../src/Compze.Internals.Sql.Common/TypeIdInterner.cs), and its MVCC / single-writer split
(`SuppressedTransactionTypeIdInterner` / `AmbientTransactionTypeIdInterner`) — carry the three-table
operations, the in-memory maps, and the reconciliation pass.

## Open questions

1. **Indexing A vs B** — the one decision left.
2. **Table / column names** — `TypeIds` / `TypeStrings` / `TypeNames` proposed.
3. **Decommission acknowledgement** — the shape of the escape hatch that lets a deliberately-removed type's
   unresolvable spelling pass the guardrail.
