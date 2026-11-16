# Taggregates for JAStudio's notes: the conclusions both teams agree on

**Status: agreed by all four sessions, then reviewed by two independent agents and revised with their agreement**
(2026-11-16). What the review changed is listed in [reviews/discussion.md](reviews/discussion.md). jastudio-rep-opus manages this folder and reports for Compze; taggregates-fable manages JAStudio's
`dev_docs/taggregates/shared/` and reports for JAStudio.

| Team | Compze session | JAStudio session | Their own records |
|---|---|---|---|
| opus | jastudio-rep-opus | taggregates-opus | `../opus/`, and JAStudio's `dev_docs/taggregates/opus/` |
| fable | jastudio-rep-fable | taggregates-fable | `../fable/`, and JAStudio's `dev_docs/taggregates/fable/` |

**JAStudio's side** is in `C:\Dev\JAStudio.wt2\dev_docs\taggregates\shared\`, on JAStudio's branch `compze-taggretates`:
what the notes require of Compze, what the teams found about JAStudio, and what is left on JAStudio's side. This folder
holds the design, the order of work in Compze, and the full list of what Magnus decides.

**The independent reviews**, by an Opus and a Fable agent that took no part in the work, are in [reviews/](reviews/),
with how each finding was settled in [reviews/discussion.md](reviews/discussion.md).

## The question

JAStudio wants its notes to be Compze.Teventive taggregates, so that every change to a note is a tevent, and whatever
follows a change subscribes to exactly the tevents it concerns. It holds about 80,000 notes in memory, each with 8 to 25
mutable parts, and keeps each note's state in a JSON file, not its history. It would use Compze.Teventive alone: no
tevent store and no bus. What must change in Compze.Teventive for that, and how?

## The answer

**Three changes make it possible, and without the first, it isn't.** Today a taggregate builds tevent dispatchers in
every instance to say how its class applies tevents. JAStudio's notes would then cost 0.6 to 1.1 GB more than as plain
objects. Prototypes of the changes, in Compze's real base classes, bring that to about 27 MB more than plain objects.
In Release builds they load a note with components and shared tomponents, the shape nearest JAStudio's, about 9 to 12
times faster, and a note of 15 components about 75 times faster.

1. **Tevent routing decided once per class, not set up per instance.** Each teventive class declares its tevent
   appliers and handlers once, in a static table, and every instance uses it. That is what decides the memory, and it
   is the right model regardless: an applier never comes and goes at run time.
2. **Restoring a taggregate from its stored state,** without a tevent store. Magnus has said, to JAStudio's
   representative, that a restore needn't go through the tevent appliers. So there are two ways, and the consumer
   chooses:
   - **setting each part's state directly,** as JAStudio's constructors do today. Compze then only needs a supported,
     protected way to give the restored taggregate its id, which today takes the `[Obsolete]` internal `Id` setter;
   - **a snapshot:** a short list of tevents the class constructs from its stored state, applied by the same appliers
     that editing uses, for one code path. Designed and prototyped.
3. **A protected hook at the end of each publish cascade,** which tells the taggregate's own class what was published,
   in the order published, so the consumer needs no observer per instance. With it, the two reentrancy bugs fixed, one
   rule for what an exception leaves behind, and an assertion that one thread at a time publishes on a taggregate.

And, once designed: **`ITaggregateTevent` as a plain `ITevent`,** since today every taggregate's tevents claim
exactly-once delivery between processes, which a note's never have.

**Whether JAStudio should make its notes taggregates at all is a separate question, and open.** After Magnus's own
reviews of JAStudio's folder, JAStudio's side says that Compze can be made to carry the notes, but that adopting
taggregates was never sized, or compared with a smaller route: see JAStudio's shared folder. The bug fixes and routing
per class stand on their own for Compze, whose tevent store builds and loads taggregates in every unit of work.

The design of each, what is built and what isn't, and why each debated point was settled the way it was:
[the-design.md](the-design.md). The numbers: [what-was-measured.md](what-was-measured.md). The bugs and the smaller
findings: [bugs-and-findings.md](bugs-and-findings.md).

## The order we propose

1. **The bug fixes.** The queue that hands a cascade's tevents over in the order published, with `Taggregate` feeding
   `TeventStream` first and calling the hook after; `Commit` taking its list before it calls back; the rule that an
   exception escaping `Publish` makes the taggregate refuse further use; `Commit` asserting it isn't called inside an
   unfinished cascade; and the tevent store's two bugs, whose fix depends on decision 6. All are wrong today, whatever
   JAStudio does.
2. **Tevent routing per class:** the table built on the dispatcher's own matching code, the root and its components, slots
   that belong to the owner's class, inheritance through `Extend`, and the safeguards that make a mistake in a table loud.
   With it, a performance specification in Release that bounds the bytes a load allocates, and the published doc
   `Taggregates/_docs/reusable-components.md` rewritten.
3. **Restoring from stored state:** the protected member that gives a restored taggregate its id, and the snapshot
   load, with the slots adopting its tevents, if a consumer wants one code path. With them, the thread assertion and the
   lists created on first use. **JAStudio can start here.** Until step 4, both styles of routing are public, one tree is all one style, and the
   prototype's fallback costs roughly 13 to 25 MB for JAStudio's notes.
4. **The rest of Compze's teventives:** entities, removable entities and shared tentities designed and moved to tables,
   the query models decided, and per-instance registration removed, with the fallback's bytes.
5. **`ITaggregateTevent` as a plain `ITevent`,** once designed.

