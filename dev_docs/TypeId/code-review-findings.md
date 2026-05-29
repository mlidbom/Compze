# Code review — `simplify_typeid_structure`

Review of the branch that extracts `Compze.TypeIdentifiers`, switches all four SQL engines from a
type GUID to a per-database interned `int`, and threads `TypeId` through the datastores.

**The SQL interner and datastore changes are correct.** Verified: SQLite reuses the ambient-transaction
connection (so `InsertOrGet`'s two `UseCommand` calls see each other), `AUTOINCREMENT`/IDENTITY prevent
id reuse (so caching `id→string` is safe), the upsert paths are atomic and never rely on MySQL
`LAST_INSERT_ID()`, reads intern-before-connect and resolve-after-close, and column-type/reader ordinals
match across engines. Wiring is complete; the interner is a per-database singleton on every engine.

All open findings are in the new parser/mapper.

## 1. Jagged arrays lose rename-safety (correctness) [FIXED]

`TypeIdentifier.Parse` strips only **one** trailing array suffix (no loop), and `GenericTypePartPattern`
(`^(.+?)(\[\[.+\]\])$`) requires the type part to end in `]]`. So `List`1[[MappedLeaf, MappedAsm]][][]`
(jagged array of a generic) has its final `[]` stripped, leaving a string ending in `[]` not `]]` — the
generic match fails and it collapses into a `StableLeafTypeIdentifier` holding the raw text.

`TransformToPersisted` then never descends into the type arguments, so a **mapped** component nested in a
jagged array is persisted by its raw type name instead of its GUID:

- `List<MappedLeaf>[]` → arg stored as `GUID` — rename-safe ✓
- `List<MappedLeaf>[][]` → arg stored as `MappedLeaf.FullName, MappedAsm` — rename-safe ✗

Reachable via the renaming serializer, which transforms arbitrary `$type` AQNs in object graphs (not just
root entity types). Silent until `MappedLeaf` is renamed/moved — then the persisted string fails
`Type.GetType` and throws `"Could not resolve stable type"`, defeating the library's core promise. Plain
`int[][]` happens to still resolve (the `[]` text survives in the leaf name), so the bug only bites when a
mapped or generic component is nested.

*Fix direction:* loop the array-suffix stripping (and re-wrap nested ranks), or make the generic match
tolerate a trailing array suffix. Add specs for jagged and nested arrays of mapped/generic types.

- [TypeIdentifier.cs:55-75](../../src/Compze.TypeIdentifiers/TypeIdentifier.cs#L55-L75)

## 2. Cache lost-update race can permanently poison an identifier (correctness, startup-only)

`GetId` uses `ConcurrentDictionary.GetOrAdd`, whose factory (`ComputeId`) runs outside any lock and reads
the mapping dictionaries. `AddLeafTypeMapping`/`AddStableAssemblyName` mutate state then call
`ClearCaches()`. If a reader computes an identifier against the pre-mapping state and stores it *after* a
concurrent registration's `ClearCaches()` runs, the stale identifier sticks permanently — `GetId(type)`
then returns the non-mapped/stable form forever instead of the GUID-mapped one.

Reachable only with concurrent registration + lookup; if all `MapTypesFromAssembly` calls complete before
requests flow there is no race. Consequence is a permanent wrong persisted identity, not a transient miss.

- [TypeNameMapper.cs:87](../../src/Compze.TypeIdentifiers/TypeNameMapper.cs#L87)
- [TypeMapper.cs:68](../../src/Compze.TypeIdentifiers/TypeMapper.cs#L68)

## 3. `GetIdsForTypesAssignableTo` omits assignable generic/constructed types (latent)

`ComputeAssignableTypeIds` scans only `RegisteredLeafTypes` (explicitly `Map<T>()`-ed leaves), with a
fallback that adds the queried type itself *only* when zero leaves match. A generic/constructed subtype
assignable to the queried base is silently skipped whenever at least one leaf also matches. Via the sole
consumer `DocumentDb.AcceptableTypeIds<TBase>`, `GetAll<TBase>()` would return leaf-typed documents but
omit generic-typed ones — no error, just missing rows. Latent today (documents are concrete leaf types);
confirm the dropped invariant is intentional.

- [TypeMapper.cs:101-116](../../src/Compze.TypeIdentifiers/TypeMapper.cs#L101-L116)

## Lower-priority notes

- **Persistence duplication.** The four `*TypeIdInternerPersistence` classes are near-identical
  (`LoadAll`/`GetById`/`TryGetId`/`NullableInt` are byte-for-byte the same). A shared base in
  `Compze.Internals.Sql.Common` with insert-dialect + add-parameter hooks would remove the drift risk.
  Consistent with the existing per-engine pattern, so optional.
- **DocumentDb PK is now a database-local int**, not a self-describing GUID. Documented as intentional on
  `ITypeIdInterner`, but a table-only backup/restore that desyncs `Store` from `TypeIds` resolves to the
  wrong type or throws. Worth an ops-docs note since the old GUID column was portable.
