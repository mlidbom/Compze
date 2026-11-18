# Taggregates for JAStudio's notes: what to change in Compze, and in what order

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../shared/the-naming-pass.md](../shared/the-naming-pass.md) and
> [../shared/reviews/end-review-consolidated.md](../shared/reviews/end-review-consolidated.md).

> **Superseded by [the shared conclusions](../shared/README.md),** which both teams agreed on together (2026-11-16).
> This folder is the opus team's record. Where the shared design differs from it:
> - **The order:** the two bugs first, then the small additions, then routing per class in increments.
> - **Slots:** a static of the owner's class, not a slot object per instance.
> - **The hook:** a queue hands a publish from inside the hook over after the call in progress, in the order published,
>   instead of a nested call.
> - **Loading:** `LoadFromSnapshot` is protected and typed on the class, with a snapshot marker of its own, the whole
>   snapshot at version 1 with no version parameter, and the slot adopting the tevents.
> - **No tevent handlers in components;** an exact-class assertion on dispatch makes a forgotten table loud.
> - **`ITaggregateTevent` as a plain `ITevent`:** independent of the rest, and recommended, not "later".

Agreed on 2026-11-16 by two sessions: jastudio-rep-opus, in Compze, and taggregates-opus, in JAStudio
(`C:\Dev\JAStudio.wt2`, branch `compze-taggretates`). Nothing is implemented. What follows is for Magnus to decide.

This is design and exploration work (Magnus, 2026-11-16). In the shared checkout, only this folder changes, and
experiments run in a worktree of their own, on a branch of their own. Another team works on the same question in
parallel, in `../fable`.

## Conclusions

JAStudio can have its notes as Compze.Teventive taggregates after three changes in Compze, made in this order:

1. **[Loading a taggregate from a snapshot](1-loading-a-taggregate-from-a-snapshot.md):** a supported way to restore a
   taggregate from its stored state, without a tevent store. A snapshot is the shortest history that produces the
   state: one tevent for the taggregate and its components, plus one per entity and per shared tomponent. Every part
   restores through the appliers editing uses. Small, and independent of the rest.
2. **[Tevent routing decided once per class](2-tevent-routing-decided-once-per-class.md):** the big one. Today every
   instance builds tevent dispatchers to say how its class applies tevents. That costs 0.6 to 0.9 GB for JAStudio's
   corpus, and is the wrong abstraction regardless of the cost. [A prototype](5-prototype-of-per-class-routing.md)
   measured it: a note with 15 components retains 1,168 B instead of 11,256 B, against 733 B as plain objects, and
   80,000 notes load about 40 times faster. [Routing by convention](6-routing-by-convention-compared.md), the
   alternative to declaring it, was prototyped too: it costs the same, and brings a silent mistake.
3. **[Told once per publish cascade](3-told-once-per-publish-cascade.md):** a hook that tells the taggregate's owner
   when a publish cascade has ended. Its design also fixes reentrancy bugs in `Taggregate.Publish` and
   `ITaggregate.Commit`, allows committing from inside the hook, and asserts that one taggregate is used by one thread
   at a time.

Then, later: [the smaller and later changes](4-later.md).

