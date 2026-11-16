# Taggregates for JAStudio's notes: what Compze.Teventive should change

2026-11-16. The findings and conclusions of the Compze session `jastudio-rep-fable`, from a dialog with the JAStudio
session `taggregates-fable`. Nothing is built: this is for Magnus to decide on.

**The question.** JAStudio wants its notes to be Taggregates, so that every change to a note is a tevent, and whatever
follows a change subscribes to exactly the tevents it concerns (Magnus, 2026-11-16, in JAStudio's
`dev_docs/new_ideas_to_discuss.md`). JAStudio holds about 80,000 notes in memory, each with 8 to 20 mutable parts. It
would use only Compze.Teventive: no tevent store and no bus. A note's file keeps holding the note's state, not its
history. What must change in Compze.Teventive for that, and how?

**What this rests on.** Reading the code on `main` at `a6317da47`, where Compze.Teventive is 0.6.0-alpha, and then
running it: every claim made from reading was run against the released package, and the central proposal was built as
a prototype and measured beside today's Taggregate. The results are in
[what-was-run-and-measured.md](what-was-run-and-measured.md). Nothing in Compze is changed: the experiments are on a
branch of their own, `taggregate-experiments-fable`.

## Conclusions

1. **Decide tevent routing once for each teventive class, not for each instance.** This is the change that decides
   whether JAStudio can use Taggregates at all: today a Taggregate with 4 tevent appliers keeps 1.6 KB and each
   component about 1.0 KB, against 350 bytes for the same data as plain objects, which for JAStudio is 0.5 to 1 GB.
   Compze needs it for itself too: the tevent store constructs and hydrates a taggregate on every load, in every unit
   of work. The shape: a static declaration per class, with the verbs and the ordering rule of today, in which the
   lambdas take the instance and a component or a collection is an entry like any tevent applier.
   **Measured on a prototype:** a vocab with four components and four shared tomponents keeps 584 bytes where today's
   Taggregate keeps 10,204 and plain objects 350, and loads in under 3 µs instead of 35. For JAStudio's notes that is
   about 30 MB more than plain objects, where today's Taggregate would be about 1.1 GB. The JAStudio session built a
   prototype of its own, and measured the same.
   See [tevent-routing-per-class.md](tevent-routing-per-class.md), and for what the cost is made of,
   [what-a-taggregate-costs-today.md](what-a-taggregate-costs-today.md).
2. **Let a taggregate load from tevents its own code constructs.** A note's stored state, read back, is a
   compacted-history: its history rewritten to the fewest tevents that give the same state. No new kind of tevent is
   needed for it: a creation tevent already is every property update at once. What is missing is that only `Publish`
   and the tevent store can give a tevent its version, so nobody else can make a history that loads.
3. **Tell a taggregate's own class when it has published tevents,** through a protected hook, so that a consumer
   needs no observer per instance; and create a taggregate's two lists and its tevent stream on first use. With it:
   - **Two bugs to fix, whatever JAStudio does.** A publish from inside an observer of the tevent stream notifies
     tevents twice and then throws, or ends the process with a stack overflow when the observer reacts every time. And
     a tevent published from inside `Commit`'s callback is lost without a word. The fix of the first must keep the
     tevents in the order they were published.
   - **An assertion that one thread at a time is in `Publish`,** so that a race fails at its cause.
4. **Make `ITaggregateTevent` a plain `ITevent`,** and let each taggregate's root tevent interface say how its tevents
   are delivered. Today the base interface says exactly-once for every taggregate there is, which a note's tevents
   are not. Nothing breaks structurally. Every taggregate that is delivered exactly-once has to say so, which is about
   five of the tests' and the sample's taggregates, and the tevent store has to ask for the `TessageId` it stores,
   which a note's tevents then needn't have.
5. **A source generator that writes tevent implementation classes is a proposal of its own,** to decide after JAStudio
   has written one note type's tevents by hand. It would be the first Roslyn generator in Compze.
6. **Not recommended: a second root base class, a Taggregate without what the tevent store needs of it.** JAStudio
   uses most of that: `Commit`, the uncommitted tevents, loading from a history. What it doesn't use is on the tevents,
   and conclusion 4 takes that away.

