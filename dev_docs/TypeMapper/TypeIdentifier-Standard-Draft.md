# TypeIdentifier Standard — Draft

> **Status:** Working draft. Not yet published.

## Problem

Software systems that persist, transmit, or route by .NET type — event stores, document databases, message buses, JSON serializers — need a stable string representation of types. The standard approach (`AssemblyQualifiedName`) breaks when types are renamed, moved between assemblies, or when assemblies are versioned. Every system reinvents an incompatible workaround. None support migration from existing persisted type strings.

## Scope

This standard defines:
- A **string format** for deterministic, reversible type identification
- **Resolution rules** for reconstructing a .NET `Type` from a TypeIdentifier string
- A **legacy migration mechanism** for transitioning from existing persisted type strings
- An optional **UUIDv5 appendix** for deriving deterministic GUIDs from TypeIdentifier strings

The standard targets languages with **reified generics** (.NET, and conceptually compatible runtimes). Languages with type erasure (Java) or monomorphization (Rust) are out of scope.

## Core Principle

**The string is the canonical identity.** All other representations (GUIDs, in-memory objects) are derived from or resolved via the string. Two TypeIdentifiers are equal if and only if their string representations are identical.

---

## 1. String Format

A TypeIdentifier string uses **ECMA-335 `AssemblyQualifiedName` structure** — the same `TypeName, Assembly` pairing with `[[ ]]` nesting that .NET defines natively. The difference from a raw `AssemblyQualifiedName`: within that structure, individual type components may use mapped or legacy identifiers instead of current CLR names.

### 1.1 Component Forms

Each `TypeName, Assembly` pair within a TypeIdentifier string is one of two forms:

#### Mapped

```
GUID, 0
```

