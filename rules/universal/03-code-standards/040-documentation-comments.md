# Documentation comments

This file is long on purpose. Doc comments are the one thing agents fail at over and over, every time for
the same reason. Read all of it. The *why* is in here, not just the letter — if you only follow the letter
you will regress to the failure the moment a case isn't spelled out.

## 0. Who reads these. Read this first; it is the why behind everything.

Doc comments have two audiences, and accessibility decides which one applies:

- **Public types and members → consumers of the library and contributors.** They may know little to nothing about it: not
  its type vocabulary, not its architecture, often not even the problem space. The doc comment IS the
  product documentation — it surfaces in IntelliSense and on docs sites, usually with no source code next
  to it, so it must stand entirely on its own.
- **Internal and private members → contributors.** A new contributor seeing the codebase for the first
  time, or one coming back after significant time away — including the owner, whose memory of a layer an
  agent wrote under direction fades like anyone else's.

Both audiences share the same profile, so the same principles apply everywhere:

- **They are competent programmers.** Never explain programming. Loops, async, generics, OOP, DI, design
  patterns, threading-in-the-abstract.
- **They do NOT know this codebase** — its types, its architecture, its domain vocabulary.
- **Do NOT assume they know the specialized substrate the code is built on** — native OS APIs and interop,
  COM, wire protocols, vendor SDKs, undocumented internals: whatever layer had to be researched rather than
  already known when it was written.

The load-bearing fact: **whoever opens a doc comment does so because they do not know what the thing is or
what it is for. That is the only reason anyone reads a comment.** They are not there for your clever
caveat. They are there for "what is this thing."

Forget this and you will write comments that are useless to every reader they will ever have.

## 1. The failure you keep committing. This section is about you, the agent.

You know everything. The basic purpose of any type or member is so obvious to you that it never registers as
something worth writing — it feels like writing "water is wet." So you silently skip it, and you spend the
entire comment on the only thing that feels non-trivial *from where you stand*: the advanced caveat, the
precise mechanism, the edge case. Then you anchor that detail by naming a row of proper nouns — type names,
API names, domain terms.

That produces garbage, and here is exactly why: **the reader is reading because they do NOT know the basic
purpose. And if they don't know the basic purpose, the odds they know your proper nouns are near zero.** Every
unexplained proper noun is a locked door. A comment that explains a thing only in terms of *other unknown
things* explains nothing — it relocates the confusion and walks away.

A real comment an agent wrote and was pleased with (from a virtual-desktop overlay project):

> One TileGroup's tiles within a Surface: a full set of DesktopTiles laid out as a grid into the group's
> region of the surface.

Delete the proper nouns: *"One ___'s ___ within a ___: a full set of ___ laid out as a grid into the _'s
_ of the _."* Nothing left says what a `TileGrid` **is**. The plain truth — "the grid of live
desktop previews you actually look at, one per virtual desktop" — was never stated. The agent assumed several
locked doors would carry it. They carried nothing. And note: that comment was already laid out nicely and had
every proper noun turned into a clickable `<see cref>`. **A clickable lock is still a lock.** Navigation and
formatting do not substitute for stating the purpose.

Stop. Write the floor first.

## 2. The structure that fixes it: `<summary>` = WHAT, `<remarks>` = WHY, then jargon

### `<summary>` — the newbie floor (the WHAT)

Plain words. No jargon. "What is this thing, to someone who has never heard of it, and what is it for." One or
two sentences. A relatable real-world anchor is fine and good — "Task Manager", "the taskbar", "the wallpaper"
— those are doors the reader can already open. Codebase and substrate jargon are not; they go to remarks.

The test, every time: **delete the proper nouns from your summary. If nothing remains that says what the thing
is for, you have not written the comment yet.**

### `<remarks>` — in this order

1. **The WHY, written as plainly as the summary.** Why does this exist; why is it needed; what breaks without
   it. The why is the second most important thing after the what — it is NOT a technical detail, and it must
   never be left as jargon. "blind by UIPI to High-IL windows" is four locked doors, not a why. **Explain the
   concept the why rests on, in plain terms, and only then name it** so the reader has a searchable handle:
   *"Windows stops a normal program from seeing keyboard input meant for a program running as administrator —
   a security wall between privilege levels (this is UIPI)."* Now `UIPI` is a labelled door, not a locked one.
