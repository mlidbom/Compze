# DDD: Ubiquitous language — the same word for a concept everywhere a human reads it

Every concept in the system has exactly one name, and that name appears **identically in every artifact a
human reads**: type and member names, comments, commit messages, documentation, test and specification names,
user-facing text (labels, dialogs, errors), and even throwaway lab and research code. This is Eric Evans'
**ubiquitous language**, applied without exception. There is no artifact where a second word for the same
concept is acceptable — "it's only a comment", "it's only a test", "it's only a lab", "it's only a UI string"
are never reasons to leave the language fractured.

## Why this is not cosmetic

A name is the interface to a concept (see
[renaming](012-renaming-is-the-most-important-refactoring.md)). The reader forms their mental model from
whatever word they meet first — and they meet words in comments, test names, and UI labels just as often as in
type declarations. When the same concept is called `preview` in the type and `tile` in the comment beside it,
the reader must stop and translate: *are these the same thing?* That translation tax is paid on every read, by
every reader, forever. A comment, a test name, or a label that uses a different word than the type is therefore
a defect of the **same kind and severity** as a lying type name — not a lesser, tidy-it-up-later issue. Mixed
vocabulary is how a codebase quietly accumulates friction, misunderstanding, and bugs born of two people
meaning different things by the same word.

## How to apply it

- **A terminology change is a whole-language change.** When a concept is renamed, the rename is not done until
  *every* artifact speaks the new word — production code, comments, specs/tests (names, folders, locals), docs,
  user-facing strings, and lab code. Sweeping the type names and leaving the comments is leaving the job
  half-done.
- **Audit user-facing text too.** The words on the screen are part of the ubiquitous language. Settings labels,
  dialog copy, and error messages must use the same term the code does, so the team, the user, and the code all
  speak one language.
- **Two words for one concept is a modeling question, not a styling one.** When you find a concept wearing two
  names, decide it structurally: if they are the same concept, unify on one word everywhere; if they are
  genuinely distinct concepts, the second one earns its *own* ubiquitous word and keeps it consistently. Never
  resolve it by taste or by "which reads nicer here".
- **A distinct word must mean a distinct concept.** Reserve different words for genuinely different things. A
  `chord` (the key combination) and a `hotkey` (the bound, globally-caught action) stay distinct because they
  *are* distinct (see [name-for-the-human](013-name-for-the-human-who-must-look-it-up.md)); `tile` and
  `preview` for the same grid item are not, and must collapse to one.

Why it matters at all is [the human mind](005-software-design-and-the-human-mind.md): a ubiquitous language is
one of the few things a 5-7-slot working memory needs to hold a system of any size in its head.

Pairs with [conceptual coherence](010-conceptual-coherence-and-naming.md),
[naming-and-conceptual-fit-are-preeminent](011-naming-and-conceptual-fit-are-preeminent.md),
[renaming-is-the-most-important-refactoring](012-renaming-is-the-most-important-refactoring.md), and the naming
how-to in [032-naming](032-naming.md).
