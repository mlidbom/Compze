# The discussion of the two reviews

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../the-naming-pass.md](../the-naming-pass.md) and [end-review-consolidated.md](end-review-consolidated.md).

2026-11-16. jastudio-rep-opus's proposed resolution of each finding of the
[Opus review](review-by-an-opus-agent.md) and the [Fable review](review-by-a-fable-agent.md), for the three of us to settle.
The outcome is recorded at the end, and the shared conclusions are changed to match.

## Proposed resolutions

**R1. The one-thread assertion against the tevent store's blocking bridge** (Opus 1, Fable 5). Both reviews show that the
assertion, as prototyped, refuses a store handler that continues on a pool thread while the publishing thread blocks in
`TeventStoreUpdater.OnTaggregateTevent`. Proposal:
- **The claim covers applying, the tevent handlers and the invariants, and is released before the hand-over** to
  `TeventStream` and the hook. A publish during the hand-over joins the queue.
- **The residual gap is named:** a truly concurrent publish during the hand-over isn't caught.
- **Keying the claim on the flow of execution (`AsyncLocal`) was considered and rejected:** a task started from inside a
  publish carries the same flow, so it could publish concurrently undetected, and it costs an allocation per outermost
  call.
- **Whether a store participation handler may change the taggregate whose tevent it handles becomes a decision for
  Magnus,** pinned by a specification through the real store whichever way he decides.

**R2. `TeventStream` is fed by `Taggregate` itself, and then the hook is called, empty by default** (Opus 2, Fable 2). An
override that forgets its base call can no longer silence the tevent store's saving.

**R3. What any exception out of `Publish` leaves behind** (Fable 2, Opus 6). One rule: an exception that escapes `Publish`
makes the taggregate refuse publishing and committing from then on. That covers the tevent appliers, the tevent
handlers, the invariants, the hook and the observers. No tevent is silently dropped, and nothing is handed over twice.
- **JAStudio's consequence is stated:** every reference to a refused note, such as indexes and view models, must be
  replaced by a reloaded note.
- **Rejected:** keeping the failed cascade queued for re-delivery. It hands the tevent again to observers that may have
  acted on it in part.
- **This widens decision 5.**

**R4. Make the declared table's mistakes loud** (Fable 1). Adopt all five safeguards:
- a part asserts, when constructed, that its parent's table routes to it;
- a slot's route-back asserts that the tomponent it reaches was constructed with that slot;
- a class's first table comes only from a protected static factory on Compze's base class, typed on the class, and a
  subclass's only from `Extend`;
- the table is typed on the class's tevent root, so `For` keeps today's constraint, and `For` keeps
  `TessageTypeInspector.AssertValidForSubscription`;
- the exact-class assertion, with the property abstract at the root.

The case for declaring over convention stands, but it's strongest only with these.

**R5. The measurements** (Fable 3, Opus 5).
- **State the Release numbers,** per shape: about 9 to 12 times faster for the slot shape nearest JAStudio's, and about
  75 times for the 15-component shape.
- **Kept bytes are exact in any build;** allocated bytes depend on the build.
- **About 27 MB** over plain objects for JAStudio's notes.
- **Startup garbage of about 0.4 GB per start** (about 750 B per loaded tevent in Release) is the largest remaining
  cost. It's to be reduced by avoiding the closures and `ScopedChange` on the load path, and later perhaps by
  wrapper-free loading.

**R6. Say what is built, what is designed, and what isn't** (Opus 3, Fable 4).
- **The tables carry what taggregates use:** `For`, `IgnoreUnhandled`, and the parts, with the slot's `ForWrapped`
  internal. `BeforeHandlers`, `AfterHandlers` and `ForGenericTevent` stay on `IMutableTeventDispatcher` only.
- **Matching uses the dispatcher's one translation rule and code:** the first increment of the routing work.
- **Still to design, before per-instance registration can go:** removable entities, shared tentities, and how Compze's
  own base classes (`Tentity` and the rest) add their appliers.
- **The query models are a decision of their own.**

