# Renaming is the most important refactoring there is — a truer name is a fix to make now, never cosmetic.

A name is the interface to a concept: it is read at every callsite, far more often than the body, and it sets
the reader's mental model before they read a line of implementation. A name that lies or blurs therefore
miscalibrates every reader, everywhere, continuously — the highest-leverage defect in a codebase — and the
rename that fixes it is the highest-leverage change. Often the rename *is* the whole fix, not a tidy-up
afterward: when a name claims a concept the type is not, correcting the name is the design fix itself.

- The moment a truer name occurs to you, rename — do not defer it as polish or "later".
- In review and design, lead with the naming/renaming issue; a rename proposal is a substantive fix, not bikeshedding.
- Never keep a poor name because it is "everywhere", "just a name", or "works fine" — that ubiquity is the cost, since every use inherits the wrong model.

Pairs with [conceptual coherence](010-conceptual-coherence-and-naming.md) and
[naming-and-conceptual-fit-are-preeminent](011-naming-and-conceptual-fit-are-preeminent.md).