2. **The mechanics / anchors** — proper nouns, API names, the exact mechanism, caveats. This is the part most
   readers rarely care about when reading a doc comment — for that they go to the code. So it lives at the
   bottom, where the rare reader who wants it finds it and everyone else skips it.

The IDE tooltip leads with the summary and shows remarks below/after, so the floor surfaces first and the
jargon is one glance further down. That is exactly the priority order you want.

### The worked example that is correct (the bar to clear)

From a Win32-heavy project:

```csharp
///<summary>
/// Optionally relaunches Vantage as administrator so its global hotkeys keep working even when a window
/// running as admin — Task Manager, an elevated editor — is in the foreground.
///</summary>
///<remarks>
/// Windows deliberately stops a normal program from seeing keyboard input aimed at a program running as
/// administrator — a security wall between privilege levels. Vantage catches its hotkeys with a system-wide
/// keyboard hook, and that wall makes the hook blind to keystrokes whenever an admin window has focus, so the
/// hotkey silently does nothing. Running as admin too puts Vantage on the same side of the wall.<br/>
/// <br/>
/// The mechanics: the hook is a <c>WH_KEYBOARD_LL</c> low-level keyboard hook; the wall is UIPI (User Interface
/// Privilege Isolation); admin windows run at a "High" integrity level. The relaunch uses the shell's
/// <c>runas</c> verb, and the freshly-elevated instance evicts the old one through
/// <see cref="SingleApplicationInstanceManager"/>.
///</remarks>
```

Summary: zero jargon, a newbie knows what `Elevation` is for. Remarks lead with the *concept* of the security
wall (no `WH_KEYBOARD_LL`/`UIPI`/`High-IL` needed to follow it), then unpack the jargon as searchable anchors.
Nothing was deleted — the technical content all survived, it just moved to where it belongs.

## 3. What is good content, and what is not

- **Restating the code is not a comment.** The signature already shows `pins an array`, `returns the
  address`, `disposes the handle`. If the comment only says what the body plainly shows, you narrated the
  code; you didn't comment it.
- **External facts about what the thing IS in the real world are GOLD, not restatement.** A type that wraps an
  OS or library concept *should* describe that concept's real behaviour — "the taskbar is one per monitor but
  shared across all virtual desktops", "PrintWindow returns black for DWM-composited content". That is exactly
  the knowledge the code cannot carry and the reader does not have. This is the single most valuable thing a
  comment can hold. Never cut it as "describing the OS instead of our abstraction" — the substrate is
  precisely the part your readers don't know.
- **The explanation you'd give in chat IS the comment.** When asked "what is this for," you answer in chat
  clearly, in plain words, with a scene. Then in the file you write an inert noun-phrase definition. That is
  the split that keeps failing. Put the chat answer — its voice, its scene-setting, not just its facts — into
  the file. Reword ≠ fix.

## 4. References: tag every mention of a type or member

A bare word could be anything to a reader new to the codebase. A tagged reference tells them it's a real symbol
and lets them click straight to it. So:

- **Anything that resolves to a real type or member → `<see cref="…"/>` — ours AND referenced libraries
  alike.** A framework's `Window`, a vendor SDK's interface, any referenced library's type or method: cref
  them. Modern IDEs navigate to library symbols too — via XML docs, Source Link source download, or
  decompilation — so a cref into a referenced assembly is a real, clickable reference, not a "lie." It's
  build-validated as well: cref resolution runs over the whole reference closure, so `CS1574` (see §6) guards
  an external cref exactly like one of ours. Use the form that resolves — `<see cref="Type.Member"/>` across
  types, and lean on the file's `using`s so a short `<see cref="ISomeLibraryType"/>` resolves. **Default to
  the real reference for anything it works on.**
- **`<c>…</c>` is the fallback — only for things that are NOT resolvable symbols.** A raw native-API name not
  surfaced as a referenced managed symbol, an acronym or concept (`UIPI`, "High integrity level"), a shell
  verb (`runas`), a filename. Also use it for **one of our own types that's higher in the dependency graph**,
  where the cref genuinely won't resolve — add a breadcrumb naming its home (e.g. "(Overlay)") so the reader
  can still find it.
