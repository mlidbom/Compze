---
name: team-review-and-fix-code-standard-issues
description: >-
  Multi-agent hunt for violations of the project's own coding standards. One agent distills the standards
  (and their rationale sidecars) into actionable, searchable checks; a fleet of hunters each sweep the whole
  codebase for one check and propose fixes; a judge dedups and ranks; independent skeptics (validity /
  improvement / risk) try to kill each candidate before it survives. Produces a vetted refactor worklist;
  on approval, applies in build-gated waves with per-wave commits. Use when asked to audit, review, or
  refactor for code-style / cohesion / rich-types / standards compliance, or to "run the style review". Not
  for: bug hunting (use /code-review), one-off single-file edits, or reviewing a PR diff.
---

# Code-standards violation hunt

A structured, multi-agent pass that searches the codebase for violations of the project's own coding-standard
rules and converges on a vetted set of fixes. It exists because a single read misses things and a single
reviewer rubber-stamps: it turns the rules into concrete checks, fans the search out so each agent hunts one
violation shape hard, then makes independent skeptics try to *kill* each proposal before it survives.

## The shape (and why it is this shape)

**The organizing axis is the RULES, not the folders.** The codebase is the search space; each check is a
query against it. Fan out where it is safe and additive — read-only search and adversarial critique. The one
write phase (Apply) is serialized and build-gated. "Agreement" is a deterministic **judge + adversarial
vet**, never agents negotiating rival rewrites.

Phases (all read-only until Apply):

1. **Distill** — one agent turns the standards into a flat list of actionable, independently-searchable
   checks, splitting compound rules into one-violation-shape-each checks (e.g. "avoid primitives" becomes
   separate checks for "≥2 primitive params" and "a raw id/handle where a domain type belongs"). The rule
   text is already in every agent's context (auto-loaded); the agent reads the `.rationale.txt` sidecars
   (which are *not* auto-loaded) for the load-bearing "why".
2. **Hunt** — one agent per check, in parallel. Each *searches* the whole codebase for its one violation
   shape (grep/scan tuned to that rule, then reads candidates to confirm in context) and proposes a concrete
   fix for every hit. Search-driven, not read-everything.
3. **Synthesize** — a judge dedups (same line flagged by several checks → one candidate naming all rules it
   breaks), resolves rule-vs-rule conflicts, ranks, and drops low-signal noise.
4. **Vet** — for each candidate, independent skeptics apply distinct lenses; only candidates that survive
   validity + improvement at the confidence gate are confirmed.
5. **Apply** (only after you approve the worklist) — build-gated waves, cross-cutting clusters kept atomic,
   per-wave commit; final independent review of the applied diff. Never push.

Run phases 1–4 first and present the worklist. That is the human checkpoint — approve, *then* apply.

## What this skill MUST get right

These are not hypothetical — each one let a real violation slip past, or wasted a run. Bake the
countermeasures in:

- **Trust the loaded rules; don't re-teach them.** Every agent already has the rule bodies in context, so the
  prompts must not waste effort restating them — the model's job is judgment, not being lectured the rules it
  can already see. The only things that genuinely must be read from disk are the `.rationale.txt` sidecars
  (not auto-loaded) and the source being audited.
- **Distill is where compound rules become catchable.** A vague "be cohesive" check finds nothing; the
  Distill agent must emit concrete, searchable violation *signatures*. If a rule yields no actionable check,
  that is a problem with the distillation, not the rule.
- **Convention is not a validity defense.** Skeptics must reject "it's the conventional purpose of such a
  file", "a framework/analyzer endorses it" (e.g. CA1060 blessing one `NativeMethods` bag), and "it's by
  design". The test is responsibility-cohesion, not idiom. (See `010-everything-in-its-place.rationale.txt`
  and `030-anti-corruption-layers.rationale.txt`.)
- **Validity gates surfacing; risk gates application.** Validity + improvement decide whether a finding is
  real and worth doing (→ `confirmed`); the risk lens decides only whether the *proposed fix* is safe to
  apply as-written (`readyToApply`) or needs design first (`needsDesign`, carrying the blocker). Never let a
  risk objection about a large or under-specified remediation bucket a validity-confirmed finding as
  *rejected* — that is how a sound finding gets silently buried under "the fix looks risky."
- **Many instances of one rule are many candidates, not noise.** A primitive threaded through 12 signatures
  is 12 real candidates (or one clustered fix), not something to dedup away because each looks individually
  small.
- **Don't auto-apply.** Produce the vetted plan; let the owner green-light. Apply in waves, each building
  green (`dotnet build` + tests) before the next.

## How to run

1. **Run the workflow.** Invoke `Workflow({ scriptPath: ".claude/skills/team-review-and-fix-code-standard-issues/review-workflow.js",
   args: { target, exclude, checksOverride, gate, includeJudgmentCalls } })` — every arg is optional:
   - **The rules are not hardcoded anywhere.** Every agent already has the project standards in context; the
     Distill agent finds the rule files from the paths shown to it and reads their rationale sidecars. Nothing
     about where the rules live is baked into the skill — it keeps working after the rules move, split, or
     migrate into CLAUDE.md, and after the reorg it recommends.
   - `target`: source root to search (default: each agent finds the first-party root itself).
   - `exclude`: extra paths to skip (legacy/vendored/build/generated are skipped by default).
   - `checksOverride`: pin the actionable-check list instead of distilling it — `[{ id, ruleId, ruleTitle,
     severity, signature, huntStrategy, fixPattern }]`. Escape hatch only; omit to let Distill do it.
   - `gate`: confidence threshold for *confirmed* (default `0.75`).
   - `includeJudgmentCalls`: `true` to also surface borderline findings (below the gate) for the owner to
     adjudicate.
   The workflow returns `{ confirmed, readyToApply, needsDesign, borderline, rejected, conflicts,
   droppedLowConfidence, judgeNotes, countsByRule, rawFindingCount, candidateCount, checkCount, gate }`.
2. **Present the worklist** grouped into build-gated waves (cluster fixes that share one new API). Note
   anything dropped/borderline for transparency.
3. **On approval, apply** wave by wave: edit → `dotnet build` + tests → commit per wave; cross-cutting
   clusters land atomically. Finish with one independent reviewer agent over the cumulative diff (behaviour
   preservation + no new violations), then a clean full build + tests.

## Apply-wave protocol

- Establish a green baseline (build + tests) before touching anything.
- Order waves easy→hard: isolated/no-logic changes first, then cross-cutting clusters.
- A cross-cutting cluster (e.g. a new domain type that several call sites adopt) is **one** wave so the tree
  stays buildable; do not split it across commits.
- After each wave: build + run tests; commit with a message naming the candidate IDs and the rule each serves.
- Paths/timing-sensitive code with no unit coverage (hooks, native input): flag that a manual/live smoke-test
  is still needed — a green build is necessary, not sufficient.
- Commit whenever appropriate. **Never push.**

## Tuning to the ask

"find any style issues" → let Distill emit the obvious checks, single-vote vet. "thorough audit" / "be
comprehensive" → `includeJudgmentCalls: true`, lower the gate; the fan-out already scales with however many
checks Distill produces.
