# Re-home concepts before restructuring — truthful names and homes FIRST, wiring second

When untangling entangled architecture, the order of work decides whether it is possible at all. Attack the
wiring/mechanics first and you stall: the shared concepts have no truthful home to be split *into*, so every cut
re-creates the coupling somewhere else. Invert the order:

1. **Diagnose where names lie and concepts lack homes.** Scattered "shared" types in one party's namespace,
   asymmetric APIs (one party first-class, the other bolted on via casts), `InternalsVisibleTo` back-doors —
   these are the actual knot, not the wiring.
2. **Give shared concepts their truthful names and homes first**, even though it looks like "just renaming".
   Make asymmetric APIs symmetric. This step is small, mechanical, and verifiable.
3. **Then split the wiring.** After step 2 it usually stops being a design problem and becomes mechanics: the
   seam is the shape the code wants once the names stop lying.

This is conceptual-coherence-first (010–013) applied as an operational *sequence*, not just a review lens.

## When given full delegation ("use your own judgment, keep going until it is actually good")

Take it literally: make the design calls; work in increments that each build clean and pass the full suite;
commit each increment with a commit message that records the *why*; record decisions and the as-built design in
the repo's docs as you go; and collect taste/naming questions into a follow-ups list to surface at the end
instead of stopping to ask. Optimize for a design a human can divide and conquer mentally — intuitive
object-oriented units — never merely for "works".

Pairs with [renaming-is-the-most-important-refactoring](012-renaming-is-the-most-important-refactoring.md).
Provenance and the full case study: see the .rationale.txt sidecar.