The type has an explicitly assigned GUID. The assembly portion is the literal string `0`, meaning "resolve via mapping dictionary." The GUID uses RFC 4122 format: lowercase hexadecimal with dashes (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`).

Example: `e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0`

#### Named

```
TypeName, AssemblyName
```

A CLR type name and assembly name. This form covers both current type names and historically persisted names (legacy). The string format is identical in both cases — the distinction is made at resolution time, not at the format level.

Examples:
- Current: `System.String, System.Private.CoreLib`
- Legacy: `MyApp.OldNamespace.CustomerCreated, MyApp.Events`

### 1.2 Composition

Types compose via ECMA-335 bracket syntax. Generic type arguments are enclosed in `[[ ]]`, with multiple arguments separated by `],[`. A single TypeIdentifier string may freely mix both component forms.

#### Closed generic — stable definition, mapped argument
```
System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib
```

#### Closed generic — mapped definition, mapped argument
```
a1b2c3d4-e5f6-7890-abcd-ef1234567890[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], 0
```

#### Multi-argument generic
```
System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib
```

#### Array of mapped type
```
e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c[], 0
```

#### Nested generics
```
System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib]], System.Private.CoreLib
```

### 1.3 Open Generic Definitions

Open generic definitions (e.g., `List<>`, `Dictionary<,>`) are leaf types. They require their own GUID registration if they are mapped types, just like any other leaf type. The arity backtick suffix (`` `1 ``, `` `2 ``) is part of the type name, not structural syntax.

### 1.4 Reserved Field

In the mapped component form `GUID, 0`, the `0` is a reserved string field. Its current value is the literal `0` with no semantic meaning. Implementations MUST write `0` and MUST accept `0`. Future versions of this standard may assign meaning to this field. Using a string (rather than an integer) ensures future values can carry arbitrary information without a format-breaking change.

### 1.5 Canonicalization

For two TypeIdentifier strings to be comparable, they must be in canonical form:

- **GUIDs**: lowercase hexadecimal with dashes, RFC 4122 format
- **Whitespace**: a single space after each comma separating `TypeName` from `Assembly` within a component. No other whitespace.
- **Type names**: the CLR type name as returned by `Type.FullName` (without assembly qualification)
- **Assembly names**: short assembly name only — no Version, Culture, or PublicKeyToken qualifiers.

### 1.6 Normalization on Read

Implementations MUST accept Named components with full assembly-qualified names (including Version, Culture, PublicKeyToken) and MUST normalize them to canonical form (short assembly name only) immediately on read, before any further processing. This means:

1. Parse the assembly portion of each Named leaf component
2. Strip everything after the short assembly name (Version, Culture, PublicKeyToken qualifiers)
3. Use the normalized string for all downstream operations: cache keys, legacy dictionary lookup, UUIDv5 derivation, string equality, and write-back

This ensures that persisted data written by other systems (e.g., Newtonsoft `$type` with full `AssemblyQualifiedName`) resolves correctly without requiring explicit legacy mappings for every framework type. It also ensures UUIDv5 derivation is stable across .NET version upgrades, since framework assembly versions change between releases.

> **Out of scope:** Side-by-side assembly versions distinguished only by assembly qualifiers (Version, PublicKeyToken) are not supported. Normalization collapses them to the same identity. Types that share a fully qualified name across assembly versions MUST use the Mapped form (GUID) instead.

---

## 2. Resolution

Given a TypeIdentifier string, resolution produces a .NET `Type`. The process has two phases:

**Phase 1 — Structural parsing.** Always happens first. The ECMA-335 bracket structure is parsed to identify leaf components and their structural relationships (generic definition + arguments, array element type, etc.). This produces a tree of leaf `TypeName, Assembly` pairs plus structural operators.

**Phase 1.5 — Normalization.** After parsing, each Named leaf component is normalized to canonical form: assembly qualifiers (Version, Culture, PublicKeyToken) are stripped, leaving only the short assembly name. All subsequent operations use the normalized string. (See Section 1.6.)

**Phase 2 — Leaf resolution.** Each leaf component is resolved independently:

- **Mapped** (`GUID, 0`): Look up GUID in the mapped type dictionary → .NET `Type`.
- **Named** (`TypeName, AssemblyName`): Check the legacy mapping dictionary for the normalized component string → if found, return the mapped `Type`. Otherwise, resolve via the runtime (e.g., `Type.GetType("TypeName, AssemblyName")` or equivalent).

After all leaves are resolved, structural composition reconstructs the final .NET `Type` via `Type.MakeGenericType()`, `Type.MakeArrayType()`, etc.

**Legacy mappings are per-leaf-component, not per-full-string.** A migration scanner generates legacy mappings for leaf types and open generic definitions. Constructed types (closed generics, arrays) do not need their own legacy mappings — they resolve through structural parsing of their leaf components.

Both directions (Type → string, string → Type) SHOULD be cached by the implementation. Parsing and resolution happen once per unique string; subsequent lookups are dictionary hits.

---

## 3. Type Mapping Registration

An implementation maintains two dictionaries:

### 3.1 Mapped Type Dictionary

Bidirectional: `Guid ↔ Type`. Populated by explicit registration, typically via assembly scanning with a marker attribute. Every leaf type and every open generic definition that participates in the mapped component form MUST have an entry.

### 3.2 Legacy Mapping Dictionary

Unidirectional: `string → Type`. Maps prior `TypeName, AssemblyName` strings to current .NET `Type`s. Used for migration from existing systems. See Section 4.

---

## 4. Legacy Migration

An application with existing persisted type strings (e.g., full `AssemblyQualifiedName`s in an event store) can migrate to TypeIdentifiers:

1. **Scan assemblies** containing the persisted types
2. **Generate a legacy mapping** for each type: from its prior `AssemblyQualifiedName` to the current .NET `Type`
3. **Register the legacy mappings** in the legacy type dictionary
4. **Assign GUIDs** to each type for use in new persisted data

After migration, the system can resolve both old (legacy) and new (mapped) TypeIdentifier strings. Types can then be freely renamed — the legacy mappings preserve compatibility with historically persisted data, and new data uses stable GUIDs.

---

## 5. UUIDv5 Derivation (Optional)

Some systems need a single GUID to represent any TypeIdentifier — including composite types that have no explicit GUID registration. This appendix defines a deterministic derivation.

### 5.1 Derivation Rule

```
DerivedGuid = UUIDv5(NamespaceGuid, CanonicalTypeIdentifierString)
```

- **NamespaceGuid**: `3b95d159-4c51-41e6-975c-51de228f0c06` — a fixed UUID published with this standard
- **Input string**: the canonical TypeIdentifier string representation (Section 1.5)
- **Hash algorithm**: SHA-1 per RFC 4122 Section 4.3 (UUIDv5)

### 5.2 Properties

- **Deterministic**: the same TypeIdentifier string always produces the same GUID, across implementations
- **Cross-implementation**: any implementation that follows this rule produces the same GUID for the same input
- **Not reversible**: you cannot recover the TypeIdentifier string from the GUID

### 5.3 Resolution Requirement

Because the derivation is not reversible, an implementation using derived GUIDs MUST maintain a resolution mechanism to map them back to TypeIdentifier strings. Two approaches:

1. **Assembly scanning** — scan assemblies for constructed types (closed generics, arrays) that appear in type hierarchies, property and field declarations, and method return values. Derive their GUIDs and build a lookup table in memory.
2. **Persisted mapping table** — store `DerivedGuid → TypeIdentifierString` pairs in persistent storage

The standard does not prescribe which approach to use. This is an implementation concern.

### 5.4 When to Use

UUIDv5 derivation is useful when:
- SQL storage requires a GUID column for type identity (indexing, filtering)
- Wire protocols benefit from fixed-size type identifiers
- Existing systems already use GUID-based type identification and need a migration path

It is NOT required for systems that can use the full TypeIdentifier string directly (JSON serialization, string-based wire protocols, document databases with string keys).

---

## Appendix A: Grammar

Informal grammar for the TypeIdentifier string format (ECMA-335 `AssemblyQualifiedName` subset):

```
TypeIdentifier     ::= Component
Component          ::= LeafComponent | GenericComponent | ArrayComponent
LeafComponent      ::= MappedComponent | NamedComponent
MappedComponent    ::= GUID ", 0"
NamedComponent     ::= TypeName ", " AssemblyName