## What Magnus decides

1. **The go-ahead,** and the order above, including whether to pull step 4's removal of the fallback forward, before
   JAStudio starts.
2. **Tevent routing per class:** whether to do it. It changes protected members of public base classes, so it changes how
   every taggregate is written: 33 registrations in 28 files of Compze's tests and sample, all mechanical, and 13 files in
   `src`, 7 of them the query models'.
3. **Declared, or by convention:** both are prototyped and cost the same. Both teams and both reviewers recommend the
   declared table, with its safeguards: by convention, a misspelled tevent handler is silently never called. A third
   way, attribute-marked methods, shares the convention's flaw that several matching appliers run in metadata order.
4. **The names:** every name in [the-design.md](the-design.md) is a placeholder.
5. **What an exception leaves behind:** one rule, that an exception escaping `Publish`, from a tevent applier, a tevent
   handler, the invariants, the hook or an observer, makes the taggregate refuse publishing and committing from then on.
   Its state can't be trusted, and today it looks usable. Both teams, both reviewers and JAStudio prefer it. For
   JAStudio it means every reference to a refused note, such as indexes and view models, is replaced by a reloaded note.
   Only what escapes counts: a hook that catches a failing subscriber inside itself sets the consumer's own policy, and
   for JAStudio, whose subscribers assert contracts, that decides how often a note is refused in practice.
   Prototyped on `taggregates-opus/combined`, though marking where the exception escapes, as the design says, is still
   to be built.
6. **May a tevent store's participation handler change the taggregate whose tevent it handles,** in the same unit of
   work? Nothing does today, since bug 1 breaks it. The answer decides the thread assertion and the store's fix:
   - **No** (recommended): the thread assertion covers the whole of `Publish`, as JAStudio needs against its race
     between a background reparse and an edit in the UI, and the tevent store asserts against the re-entry itself.
   - **Yes:** the assertion is released around each call out to an observer or the hook, so JAStudio's race goes mostly
     uncaught, and the store's `Save` must track, subscribe and publish through one path, in order.
7. **Three changes to public API, to veto or accept:**
   - taggregate tables without `BeforeHandlers`, `AfterHandlers` and `ForGenericTevent`, which nothing needs, by YAGNI on
     a new type, while `IMutableTeventDispatcher` keeps them;
   - restoring: a protected member that gives a restored taggregate its id, and, if wanted, a protected
     `LoadFromSnapshot(TaggregateId id, …)` with no marker interface;
   - the thread assertion, as a change of behaviour.
8. **`ITaggregateTevent` as a plain `ITevent`,** once designed, in one of two variants:
   - the interface only, truthful and saving nothing;
   - moving the tevents' identity to the tevents that need it as well, which touches every stored taggregate's tevents and
     the migrations, and saves JAStudio about 18 MB of garbage per start.
9. **The query models:** move them to tables in step 4, or keep their per-instance registration.
10. **The source generator for tevent implementation classes:** both teams propose it as an effort of its own, after
    JAStudio has written one note type's tevents by hand.
11. **Loading without a wrapper object:** a measured saving of garbage at startup, to decide once the performance
    specification measures a real start.

**Work that needs no decision,** listed in [bugs-and-findings.md](bugs-and-findings.md):
- specifications of eight behaviours JAStudio relies on that nothing pins;
- the validator's 3 to 9 scans of whole assemblies per class;
- `PublisherTevent.WrapIn`'s closure per call;
- `Compze.Core.slnx`, which doesn't build;
- a stale doc comment;
- the root README's claim that in-memory overhead is "entirely negligible".

## Where the work is

Nothing is merged, and nothing in Compze's `main` is changed. The prototypes are experiments, each on a branch and
worktree of its own under `C:\Dev\Compze.worktrees\`. Their API isn't the designed one where the design changed after
they were built: for example, `LoadFromSnapshot` there is public on `ITaggregate` and takes a version.

| Branch | Built by | What it holds |
|---|---|---|
| `taggregate-experiments-opus` | jastudio-rep-opus | Routing per class in the real base classes: components, entity collections, slots, inheritance. Full suite passes. |
| `taggregate-experiments-opus-convention` | jastudio-rep-opus | The same, by convention, for comparison. Full suite passes. |
| `taggregates-opus/snapshot-and-hook` | taggregates-opus | Loading from a snapshot, the hook with its queue, both bug fixes, the assertion of one thread, and the refusal of further use after a failed cascade (4b42470ba). Full suite passes. |
| `taggregates-opus/combined` | taggregates-opus | The two above merged, with the queue, and measured. Full suite passes (604c00df4). |
| `taggregate-experiments-fable` | jastudio-rep-fable | A standalone prototype of routing per class with static slots, and the claims run against 0.6.0-alpha. |
| `taggregate-experiments-fable-static-slots` | jastudio-rep-fable | Static slots in the real base classes, built on the combined branch, and measured (fb75d60a0). `Compze.Tests.Unit` passes, 496 of 496; the other test assemblies weren't run on it. |
| `review-taggregate-improvements` | the Opus reviewer | Two specifications showing the thread assertion and the hook's base call against the tevent store (861474504). In `C:\Dev\Compze\.claude\worktrees\agent-a5042660ba8c7b64c`. |
| `review-taggregate-conclusions` | the Fable reviewer | Seven checks against a Release build of the static-slots prototype (a440911a7). In `C:\Dev\Compze\.claude\worktrees\agent-ad538039c7b0a5f4e`. |