**R7. `ITaggregateTevent` as a plain `ITevent` needs design before it's a decision** (Opus 4, Fable 6). The tevent store
identifies every stored tevent by its `TessageId`, so every stored hierarchy needs identity, whether or not it is
delivered exactly-once. Two variants:
- **(a) the interface only:** truthful, no saving;
- **(b) also move the identity** to the tevents that need it: it touches every stored taggregate's tevents and the
  migrations, and saves JAStudio about 18 MB of garbage per start.

**R8. The snapshot takes the taggregate's id instead of a marker** (Fable 7): `LoadFromSnapshot(TaggregateId id, IEnumerable<…> snapshot)`.
- **Why the marker goes:** Compze used it only to take the id and assert the first tevent's position, so a new public
  interface buys nothing.
- **JAStudio's reasons are still met.** Its loaded tevent isn't a creation tevent, it may keep a marker of its own, and
  completeness can still be checked, since `LoadFromSnapshot` knows which tevents are the snapshot.
- **Unchanged:** version 1, protected, and the slot adopting.
- This reopens a settled point on a new fact, so JAStudio's representative is told.

**R9. The wrapper tevent implementation is declared in the table** (Fable 7): one declaration per class instead of two
overrides.

**R10. No tevent handlers in components in the final design** (both). The prototype has them, and they aren't carried
over.

**R11. `Commit` asserts it isn't called inside an unfinished cascade** (Fable 8). Today a tevent handler that commits hands
over tevents whose invariants then fail.

**R12. The order of work** (Fable 8):
1. **The bug fixes:** the queue, with `TeventStream` fed first and the hook after; `Commit` taking its list; the exception
   rule; `Commit`'s assertion.
2. **Routing per class:** the table on the shared matching code, root and components, static slots with the safeguards,
   and inheritance through `Extend` with the exact-class assertion.
3. **Loading from a snapshot,** with slots adopting, plus the thread assertion and the lazy lists. JAStudio starts here.
4. **The rest of Compze's teventives:** entities, removable entities and shared tentities designed and moved; the query
   models decided; per-instance registration removed.
5. **`ITaggregateTevent`,** once designed.

**R13. Two tevent-store bugs, by reading** (Opus 7, Fable 8):
- `Save` adds the taggregate to `_idMap` only after `Commit`, so a handler that `Get`s it during the save loads a second
  instance, and `Save`'s own add then throws on the duplicate key.
- With `Commit`'s fix, a tevent published from inside `Save`'s commit callback stays uncommitted for good, and is never
  stored.

Both go into the bug fixes of step 1.

**R14. Split Magnus's decisions from work that needs none** (Opus 6).
- **Decisions:**
  1. the go-ahead and the order;
  2. routing per class;
  3. declared or by convention, where the attribute variant has the metadata-order flaw too;
  4. the names;
  5. the exception rule;
  6. whether a store handler may change the taggregate it handles;
  7. the `ITaggregateTevent` variants, once designed;
  8. the generator;
  9. the query models;
  10. wrapper-free loading, later.
- **Work that needs no decision:** the validator's scans, the `WrapIn` closure, `Compze.Core.slnx`, the stale doc
  comment, the README sentence, the specifications of the eight behaviours, and the store bugs.

**R15. Corrections of detail:**
- "without the second" should say the first;
- the registration count: 33 registrations in 28 files of tests and sample, all mechanical, and 13 files in `src`, 7 of
  them the query-model family;
- the prototype's API isn't the designed one: its `LoadFromSnapshot` is public, with a version;
- the bugs aren't marked at the code, because of Magnus's rule against changing code while this is design work.

## The reviewers' replies

Both replies agreed with most resolutions as written. Where they changed or added, and how it was settled:

- **R1, the thread assertion:** the Fable reviewer showed that releasing the claim before the hand-over gives up most of
  what JAStudio asked for. JAStudio's hand-over (saving a file, dispatching to subscribers) is the long part of a
  publish, so its race, a background reparse against an edit in the UI, would almost always fall in the uncovered
  window, with the queue shared unguarded. The Opus reviewer proposed releasing only around each call out to an observer
  or the hook, which narrows the window but not past the hook. **Settled:** R1 hangs on decision 6, whether a tevent
  store participation handler may change the taggregate whose tevent it handles. Both answers are stated with their
  costs:
  - **May not** (recommended): the claim covers the whole of `Publish`, the tevent store asserts against the re-entry
    itself, and JAStudio gets the full assertion. Nothing works this way today, since bug 1 breaks it.
  - **May:** the claim is released around each call out to an observer or the hook, and held while the queue is taken
    from; JAStudio's race is then mostly uncovered.
  - Flow-keying stays rejected by both.