GenericComponent   ::= GenericDef "[[" ArgList "]], " Assembly
GenericDef         ::= TypeName "`" Arity | GUID
ArgList            ::= Component | Component "],[" ArgList
ArrayComponent     ::= (TypeName | GUID) "[], " Assembly

GUID               ::= <RFC 4122 lowercase hex with dashes>
TypeName           ::= <CLR Type.FullName without assembly qualification>
AssemblyName       ::= <Short assembly name only, no version/culture/key qualifiers>
Arity              ::= <Positive integer>
Assembly           ::= "0" | AssemblyName

Note: On read, implementations MUST accept full assembly-qualified names
(with Version, Culture, PublicKeyToken) and normalize to short names
before processing. The grammar above describes the canonical form.
```

## Appendix B: Reference Implementation

The reference implementation is the `Compze.TypeIdentifier` NuGet package (.NET). Source code: [TODO: repository URL].

## Appendix C: Comparison With Existing Approaches

| System | Type Identity | Rename-safe | Generics | Migration |
|---|---|---|---|---|
| `AssemblyQualifiedName` | Full CLR string | No | Structural (fragile) | N/A |
| Newtonsoft `$type` | `TypeName, Assembly` | No | Structural (fragile) | N/A |
| NServiceBus | Custom string mapping | Yes | Manual per type | No |
| Marten | `TypeName` or custom | Partial | No | No |
| EventStoreDB | Stream/event type string | Manual | Manual | No |
| **TypeIdentifier** | Structural + GUID hybrid | Yes | Structural (robust) | Yes |
