# DocumentDb document-id: Guid vs string inconsistency

The document **id** (distinct from the **type** identity, which the int-interning work already
settled) is represented inconsistently across the DocumentDb layers. Worth resolving as its own change.

## Current state

- **Public API is entirely `Guid`-keyed.** Every entry point constrains to `IEntity<Guid>`:
  `Save`/`Delete`/`TryGet(Guid)`/`GetAll(...)`/`GetAllIds()` ([IDocumentDB.cs](../../src/Compze.DocumentDb/Public/IDocumentDB.cs),
  [IDocumentDbUpdater.cs](../../src/Compze.DocumentDb/Public/IDocumentDbUpdater.cs),
  [IDocumentDbBulkReader.cs](../../src/Compze.DocumentDb/Public/IDocumentDbBulkReader.cs)).
- **Storage key is a string.** The `Store.Id` column is `TEXT`/`nvarchar`. The key is produced by
  `GetIdString(object id) => id.ToStringNotNull().ToUpperInvariant().TrimEnd(' ')`
  ([DocumentDb.cs](../../src/Compze.DocumentDb/Private/DocumentDb.cs)) — i.e. the Guid uppercased with
  trailing spaces trimmed. The uppercasing/trim is deliberate MsSql-compat behaviour (see the
  `ObjectsWhoseKeysDifferOnlyBy…` specs).
- **The SQL-layer contract is split** ([IDocumentDbSqlLayer.cs](../../src/Compze.DocumentDb/Internal/SqlLayer/IDocumentDbSqlLayer.cs)):
  - `string` id: `TryGet`, `Add` (`WriteRow.Id`), `Remove`, `Update`.
  - `Guid` id: `GetAllIds()` returns `IEnumerable<Guid>`; `GetAll(IEnumerable<Guid> ids, …)` takes Guids.
  - The Guid paths round-trip through the string column: `GetAllIds` parses with `reader.GetGuidFromString(0)`;
    `GetAll(ids)` formats with `ids.Select(id => id.ToString()).Join("','")` into an `IN` list.
- Even the public surface mixes representations: `IDocumentDbReader.GetAll(IEnumerable<EntityId<Guid>>)`
  vs `IDocumentDB.GetAll(IEnumerable<Guid>)` — `EntityId<Guid>` in one place, raw `Guid` in another.

So the same logical key is a `Guid` at the public boundary, a `string` for point operations in the SQL
layer, and a `Guid` again for the two bulk operations — with string⇄Guid conversions straddling the seam.
(The `//Urgent: This whole Guid vs string thing must be fixed.` comment that used to sit on
`GetAllIds` has been removed; this doc replaces it.)

## The decision to make

Pick one identity model and make every layer speak it:

1. **Uniformly `Guid`** — the public API is already Guid-only, so type the SQL-layer contract as `Guid`
   throughout (and consider storing it natively rather than as an uppercased string). Most faithful to
   today's intent; loses the "arbitrary string key" option.
2. **Uniformly `string`** — drop the `IEntity<Guid>` constraint from the bulk APIs and treat documents as
   genuinely string-keyed. Most flexible; a bigger public-API change and gives up Guid type-safety.
3. **A strong key type** (`EntityId<Guid>`, already present) used consistently end to end, with the
   string column as a pure storage detail behind it.

Whichever is chosen, the goal is a single id representation per layer with conversions confined to the
persistence boundary — mirroring how the type-identity work confined the canonical string to the interner.

## Out of scope / already done

Type identity (the `TypeId`/interning work) is settled and unrelated. This is purely about the **document
key**.
