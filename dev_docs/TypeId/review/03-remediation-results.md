# TypeId Deep Review — Remediation Results

All must-fix findings remediated and validated. Greenfield (no production / no persisted data), so changes
optimized for the best long-term design without migration hedging.

## Test status
- `Compze.TypeIdentifiers.Specifications`: **193/193** green (was 169; +24 new adversarial/contract specs).
- Full monolith suite (`C-Test`, all DB combos): **3652 passed**, 26 skipped, **0 failed** (clean run).
  - (Some earlier runs showed a handful of `MySql: Too many connections` failures — server-side
    connection-pool exhaustion under full parallel load. Environmental, not regressions: they pass in
    isolation and are MySql-only.)

## Follow-up: AppDomain scan fully eliminated
The bridge's AppDomain-wide auto-discovery is now gone from the **test** infrastructure too (it had been
relocated there in R6). The shared test setup (`SetupTestingContainer`, `CreateContainerForTesting`,
`TestClient.ConnectTo`) registers framework mappings only and takes an explicit domain-registration hook;
each test/sample registers its own domain via per-project helpers (`RegisterCommonTestTypeMappings`,
`…Integration…`, `…Performance…`, `…AccountManagement…`) — exactly as a production endpoint does. A test
that forgets to register a type now fails the same way a real application would, instead of being silently
rescued by a scan. ~24 call sites across 3 test projects + Newtonsoft.Specs + AccountManagement samples.

## What changed (production)
| File | Change |
| --- | --- |
| `TypeIdentifier.cs` | Parser rewritten regex-free: bracket-depth helpers + `Guid.TryParseExact("D")`; normalization-on-read; `0`-authoritative mapped/named classification (the bake-off "Option C") |
| `TypeNameMapper.cs` | Lookup-time PKT stability (`IsStableType`); atomic `AddAssemblyMappings` (single snapshot swap) |
| `TypeMapper.cs` | +`adb9793829ddae60`; seed tokens (no load-time scan); `UseStableNameStrategyForPublicKeyToken`; failure-atomic/retry-safe registration under a lock; **bridge deleted** |
| `ITypeMappingLookup.cs`, `StableLeaf/StableGenericTypeIdentifier.cs`, `ITypeMapper.cs` | `IsStableAssembly(string)` → `IsStableType(Type)`; by-token API |
| `CompzeFrameworkTypeMappings.cs` (new), `ServerEndpointBuilder.cs` | Curated `MapCompzeFrameworkTypes()` replaces the AppDomain scan in production |
| `TestingEndpointHost.cs`, `TypeIdentifierMapperTestRegistrar.cs` | AppDomain scan moved to **test-only** wiring (`MapAllLoadedAssembliesWithTypeMappings`) |
| `TypeIdentifier-Design.md`, `CLAUDE.md` | Doc API fixed to match shipped surface; greenfield callout |

New specs: parser normalization+classification contract (incl. 32-hex self-corruption regression), atomic
registration, by-PKT stable detection, public `TypeId` identity.

## Rubric re-score
| Cat | Before | After | Note |
| --- | --- | --- | --- |
| A Parser | Concern | **Pass** | regex-free parser; `0`-authoritative classification; adversarial contract specs; jagged-array fix held |
| B Concurrency | Concern | **Pass** | atomic failure-safe registration; `_processedAssemblies` lock-guarded; snapshot model. (outer `IdCache` concurrency spec — deferred Low) |
| C API/identity | **Fail** | **Pass** | lookup-time PKT closes both Highs; bridge deleted (fate decided); public identity specs. (single-load-context doc note — deferred Low) |
| D Standard/format | **Fail** | **Concern** | normalization implemented → code now matches §1.5/§1.6. **Deferred by decision:** persisted-key strategy (UUIDv5 vs bounded string) + standard-doc pruning |
| E Evolvability | Concern | **Pass** | cleaner parser + registration; two-tier cut intact |
| F Tests/docs | Concern | **Pass** | invariants now spec-guarded; doc fixed. (a couple of Low specs deferred) |

## Verdict
**Badge of approval for the library design.** The architecture is sound and the must-fix correctness +
conformance defects (both Fail categories) are resolved. The two strongest "reject" arguments were already
refuted in review; nothing in remediation changed that.

## Deferred (tracked future work — user decisions)
1. **Persisted-key strategy** (D, High) — decide post-normalization: UUIDv5 as the primary bounded indexed
   key vs. the canonical string with an enforced max length / hash index. (Normalization already shortened
   stable keys, easing the pressure.) Until then, do not ship the unbounded string as the unique key on
   engines that silently truncate (SQL Server `NVarChar(450)`).
2. **Standard doc** — prune to "internal, versioned, subject to change"; drop normative cross-impl MUSTs or
   gate them behind a v1-frozen milestone.
3. **Low-severity polish** — outer `IdCache` concurrency spec; `TypeId` single-load-context XML-doc note;
   format-completeness scope note (pointers/by-ref); parser arity-validation specs; persistence 4-class dedup.