- **When unsure, write the `<see cref>` and let the build decide.** Doc-gen is on; a cref that can't resolve
  fails with `CS1574`. That failure — not a guess about whether we "have the source" — is your signal to drop
  to `<c>`.
- **Method parameters → `<paramref name="…"/>`; type parameters → `<typeparamref name="…"/>`.**
- Do not cref an English word that merely happens to match a member name. "Show it" (show the window) is not
  `Show()` unless it really refers to calling that method. Use judgment.

But remember §1: crefs are navigation, not explanation. **Tagging the proper nouns does not excuse you from
stating the purpose in plain words.** A fully-crefed comment can still be useless.

## 5. Semantic XML tags and formatting

- **Make sure to use the semantic XML tags** — they carry meaning the tooling acts on: `<summary>`, `<remarks>`,
  `<see cref>` / `<seealso>`, `<param>` / `<paramref>`, `<typeparam>` / `<typeparamref>`, `<returns>`,
  `<exception>`, `<inheritdoc>`.
- A long single-line summary is hard to read — break it across lines with <br/>.

## 6. Crefs are build-validated — keep them green

These standards expect `GenerateDocumentationFile=true` (with `CS1591` silenced), typically set in
`Directory.Build.props`, so the compiler validates every `<see cref>` and fails the build (`CS1574`) on a
broken or stale one. This is the whole reason crefs beat backticks: a rename that doesn't update a cref breaks
the build instead of rotting silently. If a project doesn't have doc generation on, turn it on; do not leave a
cref pointing at something that no longer exists.

## 7. Name the current reality, don't idolise it

- Nothing about the solution's details is fixed. Never write a comment that implies something is proven,
  settled, or must-not-change — that argues against the refactoring these standards encourage
  ([conceptual coherence](../010-conceptual-coherence-and-naming.md)).
- State what we **define** (a type's responsibility, an API's contract) in plain present tense — not hedged,
  not crowned.
- State what we **observed** (a measurement, an external API's behaviour) as provisional — "in our tests",
  "seems", with the rig/date it held on. A handful of throwaway probes on one machine is evidence, not absolute truth or law.
- Applies to standalone docs (`*.md`) too: keep what the code can't tell you — the why and the external facts
  — not a restatement of the code.

## 8. Length

The summary stays short — it's the floor, and short is part of what makes it land. Remarks can be as long as
the why honestly needs; it's skippable, so length there costs the reader nothing. Don't pad either one, and
never buy brevity by stripping the setup that makes the thing land. Understanding is the bar. Brevity is a
tie-breaker, never a goal that overrides being understood.

## 9. Which members get a comment — and why the length is free

The IDE collapses every doc comment to its **first line** until you hover or expand it. So the cost you'd
normally weigh — "is a comment here worth the vertical space?" — isn't real: folded, it's one line no matter
how long the comment is, and the rest is opt-in. That flips the default toward documenting more, not less.

- **Document every member unless it is trivial *by its name, to the readers in §0*** — not to a C# expert.
  `Commit()`, `Pin()`, `Cloak()`, `CreateTarget()`, `DefaultProcessing()` look trivial to anyone who knows the
  framework, and are exactly the boundary verbs your readers do *not* know — so they get a comment (what does
  "commit" a composition even mean?). Skip only what the name tells them in full: `Right => Left + Width`, an
  operator, an `Id` whose type says it all.
- **Same shape as a type, scaled down:** a short plain-floor `<summary>` (the line seen folded) plus, where
  there's a why / mechanism / gotcha, a `<remarks>` as long as it honestly needs (§8). The worked example in
  §2 is the bar — and it applies to private members too, `<param>`/`<returns>` included.
- **What free length does NOT license: zero-information filler.** `<param name="window">The window.</param>`
  isn't even deletable-useful — it just echoes the signature. A documented member or param must add something
  the name doesn't, however small. Be generous with *information*, never with noise.

This removes the fear of over-documenting. You can't really: the worst case is a line a reader finds obvious
and skips in two seconds. A missing line on a boundary verb they don't know is the expensive failure — so when
unsure, write it.
