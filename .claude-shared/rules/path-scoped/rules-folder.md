---
paths:
  - ".claude/rules/**/*.md"
  - ".claude/rules/**/*.txt"
  - ".claude-shared/rules/**/*.md"
  - ".claude-shared/rules/**/*.txt"
---

# How the rules folder works

This folder replaces CLAUDE.md. It's read by both humans and the agent; this file is the map. It's
*path-scoped* (note the `paths:` frontmatter above), so it loads only when you're actually touching a file
under `.claude/rules/` — i.e. when editing the rules themselves — and costs nothing in a normal session.

## What loads, and when

- The native loader recursively discovers **`*.md` files only** under `.claude/rules/`. Non-`.md` files
  (`.txt`, `.gitkeep`) are never injected. Verified empirically: a `name.rationale.txt` sidecar does not load.
- A `.md` with **no `paths:` frontmatter** is always-on — loaded every session, same priority as CLAUDE.md.
- A `.md` **with `paths:` frontmatter** is path-scoped — excluded from session start, injected only when the
  agent reads or edits a file matching one of its globs. This file is an example.
- **Frontmatter gates, not the folder.** `universal/` and `path-scoped/` are human signposting only; the
  loader treats every subfolder identically and looks solely at frontmatter. A frontmatter-less file in
  `path-scoped/` would still load always; a globbed file in `universal/` would still be gated.

## Include order

Entries sort alphabetically by name within a directory — files and subfolders intermixed; a subfolder is
expanded depth-first in place at its slot. Order by importance with numeric prefixes (`01-`, `02-`): a
leading digit sorts before any letter, so it jumps the queue past sibling files *and* folders.

## Rationale sidecars (`name.rationale.txt`)

Keep each rule `.md` lean — only what helps you **follow** the rule (the instruction plus the apply-time
*why* that lets you generalize it to cases the wording didn't enumerate). Anything needed only when
**changing** the rule — provenance, the evidence behind it, rejected alternatives, "don't undo this
because…" — goes in a sidecar named `<rule>.rationale.txt` (the rule's base name with the `.md` dropped and
`.rationale.txt` appended — e.g. `030-defensive-coding.md` → `030-defensive-coding.rationale.txt`).

- **Self-documenting and co-located.** "rationale" in the name says what it is at a glance, and the shared
  prefix sorts it directly beneath its rule, so humans see it in the tree. The `.txt` extension keeps it out
  of the agent's always-on context.
- **Before editing a rule, read its `.rationale.txt` sidecar if one exists.** That's how the rationale
  reaches whoever is changing the rule — human or agent — at the one moment it matters, without taxing every
  session.
- **Write sidecars in Markdown despite the `.txt` extension.** The `.txt` only dodges the loader; it's not a
  signal to drop to plain prose. The agent and humans still read it as Markdown — as with every `.txt` doc in
  this folder (the `README.txt` too).

Worked example: `universal/03-code-standards/030-defensive-coding.rationale.txt`.
