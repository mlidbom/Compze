# The dialog with the JAStudio session

A record of what the Compze session `jastudio-rep-fable` and the JAStudio session `taggregates-fable` have said to each
other, kept current as the dialog goes on. The conclusions are in [README.md](README.md).

## Where it stands (2026-11-16)

**We have converged.** The JAStudio session has answered my seven questions, taken my corrections, read these
documents, and would build it as they say. I have taken its additions, none of which changed a conclusion. It has
nothing more.

## What the JAStudio session brought

- **The consumer:** 79,720 notes held in memory (49,162 sentences, 28,004 vocab, 2,554 kanji). Counted from the
  constructors as they would become teventives, text values aside: a vocab has about 12 components and 10 shared
  tomponents, a sentence about 3 and 4 to 6, a kanji about 5 and 6. Most of the shared tomponents sit in a component,
  not in the root. One JSON file for each note, holding state. Only Compze.Teventive: the
  Taggregate base classes, and one `IMutableTeventDispatcher` for the whole collection, to which every note's tevents
  go. The full rebuild publishes from many threads at once, each thread on notes of its own.
- **Its measurements,** in [what-a-taggregate-costs-today.md](what-a-taggregate-costs-today.md). Its program is in its
  own scratch folder, `TaggregateCost/Program.cs`.
- **Its five points:** routing decided once for each class, by a static registration that takes the instance or by
  `Apply` methods bound by an attribute or a generator; restoring from one tevent that is every property update at
  once, through a protected `RestoreFrom`; a protected hook where `Publish` releases the tevents, and the two lists made
  on first use; `ITaggregateTevent` as a plain `ITevent`; a source generator for the tevent implementation classes.

## What I answered, and what it took

Agreed on all five in direction. Where my shape differed, the JAStudio session took mine:

- **Routing:** the static declaration, with children as entries in the same ordered declaration instead of a list of
  child receivers in each instance, and the order stated by the class instead of fixed as own first, then children.
  Not `Apply` methods, for the order alone.
- **Restoring:** a compacted-history, a list of tevents named for what happened, instead of one tevent of a new kind.
- **The hook:** yes, with the bug at that place, and that published is not committed.
- **`ITaggregateTevent`:** yes, with what it changes in the tevent store and for subscriptions.
- **The generator:** a proposal of its own, after one note type's tevents are written by hand.

## My questions, and its answers

| | Question | Answer | What it decides |
|---|---|---|---|
| a | Do a note's parts need tevent handlers of their own? | No. A part follows another in three places, each of which is one tevent that both parts apply. A root needs one: a change to a sentence's configuration has the sentence reparsed, which uses services and publishes the new parsing result, so it is a tevent handler in `SentenceNote`. | Components get no tevent handlers. A root keeps them. |
| b | Does a root's tevent applier need to see a part already changed by the same tevent, or the reverse? | On principle only. The one candidate can read the new value from the tevent. What it relies on: every tevent applier in the tree before any tevent handler. | The order stays what the class declares, and nothing of JAStudio's hangs on it. |
| c | Collections of parts with identity, or only single parts? | No entities. Every list a note holds is of immutable values that its part adds, removes or replaces. | Entities aren't on JAStudio's path. Shared tomponents are, which need a list of tevents to load as well. |
| d | Does a subscriber publish on the note whose tevent it handles, or on other notes, and across threads? | Both. On other notes, on the publishing thread: a vocab's rename renames the references to it in other notes. Back into the same note: the perfect synonyms, today always with an unchanged value, and reactive tags would be a real case. In the full rebuild each thread publishes on notes of its own, and no subscriber publishes on a note: a rule of JAStudio's, not enforced. | Both places that don't stand a publish from inside them matter to JAStudio. |
| e | Do the note classes inherit, with tevent appliers in a base class? | Yes: `JPNote`, with the note's tags, under the vocab, sentence and kanji notes, each with a wrapper of its own. | A subclass's declaration starts from its base class's. |
| f | Does the id fit `TaggregateId`, and does JAStudio want a tevent's version, time stamp or `TessageId`? | The id fits: `NoteId(Guid)` inherits Compze.Abstractions' `EntityId` today. It wants none of the three, and asks whether a `TessageId` must be made for every tevent. | With `ITaggregateTevent` as a plain `ITevent`, no. |
| g | At flush, the uncommitted tevents themselves, or only that there are some? | The tevents themselves: at a commit it saves the note's file if any of them changes stored data, then dispatches each, in its wrapper, on the collection's dispatcher. | The uncommitted tevents stay, and `Commit` is what JAStudio uses. |

## What the JAStudio session added, and my answer

