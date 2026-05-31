# TypeId Remediation Plan (Phase 3)

Locked after the deep review. Scope: `Compze.TypeIdentifiers` + the draft standard, **plus** the hosting
migration required to delete the registration bridge (R6, scope-expanded by decision).

## Locked decisions
| # | Decision | Consequence |
| --- | --- | --- |
| Identity | **Cross-version-stable** — implement normalization-on-read | Strip Version/Culture/PKT; canonical = short name; rewrite the specs that pinned the versioned AQN |
| Persisted key | **Deferred** — decide after normalization | No UUIDv5 experiment now. Normalization already shrinks stable keys, easing the index-size pressure |
| Standard | **Prune the doc, don't chase its MUSTs in code** (deferred) | Normalization brings code into line with §1.5/§1.6; UUIDv5 + legacy-dictionary stay out; doc surgery is a later task |
| Registration | **Explicit per-endpoint; delete the bridge** | Full hosting migration (R6) is in scope this round |
| Stable detection | **Lookup-time PKT** + `adb9793829ddae60` + by-token escape hatch | Closes both C/High findings |
| Multi-ALC | **Not a target** | Document the single-load-context constraint on `TypeId` |

## Guiding principle
Code simplicity and readability trump microoptimizations virtually every time. The parser is never hot
(every unique string is parsed once then cached), so performance is **not** a factor in the parser rework —
readability decides.

## Stages

### 3a — Parser rework bake-off  *(running)*
The one real design fork: evolve-the-regex (A) vs. hand-written-tokenizer (B). Both implement the **frozen
contract** (`frozen-parser-contract.patch`: normalization + `0`-authoritative classification + the
self-corruption regression; 21 red specs against current code) in isolated worktrees to green, then a
3-judge panel scores **readability only**. Winner becomes the base for 3b.

Frozen contract covers R1 (normalization, High/D) + R2 (`0`-authoritative classification, High/A).

### 3b — Contained fixes on the winning base
- **R3** (High/C ×2) — stability from the live `Type.Assembly` PKT at lookup; add `adb9793829ddae60`; add
  `UseStableNameStrategyForPublicKeyToken(...)`. +specs.
- **R4** (Med/B) — failure-atomic registration: stage → validate → single `State` swap → add to
  `_processedAssemblies` last. Subsumes the HashSet race. +atomicity spec (Med/F).
- **R5** (Med/F) — public `TypeId` identity specs (invariant #2 over `ITypeMap`).
- **R6** (Med/C, **scope-expanded**) — delete the bridge `MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute`.
  - Consumers: `Compze.Hosting/ServerEndpointBuilder.cs:88` (production) and
    `Compze.Tessaging.Hosting.Testing/Wiring/TypeIdentifierMapperTestRegistrar.cs:18` (test wiring).
  - **Open design question:** the test wiring uses the bridge so test endpoints don't each enumerate
    framework mapping assemblies. Deleting it needs a deliberate replacement (an explicit framework-assembly
    registration helper, or each test endpoint declaring its assemblies). Resolve before implementing.
  - Production endpoints already register explicitly (AccountManagement pattern); framework pieces register
    via their `RegisterX()` builder extensions. The full-suite gate (3c) will surface anything that silently
    relied on auto-discovery.
- **R7** (Low/F) — fix the design doc to match the shipped API; document the by-token escape hatch.

### 3c — Integration + full-suite gate
Combine winner + 3b, run the **full** monolith suite (`C-Test`), iterate to green, then an adversarial
verification pass (each fix actually closes its finding) and update findings dispositions to "resolved".

## Deferred to a later round
UUIDv5 / persisted-key strategy · pruning the standard doc to match the code · parser tokenizer-only arity
polish (if A wins) · format-completeness doc · outer-`IdCache` concurrency spec · `TypeId`
single-load-context doc note · persistence 4-class duplication.
