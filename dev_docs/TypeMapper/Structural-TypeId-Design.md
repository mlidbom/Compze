# Structural TypeId Design

> **No backward compatibility constraints.** There are zero deployed applications using this system. No persisted data exists. Any format, ID, or behavior can change freely.

> **Draft.** Direction, not specification. The goal is a simpler, more transparent system — adjust as we go.

## Context

The current TypeMapper replaces entire `$type` strings (e.g. `"System.Collections.Generic.List`1[[MyApp.MyEntity, MyApp]], System.Private.CoreLib"`) with opaque GUIDs. This requires:
- Scanning every loaded assembly to discover all closed generic instantiations
- Computing deterministic GUIDs for every closed generic and array type
- Maintaining a global bidirectional dictionary of every type ever seen
- A static singleton with assembly-load hooks

All of this exists because the system flattens the *structurally recursive* type name into a *single opaque GUID*, then needs to reconstruct the full type from that GUID on deserialization.

## Insight

The Newtonsoft `$type` string **already encodes the full component structure** — the open generic definition, every type argument, arrays, nesting — all in a standard recursive format.

If we replace only the *leaf component names* within that string (keeping the structural brackets intact), we only need mappings for leaf types and open generic definitions. The string itself carries the composition. No deterministic GUID computation. No assembly scanning for closed generics.

## Design

### Persisted format

**Standard `AssemblyQualifiedName` structure** — the persisted format is not custom. It uses the same `TypeName, Assembly` pairing with `[[ ]]` nesting that .NET uses natively. The only differences from a raw `AssemblyQualifiedName`:

- **Mapped types**: the type name is replaced with a GUID, and the assembly name is replaced with a placeholder (`0`) meaning "resolve via mapping dictionary"
- **Stable types**: kept as-is — real type name, real assembly name

**All mapped types use GUIDs.** No arbitrary string IDs — GUIDs are reliable, no risk of collisions or naming mistakes.

Examples:

Leaf type:
```json
{ "$type": "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0" }
```

Closed generic with mapped element type and stable definition:
```json
{ "$type": "System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib" }
```

Closed generic with mapped definition and mapped argument:
```json
{ "$type": "a1b2c3d4-e5f6-7890-abcd-ef1234567890[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], 0" }
```

Multi-argument generic:
```json
{ "$type": "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib" }
```

Array of mapped type:
```json
{ "$type": "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c[], 0" }
```

Nested generics:
```json
{ "$type": "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib]], System.Private.CoreLib" }
```

### Resolution on deserialization

The persisted format is standard `AssemblyQualifiedName` structure, so the parser handles both Newtonsoft's raw output (serialize direction) and our persisted form (deserialize direction) — same format, different leaf values.

For each `TypeName, Assembly` component:
- **Assembly is `0`** → type name is a GUID → look up in the mapping dictionary → get the current .NET `Type`
- **Assembly is a real name** → stable type → resolve via `Assembly.GetType(typeName)` within the registered stable assemblies
- **Structural composition** (generics, arrays) → reconstruct the .NET `Type` using `Type.MakeGenericType()`, `Type.MakeArrayType()` etc. after resolving all components

### Caching

Both directions are cached on the `ITypeMapper` instance:
- **Serialize**: `Type` → `TypeId`. Once computed, subsequent serializations of the same type are a dictionary lookup.
- **Deserialize**: `TypeId` string → `Type`. Once resolved, subsequent deserializations of the same string are a dictionary lookup.

Parsing and tree-walking happen only on first encounter. Container-scoped caches are naturally garbage-collected with the container.

---

## TypeId hierarchy

`TypeId` is the identity of a fully constructed type. It is an abstract base with three concrete subtypes, each representing a different kind of identity:

```
TypeId (abstract)
├── MappedTypeId      — leaf type from a mapped assembly. Has a GUID. SQL-storable.
├── StableNameTypeId  — type(s) entirely from stable assemblies. Untouched AssemblyQualifiedName.
└── ConstructedTypeId — mixed: AssemblyQualifiedName with some GUID, 0 components.
```

All three expose a **string representation** — the `AssemblyQualifiedName`-format string used by serialization. Only `MappedTypeId` additionally exposes a **GUID** for SQL storage.

### MappedTypeId

A leaf type from a mapped assembly. Has an explicitly assigned GUID. String representation is `"GUID, 0"`.

This is the only subtype that can be stored in SQL GUID columns. The event store, document DB, tessaging outbox/inbox, and Typermedia routing all deal in `MappedTypeId`.

### StableNameTypeId

A type — leaf or composite — where every component comes from a stable assembly. The string representation is the unmodified `AssemblyQualifiedName`. Resolution is trivial: pass it directly to `Type.GetType()`.

Examples: `System.String`, `List<string>`, `Dictionary<string, int>`.

### ConstructedTypeId

A composite type where at least one component is mapped. The string representation is an `AssemblyQualifiedName`-format string with `GUID, 0` in place of mapped components.

Examples: `List<MyEntity>` → `"System.Collections.Generic.List`1[[e4a8c9f2-..., 0]], System.Private.CoreLib"`.

Resolution requires walking the string, resolving GUID components via the mapping dictionary, and reconstructing with `Type.MakeGenericType()` / `Type.MakeArrayType()`.

### TypeId is a pure identity value

`TypeId` subtypes are dumb value objects — they hold only the string representation (and GUID for `MappedTypeId`). They do not participate in parsing or resolution. That logic lives in the `TypeNameMapper`.

### Open generic mappings are NOT TypeIds

An open generic like `List<>` is not a type — it's a template. It never exists at runtime, never gets serialized, never gets stored, never crosses the wire. Its GUID mapping exists solely as a building block for constructing and parsing `ConstructedTypeId` strings.

This mapping is represented by `OpenGenericId` — a GUID-backed struct, distinct from `TypeId`. The `ITypeMapper` may handle the `OpenGenericId` ↔ open generic `Type` mapping internally, but `OpenGenericId` is never returned where a `TypeId` is expected. TypeIds name fully constructed types; `OpenGenericId` names a template.

---

## Two tiers of type handling

1. **Stable assemblies** — types whose names won't change. Their full `TypeName, Assembly` pairs pass through into the persisted form untouched.
   - .NET runtime assemblies are stable by default (detected automatically via public key token or well-known names)
   - Users can declare additional assemblies stable:
     ```csharp
     mapper.UseStableNameStrategyForAssembliesContaining<NodaTime.Instant, SomeOtherLib.Foo>();
     ```

2. **Mapped assemblies** — types that need rename-safety. Leaf types get GUID assignments (`MappedTypeId`). Open generic definitions get GUID assignments (not TypeIds — building blocks only). Registered explicitly per-container:
   ```csharp
   mapper.MapTypesFromAssemblyContaining<MyEntity>();
   ```

---

## Assembly-level mapping declaration

Each assembly declares its mappings via an assembly-level attribute. Registration is fluent:

```csharp
[assembly: TypeMappings(typeof(MyAssemblyTypeMappings))]

static class MyAssemblyTypeMappings : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar registrar)
   {
      registrar
         .Map<MyEntity>("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c")
         .Map<IOtherEntity>("f5a9d1b3-8c4e-4a2f-b7d6-3e1c9f0a5b8d")
         .MapOpenGeneric(typeof(MyGenericThing<>), "a1b2c3d4-e5f6-7890-abcd-ef1234567890");
   }
}
```

`MapOpenGeneric` is a separate method from `Map` to make it explicit that open generics are not types — they are templates. This mapping produces an `OpenGenericId`, not a `TypeId`. In practice, `MapOpenGeneric` is rarely needed — most generics used in serialized data come from stable assemblies (BCL collections, etc.).

The framework enforces that a mapping class only maps types from its own assembly. Mapping a type from another assembly is an error at registration time.

---

## Per-container registration (no global state)

```csharp
container.RegisterTypeMapper(mapper =>
{
   mapper
      .MapTypesFromAssemblyContaining<MyEntity>()
      .MapTypesFromAssemblyContaining<AnotherDomainType>()
      .UseStableNameStrategyForAssembliesContaining<SomeLibraryType>();
});
```

- No `AppDomain.AssemblyLoad` hook
- No static singleton
- No auto-discovery
- Each container owns its `ITypeMapper` instance with its own mappings
- If a container didn't register an assembly's mappings, serializing those types is an error

---

## Serialization / deserialization pipeline

Three separate classes, each with one job:

1. **`TypeNameParser`** — parses a standard `AssemblyQualifiedName`-format string into a tree of `TypeName, Assembly` components with nested type arguments. Works on both Newtonsoft's raw output and our persisted form (same structure). Independently testable, separate class, no mapping knowledge.

2. **`TypeNameMapper`** — walks a parsed tree and transforms it:
   - **Serialize direction**: Given a .NET `Type`, produces a `TypeId` (choosing the correct subtype). For mapped types: replaces type name with GUID, assembly with `0`. For stable types: passes through unchanged. For composites: walks components recursively.
   - **Deserialize direction**: Given a `TypeId`, resolves it back to a .NET `Type`. `MappedTypeId` → dictionary lookup. `StableNameTypeId` → `Type.GetType()`. `ConstructedTypeId` → parse, resolve components recursively, `MakeGenericType()` / `MakeArrayType()`.
   - Caches results in both directions.

3. **`RenamingDecorator`** — finds `$type` values in JSON, delegates to `TypeNameMapper`, puts the result back. Stays trivial (~20 lines).

---

## SQL storage

TypeId columns in the event store, document DB, tessaging, and Typermedia remain GUID columns. These accept only `MappedTypeId` — enforced at the type level. This is not a limitation in practice: events and documents are concrete leaf domain types, not generic collections.

---

## What goes away

- `DeterministicTypeId` / UUID v5 composition entirely
- `ComputedTypeIdType` branch of the old `TypeMapperType` hierarchy
- `TypeMapperAssemblyScanner.CollectClosedGenericTypes`
- The `AppDomain.AssemblyLoad` hook and `ReentrancyGuard`
- The static singleton `TypeMapper.Instance`
- The geometric explosion problem — it ceases to exist

## What changes

- `TypeId` — from a single GUID-backed type to an abstract base with three subtypes
- `ITypeMapper` — becomes container-scoped, explicitly configured. Produces `MappedTypeId` for leaf types. May handle `OpenGenericId` ↔ `Type` mappings internally, but `OpenGenericId` is not a `TypeId`.
- `TypeMapper` — no longer a static singleton
- `RenamingDecorator` — delegates structural work to `TypeNameParser` + `TypeNameMapper`
- Generated mapping files — only contain leaf types and open generic definitions
- Persisted `$type` strings — structural `AssemblyQualifiedName` format with GUIDs for mapped components

## What stays

- Leaf types get explicit GUID assignments
- SQL schema stays GUID-based
- `GetType(MappedTypeId)` / `GetId(Type)` for leaf types — same dictionary lookup

## Resolved questions

- **Open generic mapping name**: `OpenGenericId` — a GUID-backed struct, separate from `TypeId`.
- **Stable assembly detection**: At **setup time**, assemblies are checked by public key token to determine stability. At **parse time**, the `$type` string only contains assembly names (no tokens), so stable assembly lookup is by name against the pre-built set. Microsoft uses a small, known set of public key tokens:
  - `7cec85d7bea7798e` — `System.Private.CoreLib`
  - `b03f5f7f11d50a3a` — most `System.*` runtime libraries
  - `b77a5c561934e089` — legacy (`mscorlib`, `System`, `System.Core`)
  - `cc7b13ffcd2ddd51` — `System.Private.Xml`, `netstandard`, etc.
  - `31bf3856ad364e35` — `Microsoft.*` libraries

  These are hardcoded as stable by default. Users can also register additional stable assemblies by public key token:
  ```csharp
  mapper.UseStableNameStrategyForPublicKeyToken("xxxxxxxxxxxx"); // all assemblies signed with this token
  mapper.UseStableNameStrategyForAssembliesContaining<NodaTime.Instant>(); // or by marker type
  ```