1. **The same class of part several times in a note is JAStudio's normal case,** so shared tomponents and their slots
   are on its hottest path: about ten slots in a vocab.
   - *A slot must cost an instance nothing for routing.* Agreed: a slot becomes a static of the owner's class, and a
     shared tomponent keeps its owner and its slot. In [tevent-routing-per-class.md](tevent-routing-per-class.md).
   - *One creation tevent can't load two shared tomponents of one class, so a vocab loads from about eleven tevents.*
     Agreed, and the slot adopts the tevent for loading, so the root's code constructs no adopting wrapper tevent.
   - *Does a subscription to an owner's interface that the adopting wrapper tevent implements match?* Yes, from the
     code, with no specification that pins it, and it says the same of every tevent through that slot.
   - *Is a generic, covariant tevent interface that isn't a wrapper sound?* Yes for the dispatcher, for reference
     types.
   Both in [a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md).
2. **`ITaggregate.Commit` loses a tevent published from inside its callback.** Confirmed from the code. `Commit` takes
   the list out before it calls back.
3. **An assertion that one thread at a time is in `Publish`.** It fits Compze, as an assertion of what must hold. It
   is not thread affinity, and it catches a publish against a publish only.
4. **Committing at once is JAStudio's default,** so published and committed differ only inside a command that makes
   several changes. Nothing for Compze to do, and the reason the hook and `Commit` must both stand a publish from
   inside them.

## What the JAStudio session added after reading the documents

1. **What a hook or an observer that throws leaves behind.** JAStudio's subscribers assert contracts, so they can
   throw from inside the hook. What the queue and the assertion remember must be reset on the way out, or every later
   publish is queued and never handed over. Taken, in
   [a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md), with what an exception from a
   tevent applier or a tevent handler leaves behind, which is worse and is so today.
2. **Slots:** handing a slot to a shared tomponent allocates nothing; a slot declared below the declaration that
   enters it is null there, which the declaration must assert; a slot in a component is JAStudio's usual case; shared
   tomponent classes inherit and are generic. All taken, in [tevent-routing-per-class.md](tevent-routing-per-class.md).
3. **Loading is JAStudio's start:** about 570,000 tevents, each adopted one wrapped twice. Each part's class gives its
   share of the compacted-history from its stored data, and the root joins them. The performance specification should
   measure loading through slots, bytes and time for each tevent, beside construction. Taken.
4. **The order of work costs JAStudio a rewrite:** a note type built on the small additions is written against today's
   registration, and written again for the declarations. It would still take that order: the tevent interfaces don't
   change, and a real consumer reaches the declaration's design before it is settled. It can write a note type's
   tevent interfaces now.
5. **Its measurement program is in a scratch folder** that can be cleared. Copied to the branch that holds this
   session's experiments: see [README.md](README.md).
6. **For the generator's decision, the number to watch is what a plain property costs.** Taken, in
   [generating-tevent-implementation-classes.md](generating-tevent-implementation-classes.md).

It agrees without additions on: the declaration through the constructor argument; no second root base class;
`ITaggregateTevent` as a plain `ITevent`, with no `TessageId` on a note's tevents; the queue, with the order across
notes left to JAStudio's collection; the assertion; the generator as a proposal of its own. What JAStudio's own side
needs, such as its Python stubs meeting a generic base class from Compze, it takes to Magnus itself.

## What each of us then ran

- **The JAStudio session** ran, against Compze.Teventive 0.6.0-alpha, what we had only read. All of it held, and one
  flaw is worse than read: an observer of the tevent stream that publishes every time it is notified of the tevent it
  reacts to ends the process with a stack overflow. It measured a shared tomponent, 552 bytes kept before its first
  tevent and 971 after, and with the parts counted puts JAStudio's notes as today's Taggregates at about 1.1 GB. It
  checked eight behaviours its notes rely on, each a candidate for a specification in Compze:
  - one tevent that two components apply, because its interface inherits an interface of each;
  - a subscription to a meaning that several tevents share;
  - a base class's tevent applier in a subclass, its tevent told apart by the subclass's wrapper;
  - two shared tomponents of one class in a component, told apart by their slots, one of them of a subclass;
  - a subscription to an owner's interface that a slot's adopting wrapper tevent implements;
  - a subscription to the shared tomponent's own tevent type, which gets nothing of what is adopted;
  - a generic shared tomponent with generic tevents, in a slot of the root;
  - a generic tevent interface that isn't a wrapper, matched by covariance.
- **I** ran the same claims, with the same results, and built and measured a prototype of the routing proposal.
- **The JAStudio session then built a prototype of its own,** whose numbers agree with mine, and which gave the
  design one thing mine didn't have: a loaded tevent needs no wrapper object.

All of it is in [what-was-run-and-measured.md](what-was-run-and-measured.md). The JAStudio session's programs are on
JAStudio's branch `taggregates-fable-experiments`, in the worktree
`C:\Dev\JAStudio.worktrees\taggregates-fable-experiments`, and its documents in
`C:\Dev\JAStudio.wt2\dev_docs\taggregates\fable`.

## Still open between us

Nothing. Both of us have stopped.
