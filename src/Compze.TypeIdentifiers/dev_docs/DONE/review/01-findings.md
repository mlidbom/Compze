# TypeId Deep Review — Verified Findings & Draft Verdict

Deep multi-agent review of `Compze.TypeIdentifiers` + the draft standard. 30 agents · 7 blind design
lenses + tail sweep · triage → adversarial verify → synthesis. **36 raw → 19 canonical → 17 survived
verification** (13 Confirmed, 4 PartiallyConfirmed, 2 Refuted). Three headline Highs were additionally
**reproduced by hand** (see §6).

## 1. Verdict

**Not a reject — a salvageable design with must-fix correctness and conformance defects.** The core
architecture is sound: the two-tier stable-vs-mapped cut is right, the format/parser/mapper/persistence
separation is clean, the snapshot copy-on-write concurrency redesign genuinely closes the prior
lost-update race, the jagged-array fix holds, and a 14-type real-CLR matrix round-trips 0 failures. The
two strongest "reject the design" cases (cross-ALC ownership rejection; interned-int-as-identity-hazard)
were **refuted** under scrutiny.

But it is **not ready as-is.** Two themes carry four confirmed High findings:
- **Theme 1 — the canonical string.** The implementation never emits the short-name canonical form it
  defines; it emits the full *versioned* AQN. So normalization-on-read is unimplemented, canonical-per-type
  is broken, and a .NET runtime upgrade splits interner identity for every framework-referencing type. That
  same unbounded Unicode string is then the UNIQUE-indexed persisted key, with three divergent per-engine
  ceilings and **silent truncation on SQL Server**.
- **Theme 2 — the zero-config stable tier.** The hardcoded Microsoft public-key-token set omits the
  dominant `adb9793829ddae60` (signs `Microsoft.Extensions.*`, `Microsoft.AspNetCore.*`), and stable
  detection is a one-shot construction-time scan — so common framework types throw on serialize,
  non-deterministically by load order.

## 2. Rubric scorecard

| Cat | Area | Rating | One-line |
| --- | --- | --- | --- |
| A | Parser correctness | **Concern** | Classification keys on the wrong field (self-corruption path); good CLR data is safe |
| B | Concurrency | **Concern** | Inner race closed; registration not failure-atomic; outer cache layer & `_processedAssemblies` unproven/unguarded |
| C | API & identity | **Fail** | Stable-tier zero-config promise not delivered for a large class of framework assemblies; temp bridge is the load-bearing prod path |
| D | Standard / format | **Fail** | Reference impl violates its own normative MUSTs; unbounded string as unique key |
| E | Evolvability | **Concern** | Architecture sound and worth keeping; identity-string & stable-detection must settle before the format freezes |
| F | Tests & docs | **Concern** | Headline invariants unguarded; two specs enshrine the conformance defect; doc advertises a non-existent API |

## 3. Findings by disposition

