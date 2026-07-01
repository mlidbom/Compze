---
name: refactor-to-code-standards
description: >-
  Split a hard-to-read class into a shape that truly has a single responsibility and delegates everything
  that is not that responsibility to collaborators — creating or extending those collaborators along the
  way — until it is conceptually coherent per the project's code standards. Use when asked to refactor,
  restructure, clean up, or "make readable / maintainable" a class or file that mixes several concepts or
  abstraction levels (a god-class, a static grab-bag, a method that interleaves policy with mechanism with
  interop), or when told "this is hard to follow, split it properly". The skill carries worked before→after
  examples (full unified diffs + walkthroughs) so the method is learned from real refactorings, not just
  described. Not for: fixing a bug, adding a feature, a one-line rename you can just make, or a pure
  formatting pass.
---

# Refactor to code standards

Take a class that is hard for a human to hold in their head and split it so it keeps **one** responsibility
and hands every other concept to a collaborator — extending an existing type or creating a new one where none
fits. The result is not "fewer lines" or "more files"; it is a class every line of which reads at **one**
level of abstraction, understandable from names alone.

## Why this exists

The failure mode this fixes is the class that passes *your* comprehension gauge and fails the human's. A
model holds thousands of facts in working memory, so a 245-line class that interleaves caching policy, COM
interop, a threading idiom and dictionary mechanics reads fine to it. A human has **5–7 short-term-memory
slots** (`005`); to understand *one* line of that class they must load *all* of its concepts at once, and it
overflows. This skill is the discipline of evicting every guest concept until the reader needs to hold only
one. Trust the standards' gauge, not your own ability to read the tangle.

## The method

Work in this order. The order is load-bearing: **rehome concepts first, split wiring last** (`014`). Attack
the mechanics first and you stall, because the shared concepts have no truthful home to move *into*.

1. **Name the responsibilities out loud.** List every distinct concept and abstraction level the class
   currently holds. The concept the class is *named for* is the one it keeps; every other is a **guest to
   evict**. If you cannot name them cleanly, that confusion IS the finding — say so before cutting.

2. **For each guest, find or create its true home** — do this *before* rewiring anything:
   - Belongs on data you already pass around? → make it a method **on that type** (an extension member if the
     type is external/interop, e.g. a native handle). Behaviour goes with data (`030`).
   - A self-contained policy/mechanism with its own state? → **extract a new named class** for it.
   - A reusable language idiom (a threading incantation, a collection bulk-op)? → a small, well-named
     extension in its proper home.
   - Name each home and each operation for **the human who must look it up** (`013`) — long and truthful over
     short and clever.

3. **Move the knowledge, not just a call to it.** The collaborator must *own* the concept end-to-end — the
   magic constants, HRESULTs, sentinels, class atoms, the incantation all travel *with* the mechanism. Half a
   concept left behind is worse than none.

4. **Rewrite the original as orchestration.** Its body should now read as a sequence of named calls at one
   consistent level. **Any line still at a lower level** — a raw loop, an inlined incantation, a magic
   literal — **is a guest you missed. Return to step 2.** Recurse all the way down.

5. **Audit behaviour-equivalence deliberately.** List each behaviour of the before; confirm the after
   preserves it; **name any change you make on purpose** (a moved `catch` scope, a dropped short-circuit, a
   deleted dead path). A green build is necessary, not sufficient — say what you verified and how.

6. **Apply the reader test.** Re-read the original as if you do not know the domain: understandable from names
   alone, ≤ 5 chunks held at once? If not, keep going.

Work in increments that each build clean; commit each with a message recording the *why*. Renames ripple —
let the IDE/refactoring engine propagate them rather than hand-editing call sites.

## What this skill MUST get right

- **Extract for clarity, not for reuse.** The moment a concept reads clearer in its own home, move it —
  "used once" is no reason to inline (`040`, `010`). Do not wait for a second caller.
- **A truthful rename is a real fix, not cosmetics.** A name that lies (a `Kind.Application` that is only a
  *candidate*) miscalibrates every reader; correcting it is often the highest-leverage change in the whole
  refactor (`012`). Lead with it.
- **Never invent a "utils"/"helpers"/"manager" bag.** Those are the absence of a concept. Every new home is a
  real abstraction with a truthful name; if you cannot name it, you have not found the concept yet.
- **"Confined to one place" beats "technically shared".** Grouping code because it shares a *technical* trait
  (all the DLL imports, all the COM calls) is not cohesion (`031`). Group by responsibility.
- **Behaviour changes get named, never buried.** If an extraction shifts behaviour — even an edge case, even
  an improvement — surface it explicitly. Silent behaviour drift under "just refactoring" is the cardinal sin.
- **Don't degrade to hit the shape.** If following the standards you cannot reach a design that reads well,
  stop and ask (`020`) — do not force an awkward split.

## Learn from the worked examples

Before refactoring a target, **read the example whose shape matches it** — the full unified diff *and* its
walkthrough. Each is a real before→after in this catalog (diffs are checked in, so they resolve in every
project the catalog reaches — no repo-specific commit hashes to go stale).

| Example | Teaches | Read when the target is… |
|---|---|---|
| [affinity-cache-god-class-split](examples/affinity-cache-god-class-split.md) | A 245→78-line static cache split into orchestration + a policy class + interop moved onto the handle it acts on + idiom extensions; includes a truthful enum rename and a move-by-move behaviour audit. | a class mixing policy + mechanism + interop + collection plumbing; a static grab-bag; anything where "to read one line you must know everything". |

Read the walkthrough first (it names the *moves*), then the diff (it shows them). Match the *pattern of
moves* to your target; do not copy the Win32 specifics.

> One example teaches one shape. As more before→after refactorings accrue, add each as a new row here plus a
> `examples/<name>.diff` + `examples/<name>.md` pair — spanning different shapes (conditional→polymorphism,
> primitive→value-object, procedural→OO) makes the method generalise instead of over-fitting to extraction.

## Files

- `examples/affinity-cache-god-class-split.diff` — full unified diff of the reference refactoring (all
  touched files, so nothing is hidden).
- `examples/affinity-cache-god-class-split.md` — the walkthrough: the before-diagnosis, each rehoming move,
  the residue, the behaviour audit, and which files are the lesson vs. mechanical churn.