**All three changes together** were prototyped and measured. A 15-component note would keep about 936 B, some 200 B over
plain objects: about 75 MB for JAStudio's 80,000 notes, against about 59 MB as plain objects and 0.6 to 0.9 GB today.
See [all three combined](5-prototype-of-per-class-routing.md#all-three-changes-combined).

The experiments, none of them for merging, each on a branch of its own, pushed to origin. Their worktrees, in
`C:\Dev\Compze.worktrees\`, were removed on 2026-11-16:

- `taggregate-experiments-opus`: per-class routing (jastudio-rep-opus).
- `taggregate-experiments-opus-convention`: routing by convention, built on it (jastudio-rep-opus).
- `taggregates-opus/snapshot-and-hook`: loading from a snapshot and the per-cascade hook (taggregates-opus). Written up
  in JAStudio's `dev_docs/taggregates/opus/`.
- `taggregates-opus/combined`: all three changes merged and measured (taggregates-opus).

Compze's full suite passes on each.

## The use

- **About 80,000 notes held in memory at once:** 28,004 vocab, 49,162 sentences and 2,554 kanji. JAStudio's
  managed heap is about 733 MB today.
- **Each note stored as a JSON file of its state,** not its history. There is no tevent store, and there won't be one.
- **Tevents for listening, in process, to exactly what changed,** both inside a note (one component following
  another's changes) and across the corpus (indexes, the UI, reparsing sentences after a vocab changes). Today a note
  only says "I changed", after serializing and hashing itself on every edit.
- **Shape:** `JPNote` is the root of an inheritance hierarchy, with `VocabNote`, `SentenceNote` and `KanjiNote` below
  it. A vocab has about 15 components, some of them nested. Some parts recur across note types, such as audio lists
  and media lists, so shared tomponents eventually.
- **Every note is restored at startup.** Edits are rare and one at a time, from the UI. A full rebuild recomputes the
  generated data of every note, in parallel on many threads, with each note touched by one thread only.
- **JAStudio consumes Compze as NuGet packages,** today at the versions that go with Compze.Teventive 0.6.0-alpha.
  Nothing in JAStudio uses Compze.Teventive yet.

## What JAStudio does meanwhile

Nothing in JAStudio moves onto taggregates before the snapshot and the per-class routing have landed. Today's memory cost makes that
unshippable: vocab alone would add 320 to 520 MB.

## What JAStudio will meet that needs no change in Compze

- **Identity is a `TaggregateId`, a Guid.** Every note already has a Guid id: JAStudio's `NoteId(Guid)` will derive from
  `TaggregateId` instead, with the same equality.
- **A subclass's components derive from the top-level base classes:** see "Components typed on the class that declares
  them" in [the per-class routing](2-tevent-routing-decided-once-per-class.md). That works today.
- **`TaggregateTypeValidator` runs in the static constructor of every taggregate, component and entity class.** It
  rejects public setters and public fields on the class, on its tevents, and on every type in the assembly assignable
  to them. It calls `Assembly.GetTypes()` three times per class: with some 50 component classes in a large assembly,
  that's a startup cost per class, not per instance. Worth timing once. The per-class tables add a smaller cost per
  class of their own, when each is declared and resolved.
- **Each published tevent reads `UtcTimeSource.UtcNow` and creates a `TessageId` (a Guid).** During a full rebuild that
  publishes on many notes, that is processor time and garbage, not retained memory.
- **Wrapping a tevent is cheap:** `PublisherTevent.WrapIn` uses a constructor compiled once per closed wrapper type, and
  each call costs two dictionary lookups and a delegate call. 80,000 snapshot loads take milliseconds.

## What Magnus decides

- **The go-ahead,** and the order above.
- **Explicit declarations or convention:** appliers declared explicitly in a table per class, as both sessions
  recommend, or found by convention as `Apply(…)` methods. Both are prototyped and cost the same: see
  [routing by convention, compared](6-routing-by-convention-compared.md), which also describes a third way.
- **Names:** the snapshot marker (`ITaggregateSnapshotTevent` or `ITaggregateRestoredTevent`), and the names the per-class
  routing's prototype uses: `TeventRouting<TInstance>`, `DeclareAppliers`, `DeclareHandlers`, `Extend`, `Component`,
  `Entities`, `SharedTomponent`, `ITeventRoutedMember`, and the properties `TeventAppliers` and `TeventHandlers`.
  They're placeholders.

## Two bugs found on the way

Both were reproduced with 0.6.0-alpha. Only these documents record them: there are no `bug:` comments at the code yet,
since this is design work. The per-cascade hook's design fixes both: see
[told once per publish cascade](3-told-once-per-publish-cascade.md).

- **`Taggregate.Publish`:** a `TeventStream` observer that publishes on the same taggregate gets the cascade's tevents
  twice, and the publish then throws after the state has changed.
- **`ITaggregate.Commit`:** a publish on the same taggregate from inside the commit callback either throws or, worse,
  is never committed: the state changes and the tevent is lost.

## How the measurements were made

- **[The benchmark](benchmark/README.md):** taggregates-opus's `generate.py` measures the bytes a taggregate instance
  retains against Compze.Teventive 0.6.0-alpha, for any number of components and properties per component.
  jastudio-rep-opus's `generate_both.py` measures the prototypes' styles side by side, and the time to load 80,000 notes.
- **[Checks against 0.6.0-alpha](checks-against-0.6.0-alpha/README.md)** are jastudio-rep-opus's: a component typed on
  the subclass that declares it, the reproductions of the two reentrancy bugs, and how a subscriber across every owner
  gets a shared tomponent's tevents.
