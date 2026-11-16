# Taggregates for JAStudio's notes: the conclusions both teams agree on

**Status: draft, under review by the four sessions** (2026-11-16). This folder holds what the two teams that worked on
the question separately have agreed on together. jastudio-rep-opus manages it and reports for Compze; taggregates-fable
manages JAStudio's `dev_docs/taggregates/shared/` and reports for JAStudio.

| Team | Compze session | JAStudio session | Their own records |
|---|---|---|---|
| opus | jastudio-rep-opus | taggregates-opus | `../opus/`, and JAStudio's `dev_docs/taggregates/opus/` |
| fable | jastudio-rep-fable | taggregates-fable | `../fable/`, and JAStudio's `dev_docs/taggregates/fable/` |

**JAStudio's side** is in `C:\Dev\JAStudio.wt2\dev_docs\taggregates\shared\`, on JAStudio's branch `compze-taggretates`:
what the notes require of Compze, what the teams found about JAStudio, and what is left on JAStudio's side. This folder
holds the design, the order of work in Compze, and the full list of what Magnus decides.

## The question

JAStudio wants its notes to be Compze.Teventive taggregates, so that every change to a note is a tevent, and whatever
follows a change subscribes to exactly the tevents it concerns. It holds about 80,000 notes in memory, each with 8 to 25
mutable parts, and keeps each note's state in a JSON file, not its history. It would use Compze.Teventive alone: no
tevent store and no bus. What must change in Compze.Teventive for that, and how?

## The answer

**Three changes make it possible; without the second, it isn't.** Today a taggregate builds tevent dispatchers in
every instance to say how its class applies tevents. JAStudio's notes would then cost 0.6 to 1.1 GB more than as plain
objects. Prototypes of the changes, built in Compze's real base classes with Compze's full suite passing, bring that to
about 30 MB more than plain objects, and load notes about 15 to 40 times faster.

1. **Tevent routing decided once per class, not set up per instance.** Each teventive class declares its tevent
   appliers and handlers once, in a static table, and every instance uses it. That is what decides the memory, and it
   is the right model regardless: an applier never comes and goes at run time.
2. **Loading a taggregate from a snapshot,** without a tevent store: a short list of tevents that the class constructs
   from its stored state, applied by the same appliers that editing uses.
3. **A protected hook at the end of each publish cascade,** which tells the taggregate's own class what was published,
   in the order published, so the consumer needs no observer per instance. With it, the two reentrancy bugs fixed, and
   an assertion that one thread at a time publishes on a taggregate.

And, independently: **`ITaggregateTevent` as a plain `ITevent`,** since today every taggregate's tevents claim
exactly-once delivery between processes, which a note's never have.

The design of each, and why each debated point was settled the way it was: [the-design.md](the-design.md).
The numbers: [what-was-measured.md](what-was-measured.md). The bugs and the smaller findings:
[bugs-and-findings.md](bugs-and-findings.md).

## The order we propose

1. **Fix the two bugs** in `Taggregate.Publish` and `ITaggregate.Commit`. They are wrong today, for the tevent store
   too, whatever JAStudio does.
2. **The small additions to `Taggregate`:** loading from a snapshot, the hook with its queue, the assertion of one thread,
   the lists and `TeventStream` created on first use.
3. **Tevent routing per class,** in increments that each pass the full suite: the root and its components, then shared
   tomponents and their slots, then entities and their collections, then the query models, then removing the
   per-instance registration. JAStudio moves nothing onto taggregates until slots are in: every note type needs them.
4. **`ITaggregateTevent` as a plain `ITevent`,** whenever Magnus decides.

With them, a performance specification in Compze that loads many taggregates, through slots too, publishes on them, and
bounds the bytes allocated, so the cost is measured where it's caused.

## What Magnus decides

1. **The go-ahead,** and the order above.
2. **Tevent routing per class:** whether to do it. It changes protected members of public base classes, so it changes how
   every taggregate is written: 56 registrations in 41 files of Compze's tests and sample, all mechanical.
3. **Declared, or by convention:** both are prototyped and cost the same. Both teams recommend the declared table: by
   convention, a misspelled tevent handler silently never runs. A third way, attribute-marked methods, is described in
   [the-design.md](the-design.md).
4. **The names:** every name in [the-design.md](the-design.md) is a placeholder.
5. **A taggregate that an exception has left `Publish` from a tevent applier, a tevent handler or the invariants:**
   should it refuse every later use? Its state can't be trusted, and today it looks usable. Both teams and JAStudio
   prefer it to refuse. It's prototyped and specified on `taggregates-opus/combined`: a flag set when the outermost
   cascade starts and cleared once its invariants hold, so a taggregate whose cascade didn't complete refuses
   publishing and committing.
6. **`ITaggregateTevent` as a plain `ITevent`:** a change to a public interface. Both teams recommend it whatever JAStudio
   does.
7. **The source generator for tevent implementation classes:** both teams propose it as an effort of its own, after
   JAStudio has written one note type's tevents by hand.
8. **Specifications** for eight behaviours JAStudio's notes rely on and nothing in Compze pins, listed in
   [bugs-and-findings.md](bugs-and-findings.md).
9. **`TaggregateTypeValidator`'s scans** of whole assemblies, three per class: make it one per assembly.
10. **Loading without a wrapper object:** a measured saving of garbage at startup, to decide when the performance
    specification measures a real start.

## Where the work is

Nothing is merged, and nothing in Compze's `main` is changed. The experiments, each on a branch and worktree of its own
under `C:\Dev\Compze.worktrees\`:

| Branch | Built by | What it holds |
|---|---|---|
| `taggregate-experiments-opus` | jastudio-rep-opus | Routing per class in the real base classes: components, entity collections, slots, inheritance. Full suite passes. |
| `taggregate-experiments-opus-convention` | jastudio-rep-opus | The same, by convention, for comparison. Full suite passes. |
| `taggregates-opus/snapshot-and-hook` | taggregates-opus | Loading from a snapshot, the hook with its queue, both bug fixes, the assertion of one thread, and the refusal of further use after a failed cascade (4b42470ba). Full suite passes. |
| `taggregates-opus/combined` | taggregates-opus | The two above merged, with the queue, and measured. Full suite passes (604c00df4). |
| `taggregate-experiments-fable` | jastudio-rep-fable | A standalone prototype of routing per class with static slots, and the claims run against 0.6.0-alpha. |
| `taggregate-experiments-fable-static-slots` | jastudio-rep-fable | Static slots in the real base classes, built on the combined branch, and measured (fb75d60a0). `Compze.Tests.Unit` passes. |