### Refactor-now (8)
| Sev | Cat | Finding | Anchor |
| --- | --- | --- | --- |
| High | D | Stable canonical string is the full **versioned** AQN; normalization-on-read unimplemented → breaks canonical-per-type & splits interner identity across runtime versions | `TypeNameMapper.cs:176-182`, `TypeIdentifier.cs:101-108` |
| High | A | Mapped/Named classification keys only on `Guid.TryParse(typeName)`; reserved `0` never validated → assembly silently discarded, 32-hex CLR type self-corrupts on its own round-trip | `TypeIdentifier.cs:76-82,95` |
| High | C | Microsoft PKT set omits `adb9793829ddae60` → `Microsoft.Extensions/AspNetCore/`out-of-band NuGet types throw on serialize | `TypeMapper.cs:22` |
| High | C | Stable detection is a one-shot construction-time scan; lazily-loaded stable assemblies are missed → load-order non-determinism | `TypeMapper.cs:114-121` |
| Med | B | `MapTypesFromAssembly` is not failure-atomic / retry-safe (marks processed before publish, no rollback) | `TypeMapper.cs:28-56` |
| Med | F | No spec guards registration failure-atomicity / retryability | — |
| Med | F | Public `TypeId` identity contract (reference equality + invariant #2) has **zero** guarding specs; all identity specs target internal types | `TypeId.cs:33-35` |
| Low | F | Design doc advertises a fluent / multi-type / by-token API that doesn't exist (`ForAssembliesContaining` vs `ForAssemblyContaining`); examples don't compile | `TypeIdentifier-Design.md` |

### Needs-experiment (1)
| Sev | Cat | Finding | Anchor |
| --- | --- | --- | --- |
| High | D | Unbounded Unicode canonical string is the UNIQUE-indexed persisted key; 3 per-engine ceilings, no fallback; **SQL Server silently truncates** over-length (`NVarChar Size=450`) | `typeids-table-key-sizing.md` |

### Reject-design (2)
| Sev | Cat | Finding | Decision |
| --- | --- | --- | --- |
| Med | D | Draft "standard" carries normative MUSTs its sole reference impl violates; cross-impl publication premature | Descope to **internal, versioned, may-change** format |
| Med | C | "Temporary bridge" `MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute` is the **sole production registration path** with no decided fate; AppDomain-wide scope undermines per-container design | Decide fate before merge |

### Defer (6)
| Sev | Cat | Finding |
| --- | --- | --- |
| Med | D | UUIDv5 appendix unimplemented/untested/not the key (precondition = normalization) |
| Low | B | `_processedAssemblies` unsynchronized `HashSet` (no concurrent-registration path today) |
| Low | A | Parser absorbs structurally invalid bracket content; no generic-arity validation → deferred error, not silent wrong resolution |
| Low | D | Format completeness: pointer / by-ref / nested / generic-parameter / rank-1 array shapes unspecified |
| Low | F | Outer `TypeMapper.IdCache` concurrency invariant has no guarding spec |
| Low | C | `TypeId` pins a live `Type` (reference equality, cached forever); single-load-context tradeoff lives only in a code comment |

## 4. Refuted (culled — credit to the design)
- **Cross-ALC assembly-ownership check wrongly rejects legit mappings** — Refuted.
- **Interned database-local int as type reference is a hazard** — Refuted: ordinary normalization; the
  UUIDv5 alternative isn't reversible so it would still need a mapping table.

## 5. Standard decision (recommended)
**Descope now; do not publish as a public cross-implementation standard.** The draft is branded
cross-impl (fixed namespace GUID, comparison table, normative MUSTs) yet its only reference implementation
violates those MUSTs, the legacy dictionary is absent, the reserved-`0` lever is non-functional, the
entire UUIDv5 appendix is unimplemented, and Appendix B's repo URL is literally `[TODO]`. Rename to
"Compze TypeIdentifier format — internal, versioned, subject to change." Cost of holding publication ≈ 0;
cost of publishing a standard the code violates is a future-compat trap.

## 6. Independent verification (reproduced by hand, not agent-asserted)
A throwaway spec (since deleted) asserted the claimed buggy behavior; all three passed against the built dll:
1. `new TypeMapper().ToPersistedTypeString(typeof(string))` contains `Version=` — normalization unimplemented.
2. `TypeIdentifier.Parse("e4a8c9f2-…-3e7c, MyRealAssembly").StringRepresentation == "e4a8c9f2-…-3e7c, 0"` — assembly discarded.
3. A 32-hex global type → `"deadbeef-…, 0"` via the library's own serialize path, which then throws on resolve.

## 7. Open questions for the human (gate Phase 3)
1. **Identity source of truth** — do you want cross-runtime-version-stable identity (survives .NET 10→11)?
   If yes, normalization MUST be implemented and the two pinning specs rewritten.
2. **Persisted-key strategy** — make bounded UUIDv5 the primary indexed key (string as non-indexed
   payload), or keep the variable-length canonical string as the unique key with an enforced max
   length / library-provided hash indexing?
3. **Standard publication** — confirm descope to internal versioned format.
4. **Registration model** — explicit per-endpoint `MapTypesFromAssemblyContaining<T>()` everywhere (delete
   the bridge), or promote auto-discovery to `ITypeMapper` with a name, spec, and load-order contract?
5. **Stable detection** — accept lookup-time `Type.Assembly` PKT check (fixes both C/High findings) + add
   the advertised `UseStableNameStrategyForPublicKeyToken(...)` escape hatch? *(Recommended: yes.)*
6. **Multi-ALC / collectible contexts** — a real target (plugin/hot-reload)? If no, a documented
   single-load-context constraint suffices. *(Recommended: no — document the constraint.)*