- **R3, the exception rule:** both added precisions, all taken.
  - The refusal mark is set where an exception escapes, at any nesting level, by a catch that rethrows or a finally on a
    success flag. The prototype's "incomplete until the end" flag is cleared before the hand-over; stretched to cover the
    hand-over, it would refuse JAStudio's own publishes from inside the hook. That is why the mark is set on escape.
  - Checks that run before anything changes, the thread claim and the refusal itself, don't set it.
  - Only what escapes counts: a hook that catches inside itself sets JAStudio's policy for a failing subscriber.
  - Reads aren't refused, though what they read may be half applied.
  - A load that throws sets the same mark.
- **R4:** the part's check is per class, or per slot, not per instance, since the parent's field doesn't hold the part
  while the part's constructor runs. Two components of one class with one entry forgotten aren't caught. `Extend` can skip
  an intermediate class that has a table: named, not solved.
- **R6:** the verbs are dropped by YAGNI on a new type, not because Compze's own call sites don't use them, which the rule
  on public types forbids as an argument. Dropping them from taggregates changes public API, so it goes on Magnus's list.
  `IgnoreAllUnhandled` becomes `IgnoreUnhandled` of the root tevent.
- **R7:** under either variant, a hierarchy that forgets its delivery declaration silently stops being delivered remotely
  (`UnitOfWorkTeventPublisher.RemoteDeliveryFor` returns null), with no compile-time failure. Variant (a) is simplest as
  `ITaggregateTevent : ITevent, ITessageWithIdentity`, which the tevent store needs nothing new for.
- **R8:** agreed by both. The prototype's assertions stay: the snapshot isn't empty, and no tevent carries another id.
  JAStudio's representative is told, since it reverses a point it settled.
- **R9 is dropped,** on the Opus reviewer's point: there are two tables, and which publisher a class is isn't tevent
  routing. Today's virtual `WrapperTeventImplementation` stays, its default from the generic argument meaning a root needs
  nothing. One per-class declaration holding appliers, handlers and the wrapper is an option for the design.
- **R12, the order:**
  - The performance specification comes with step 2, in Release, naming its configuration and leaving headroom.
  - The published `src/Compze.Teventive/Taggregates/_docs/reusable-components.md` is rewritten with step 2.
  - Between steps 3 and 4 both styles are public, with the rule that one tree is all one style. The scaffolding's bytes,
    roughly 13 to 25 MB for JAStudio by the two reviewers' estimates, and the abstract property can only land with step
    4. Stated, or step 4's removal pulled forward: Magnus's choice.
- **R13:** the two tevent-store bugs are one fix, and it hangs on decision 6. If a handler may change the taggregate,
  `Save` must track and subscribe before `Commit`, and publish what it saves through one path in order, with the queue's
  discipline. If not, the fix is the store's assertion.
- **R14:** the store bugs move under decision 6. Three changes to public API are listed for Magnus to veto: the verbs
  dropped from taggregates, the snapshot's shape, and the thread assertion as a change of behaviour.
- **Also taken:**
  - The thread assertion catches only overlapping publishes, never a reader against a publisher.
  - An entity class runs the validator's scans 6 to 9 times, not 3.
  - The snapshot's adopted tevents don't need static slots, since today's slot object could adopt too, though nothing
    waits for an early snapshot anyway.
  - JAStudio's shared README repeats "15 to 40 times", and needs R5 as well as R8.

## Outcome

**Both reviewers agree with the settlement above** (2026-11-16), with two small additions, both taken:
- the Opus reviewer: under "may not", a handler that changes the taggregate from another thread trips the thread
  assertion before the tevent store's own check, so the assertion's message names that case too;
- the Fable reviewer: the wording of the prototype's flag in R3, corrected above.

The shared conclusions were then changed to match. R1 to R15 stand as proposed where the replies didn't change them.
