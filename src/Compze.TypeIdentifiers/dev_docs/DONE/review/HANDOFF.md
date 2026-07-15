# TypeId Review — Session Handoff (read this first)

Continues the deep design review + remediation of `Compze.TypeIdentifiers` and its draft standard.

## Status: COMPLETE & committed; suite green
- Branch `simplify_typeid_structure`, **working tree clean** (all work is committed).
- Two commits hold everything: `173a92940` (R1–R7 remediation) and `f1b1dbe12` (AppDomain scan elimination).
- Full monolith suite (`C-Test`, all DB combos): **3652 passed, 0 failed**. TypeIdentifiers specs: **193/193**.
- Branch history *below* those two commits is messy (merges / "cruft" / "checkpoint") → **squash before merging to main**.

## Verdict
**Badge of approval for the library design.** The architecture was sound; the must-fix correctness +
standard-conformance defects are fixed and spec-guarded. The two strongest "reject the design" arguments
(cross-ALC ownership, interned-int-as-identity-hazard) were *refuted* in review.

## What was done — DO NOT REDO. Read these in order:
- **`03-remediation-results.md`** — best single status doc: what shipped, rubric re-score (C & D went Fail→Pass/Concern), test status, the deferred list.
- `01-findings.md` — the 17 verified findings + the original A–F rubric scorecard.
- `00-contract-and-rubric.md` — frozen intended-contract + acceptance rubric + the greenfield note.
- `02-remediation-plan.md` — the locked remediation plan.
- `bakeoff/` + `frozen-parser-contract.patch` — parser bake-off provenance. The patch is already applied/committed; the `A__`/`B__` files are just records (deletable).

### Remediation shipped (R1–R7 + scan removal)
- R1/R2 — normalization-on-read + `0`-authoritative mapped/named classification → the **regex-free parser** ("Option C").
- R3 — lookup-time public-key-token stable detection (+ `adb9793829ddae60`, + `UseStableNameStrategyForPublicKeyToken`).
- R4 — failure-atomic, retry-safe assembly registration (single snapshot swap; `_processedAssemblies` lock-guarded).
- R5 — public `TypeId` canonical-per-type identity specs.
- R6 + follow-up — deleted the AppDomain auto-discovery bridge; production uses curated `MapCompzeFrameworkTypes()`; **the AppDomain scan is gone everywhere** (tests register their domain explicitly via per-project helpers).
- R7 — design-doc API corrected.

## Deferred work — your decisions; none is an open defect (details in `03-remediation-results.md` §Deferred)
1. **Persisted-key strategy** (the only remaining High). The canonical string is the unique-indexed key in the per-DB `TypeIds` table; it's unbounded Unicode and **SQL Server silently truncates at `NVarChar(450)`**. Decide: UUIDv5 as the primary bounded key (string as non-indexed payload) **vs** keep the string with an enforced max length / hash index. Deferred until after normalization (now done — normalization already shortened stable keys). **Until decided, don't ship the unbounded string as the unique key on SQL Server.** This is the natural next design decision.
2. **Standard-doc pruning** — cheap, doc-only: rename the draft to "internal, versioned, subject to change"; drop/v1-gate the normative cross-impl MUSTs (UUIDv5 appendix, legacy dictionary, reserved field). Normalization already brought the code in line with §1.5/§1.6.
3. **Low-severity polish** — outer `IdCache` concurrency spec · `TypeId` single-load-context XML-doc note · parser arity/strictness specs · format-completeness scope note · persistence 4-class dedup.

## Key context & gotchas for continuing
- **Greenfield**: zero deployed apps, no persisted data, **no backward-compat**. Optimize for best long-term design (also at top of `CLAUDE.md`).
- **Simplicity > microoptimization** (saved as a memory): readable code wins unless the speedup is real at runtime. The parser is parse-once-then-cached, so its performance is moot — readability decides.
- **Parser is "Option C"**: regex-free, explicit bracket-depth helpers (`IndexOfTopLevelComma`, `SplitAtTopLevelCommas`, `TryPeelTrailingArraySuffix`) + `Guid.TryParseExact(x, "D")`. The user **rejected the hand-written-tokenizer alternative** (hidden mutable cursor → temporal coupling). Don't reintroduce it.
- **Test registration model**: shared test setup (`SetupTestingContainer`, `CreateContainerForTesting`, `TestClient.ConnectTo`) registers framework mappings only and takes `Action<ITypeMapper> registerDomainTypeMappings` as its **first** parameter; per-project helpers supply the domain (`RegisterCommonTestTypeMappings`, `RegisterIntegrationTestTypeMappings`, `RegisterPerformanceTestTypeMappings`, `RegisterAccountManagementTypeMappings`). ⚠️ **Footgun:** that first param is a *required delegate* — an old-style `SetupTestingContainer(null)` / `(_ => {})` compiles but NREs/misbehaves. (None remain; this bit us once and the full suite caught it.)
- **MySql `Too many connections`** in full-suite runs = environmental flakiness under parallel load, **not** a regression (passes in isolation; MySql-only; type-identity bugs fail every combo with a mapping error).

## How to build / test
- Full suite: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking; C-Test` (builds + runs all combos; perf tests run stress-only by default).
- Focused (fast): `dotnet test test/Compze.TypeIdentifiers.Specifications/Compze.TypeIdentifiers.Specifications.csproj` (193 specs, ~sub-second).
- Avoid `C-Test -FullGitReset` (cleans the working tree).

## Suggested next steps
1. **Persisted-key strategy** (deferred #1) — the last real design decision.
2. **Squash the branch + open a PR to main** (history cleanup before merge).
3. **Standard-doc pruning** (deferred #2) — cheap win.
4. **Low-severity polish** (deferred #3).