Conclusions 2 to 4 and 6 are in
[a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md), conclusion 5 in
[generating-tevent-implementation-classes.md](generating-tevent-implementation-classes.md).

## The order I would do it in

1. **The two bugs** (conclusion 3): small, and wrong today whatever JAStudio does.
2. **Loading from a compacted-history, the hook, the assertion, and the lists on first use** (conclusions 2 and 3):
   small additions that let JAStudio build one note type as a Taggregate and learn from it, before the memory is
   solved.
3. **Tevent routing for each class** (conclusion 1), in increments that each pass the full suite: the root and its
   components, then shared tomponents, which with those two is all that JAStudio uses, then entities and their
   collections, then the query models, then the removal of registration per instance.
4. **`ITaggregateTevent` as a plain `ITevent`** (conclusion 4): independent of the others, whenever Magnus has decided.

With it, a performance specification in Compze that loads many taggregates, through slots too, and publishes on them,
and reports the bytes kept and allocated and the time for each, so that the cost is measured where it is caused. The
program that measured the prototype is the start of it.

## What is verified, and what is not

- **Read in the code, and then run:** the two bugs, what an exception leaves behind, loading a tevent constructed
  outside `Publish`, a slot in a component, and what a subscriber can subscribe to. All came out as read.
- **Measured:** what today's Taggregate costs, by the JAStudio session and again by me with the same numbers, and
  what a prototype of the proposal costs, by each of us on a prototype of our own. The bytes are exact. The times are
  from a few runs on a machine that other work shares.
- **Run on the prototype:** the queue that hands over published tevents in order, `Commit`, loading from a
  compacted-history, the assertion of one thread in `Publish`, and many threads through shared declarations.
- **Read in the code only:** how the tevent store's session and the publisher work, and so what `ITaggregateTevent`
  as a plain `ITevent` changes. Which of the tests' and the sample's taggregates it touches is from searching them.
- **Not read:** all 41 files that register tevent appliers (I read the base classes and about ten of them), and
  `TaggregateHistoryValidator`.
- **From the JAStudio session, not checked by me:** how JAStudio's notes are built. Its answers to my questions, and
  where we stand, are in [the-dialog-with-the-jastudio-session.md](the-dialog-with-the-jastudio-session.md).

## What Magnus has to decide

1. **Tevent routing for each class:** whether to do it, and the names. It changes protected members of public base
   classes, and so how every taggregate is written.
2. **The two bugs:** to fix them needs no decision, only his word to start, since nothing was to be built.
3. **The small additions to `Taggregate`,** and their names: loading from a compacted-history, the hook, the assertion
   of one thread in `Publish`, the lists on first use.
4. **`ITaggregateTevent` as a plain `ITevent`:** a change to a public interface.
5. **The generator:** now, later or never.
6. **Specifications for what JAStudio's notes rely on and no specification pins:** a slot in a component, a
   subscription to an owner's interface that an adopting wrapper tevent implements, and the rest of the eight
   behaviours in [the-dialog-with-the-jastudio-session.md](the-dialog-with-the-jastudio-session.md).
7. **`TaggregateTypeValidator`'s scans** of whole assemblies, several for each class: make it one for each assembly.
8. **A taggregate that an exception has left `Publish` from a tevent applier or a tevent handler:** whether it should
   refuse every later use, since its state isn't to be trusted. Today it looks usable.

## The files

| File | What is in it |
|---|---|
| [what-a-taggregate-costs-today.md](what-a-taggregate-costs-today.md) | Where the bytes go today, what depends on the order of registration, and the smaller findings |
| [tevent-routing-per-class.md](tevent-routing-per-class.md) | The proposal for conclusion 1 |
| [a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md) | Conclusions 2 to 4 and 6 |
| [generating-tevent-implementation-classes.md](generating-tevent-implementation-classes.md) | Conclusion 5 |
| [what-was-run-and-measured.md](what-was-run-and-measured.md) | The experiments, their numbers, and where they are |
| [the-dialog-with-the-jastudio-session.md](the-dialog-with-the-jastudio-session.md) | What the JAStudio session and I asked and answered each other |
