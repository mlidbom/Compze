# TypeId Deep Review — Frozen Contract, Rubric & Scope

> **This is the reference every reviewer judges against.** Frozen at the start of the review.
> Scope: **`Compze.TypeIdentifiers` library + the draft standard.** The SQL interner / DocumentDb /
> Typermedia ripple is **out of scope** except where a flaw there reveals a *design* flaw in the library
> or standard. Reviewers critique and gather evidence; they do **not** refactor in Phases 1–2.

## 1. Intended contract (what the design is *trying* to be)

**Purpose.** Rename-safe, structural type identity for .NET persistence / serialization / routing. A
persisted `$type` string must keep resolving to the right `Type` even after the type is renamed or moved
between assemblies.

**Two tiers.**
- **Stable assemblies** — names won't change (runtime assemblies by public-key-token, or user-declared).
  Their `TypeName, Assembly` pairs pass through untouched.
- **Mapped assemblies** — rename-safety needed. Leaf types and open generic *definitions* get GUIDs.

**Public surface.**
- `TypeId` — canonical identity = (resolved `Type`, canonical string). Obtainable **only** via `ITypeMap`.
  Equality is **reference equality on `Type`**.
- `ITypeMap` — `GetId(Type)`, `ToPersistedTypeString`, `FromPersistedTypeString`,
  `GetIdFromPersistedString`, `AssertMappingsExistFor`.
- `ITypeMapper` — `MapTypesFromAssemblyContaining<T>`, `MapTypesFromAssembly`,
  `UseStableNameStrategyForAssemblyContaining<T>`.

**Format (standard draft).** ECMA-335 `AssemblyQualifiedName` structure. Mapped component = `GUID, 0`
(assembly literal `0`, reserved field). Stable component = `TypeName, AssemblyName`. Composition via
`[[ ]]`. Canonical form: lowercase RFC-4122 GUIDs, short assembly names, single space after comma.
Normalization on read strips Version/Culture/PublicKeyToken.

**Resolution.** Parse ECMA-335 structure → normalize leaves → resolve each leaf (mapped via GUID dict;
named via legacy dict or runtime) → recompose via `MakeGenericType` / `MakeArrayType`.

**Concurrency.** `TypeNameMapper` holds an immutable `State` snapshot in one `volatile` field;
registration copies-on-write and atomically swaps; a write lock serializes writers; reads are lock-free;
each snapshot owns its caches. `TypeMapper` layers its own `Caches`, swapped wholesale on `ClearCaches()`.

**Invariants (must hold).**
1. A type maps to ≤1 GUID; a GUID maps to ≤1 type. (Enforced in `AssertTypeAndGuidAreUnmapped`.)
2. `TypeId` is canonical per type: two instances for the same type are equal and share `CanonicalString`.
3. Round-trip identity: `Type → canonical string → Type` returns the same `Type` for every supported shape.
4. Rename safety: a mapped component persists as its GUID regardless of nesting depth (generics, arrays,
   jagged arrays, nested generics).
5. A mapping class may only map types from its own assembly.

## 2. Prior-review waterline (verify, don't re-derive)

| Prior finding | Status | Deep-review obligation |
|---|---|---|
| #1 Jagged arrays lose rename-safety | Claimed FIXED | Re-verify via adversarial parser matrix |
| #2 Cache lost-update race | Re-architected (snapshots) | **Prove** closed at *both* cache layers; scrutinize the `IThreadGate` spec |
| #3 `GetIdsForTypesAssignableTo` omits subtypes | Removed (feature deleted) | Confirm nothing downstream needs it |
| SQL interner + datastore | Claimed correct & verified | Out of scope unless it exposes a library/standard design flaw |
| Persistence duplication (4 near-identical classes) | Open, low priority | In scope only as a design-smell note |

## 3. The rubric — definition of "good for the long term"

Merge requires **every category = Pass**, **every High/Critical finding resolved or consciously accepted
with rationale**, and the **devil's-advocate verdict = "use it"** (not "reject").

- **A. Parser correctness & round-trip fidelity** — full grammar + adversarial inputs (jagged/nested
  arrays, multi-arg & nested generics, Unicode identifiers, full-AQN normalization, malformed input →
  clear errors); no silent wrong-resolution path. *Open question: regex vs. a real tokenizer.*
- **B. Concurrency** — lost-update race provably closed at both cache layers; `volatile` snapshot-swap
  memory-model correctness; registration-during-lookup safe or documented-unsupported-with-a-guard;
  `_processedAssemblies` HashSet access justified.
- **C. Public API & identity model** — minimal, hard-to-misuse surface; `TypeId`'s live-`Type` /
  reference-equality / load-context semantics correct, documented, tradeoffs understood; the "temporary
  bridge" auto-discovery method has a decided fate.
- **D. Standard / format soundness** — unambiguous, versionable (reserved `0` field), complete for the
  shapes .NET produces; normalization tradeoffs deliberate; UUIDv5 appendix sound. **Decision: is
  "publish as a standard" justified now, or descope to "internal format"?**
- **E. Long-term evolvability** — clean separation format / parser / mapper / persistence; the two-tier
  cut is right; the devil's-advocate case for *not* shipping has been heard and rebutted-or-accepted.
- **F. Tests & docs** — specs read as behavior and cover the adversarial matrix; each invariant has a
  guarding spec; design docs reflect current state, no historical baggage.

## 3a. Guiding principle (overrides micro-tradeoffs)

**Code simplicity and readability trump microoptimizations virtually every time.** A more complex
"faster" approach is only justified when it is *both* clearly more readable *and* produces a real runtime
difference. For the parser specifically, performance is moot: every unique string is parsed once and then
served from a cache, so the parser is never hot. Readability decides.

## 4. Baseline

- Branch: `simplify_typeid_structure`
- Baseline test status: **GREEN** — `Compze.TypeIdentifiers.Specifications` 169/169 passed, clean build.
- Pre-existing analyzer warnings (not findings): `CA1050` (SystemCE, unrelated), `CA1019`
  (`AssemblyTypeMapperAttribute.Mapper` positional-arg accessor).
