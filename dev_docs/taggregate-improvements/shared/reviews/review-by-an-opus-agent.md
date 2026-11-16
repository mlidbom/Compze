# Review by an independent Opus agent

2026-11-16. An Opus subagent that took no part in building the conclusions, asked by jastudio-rep-opus to review them
critically and independently. Recorded verbatim, as the agent returned it. Its experiments are on its branch
`review-taggregate-improvements` (commit `861474504`), in `C:\Dev\Compze\.claude\worktrees\agent-a5042660ba8c7b64c`.

---

## Review of the shared conclusions on taggregates for JAStudio's notes

### 1. Verdict

The core of the conclusions holds. Tevent routing decided once per class is the right abstraction, and it is what fixes the memory. Both bugs are real. The numbers add up. But the documents claim more is designed and proven than the prototypes show. Two parts of the publish-cascade design would break the tevent store, Compze's other user of taggregates. What most needs to change:

- **The one-thread assertion** must not cover handing tevents to the hook and the `TeventStream` observers.
- **The hook** must not be what feeds `TeventStream`.
- **The routing tables lack what the design promises:** the exact-class assertion, today's verbs and rules, removable entities and shared tentities are not built.
- **The `ITaggregateTevent` change** is misdescribed.
- **The README overstates the speed-up.**

### 2. Findings, most important first

**1. The one-thread assertion refuses a legitimate publish that Compze's own tevent store produces.** (Changes a recommendation.)
- **The problem:** in the prototype, the thread is claimed for all of `Publish`, including handing tevents to the hook and the observers (`Taggregate..cs:82` covers the hand-over at `:108`; `OneThreadAtATime.cs:29-31`).
- **Why the store hits it:**
  - `TeventStoreUpdater.OnTaggregateTevent` hands each tevent to the bus with `PublishAsync(...).GetAwaiter().GetResult()`.
  - In-process handlers run in the caller's unit of work (`UnitOfWorkTeventPublisher.PublishCoreAsync`, `TessageHandlerExecutor.ExecuteTeventHandlers`).
  - So a handler gets the same scoped session, and the same taggregate instance.
  - A handler that awaits real I/O resumes on a thread-pool thread while the publishing thread waits for it. Its publish on that taggregate is then refused.
- **Evidence:** my specification `editing_it_throws_that_a_taggregate_is_used_by_one_thread_at_a_time` passes, i.e. the edit throws.
- **Why no test caught it:** bug 1 makes this pattern fail on `main` today, so no test uses it.
- **The documents should say:**
  - the claim covers applying, the tevent handlers and the invariants, and is released before the hand-over (simplest, and safe here because the waiting thread is blocked);
  - or it is keyed on the flow of execution, which allocates once per outermost call;
  - either way, it is Magnus's decision.

**2. The hook's base implementation is what feeds `TeventStream`.** (Changes a recommendation.)
- **Evidence:** in the prototype, `OnPublishCascadeEnded` emits to `_teventStream` (`Taggregate..cs:172-176`). My specification `the_tevent_stream_emits_nothing` passes for a class whose override doesn't call the base.
- **Why it matters:** the tevent store's session saves every tevent after the first `Save` through `TeventStream`. An override like JAStudio's `JPNote` that forgets the base call would silently stop those saves.
- **Fix:** `Taggregate` feeds `TeventStream` itself, then calls an empty protected virtual hook.

**3. The per-class design's key claims are not in any prototype.** (Changes the order of work, and limits what "full suite passes" proves.)
- **No exact-class assertion:** `TeventRouting.Dispatch` just casts with `(TInstance)instance` (`TeventRouting.cs:60-65`), so a subclass instance passes.
- **"Today's verbs stay, matching implemented once and shared with `IMutableTeventDispatcher`" isn't so:**
  - The declaration has only `For`, `Component`, `Entities`, `SharedTomponent` and `IgnoreUnhandled`.
  - It re-implements matching (`TeventRoute.Matches`, `MayGoUnhandled`) beside `Subscriptions.MatchingHandlersFor`. That is two implementations of one abstraction.
  - Its `For` is `where TTevent : ITevent`. Today's `For` is constrained to the class's own tevents (`where THandledTevent : TTevent`), so that compile-time check is lost.
  - The prototype's `For` also skips `TessageTypeInspector.AssertValidForSubscription`, the check that only tevent interfaces are subscribed to.
  - The design's own snippet, `TeventRouting<VocabNote>`, can't express the constraint. The table needs the tevent root as a type parameter.
- **What taggregates actually use today:** only `For` and `IgnoreUnhandled` (plus the slot's internal `ForWrapped`). `BeforeHandlers`/`AfterHandlers` have no taggregate user, and `ForGenericTevent` is used only in `SingleTaggregateQueryModelGenerator`.
  - So whether the tables carry those verbs is a decision.
  - Building the shared matching engine is the real first increment of step 3.
- **Removable entities throw under declared routing:**
  - `TeventiveRemovableEntity.CollectionManager`'s constructor is unchanged. It calls `parent.RegisterTeventAppliersInternal()`, which asserts the parent declares no table.
  - The collection's `DispatchInternal` never removes an entity.
  - Established by reading, not run.
- **`SharedTentity` and `SharedTentityCollection` are in neither the design nor any prototype.**
  - Both keep their dispatchers per instance (`SharedTentity.cs:30`).
  - The collection only accepts a slot object per instance.
  - The last increment, removing per-instance registration, can't happen without designing them.
- **The prototype adds what the design rejects:** it routes tevent handlers into components (`DispatchInternal(…, toTeventHandlers)`). That should be dropped.

**4. The `ITaggregateTevent : ITevent` change is misdescribed.** (Changes how decision 6 is framed.)
- **The store needs a tevent identity, not exactly-once delivery:** it uses the `TessageId` as each stored tevent's identity.
  - When reading tevents back: `TeventStore.cs:109`.
  - In migrations: `PersistMigrations.cs:181-203`.
  - In `TeventDataRow`.
  - So it needs `ITessageWithIdentity` even for a taggregate whose tevents never leave the process.
- **The stated saving needs a bigger change:** "saves a note 32 B and a Guid" requires removing `Id` from `TaggregateTevent`. That touches every stored taggregate's tevent classes and the migrations' replacement tevents, not "about five hierarchies, one line each".
- **The saving is garbage, not kept memory:** about 18 MB at each start.
- **Decision 6 should offer two variants:** change the interface only, which saves nothing measurable; or also move the identity, which is a bigger change.

**5. The README overstates.** (Corrections of detail.)
- **"About 15 to 40 times faster":** the documents' own measurement of a vocab with slots in the real base classes is 36.6 → 4.8 µs, about 7.6×. The full spread is about 8× to 45×. JAStudio's README repeats the "15 to 40".
- **"Without the second, it isn't [possible]":**
  - The second change is the snapshot.
  - The opus team's own snapshot document says loading works today through `LoadFromHistory` plus the `[Obsolete]` internal version setter.
  - The blocker is memory, which is the first change.
- **Startup garbage isn't stated:** the real-base-class load allocates about 830 B per loaded tevent, about 0.45 GB of garbage per start. That beats today, but it is the largest remaining cost.

**6. The list of what Magnus decides.**
- **Items that are ordinary work, not his decisions:** the validator scans (item 9), the `WrapIn` closure fix, `Compze.Core.slnx:59`, the doc comment naming a class that doesn't exist, and the specifications (item 8).
- **Missing items:**
  - findings 1, 2 and 4;
  - which verbs the tables carry;
  - shared tentities.
- **Decision 5 is incomplete:** refusing further use means JAStudio must replace every reference to a refused note (indexes, view models) with a reloaded one.
- **The attribute-marked alternative is presented as free of the convention's defects, but it isn't:** several matching appliers in one class would still be ordered by metadata order, unless the attribute carries an order.

**7. A side finding in the tevent store, by reading, not run.**
- `TeventStoreUpdater.Save` adds the taggregate to `_idMap` only after `Commit` (`:119`), and the commit callback already runs the in-process handlers.
- A handler that `Get`s the new taggregate during the save loads a second instance and adds it to `_idMap`. `Save`'s own `_idMap.Add` then throws on the duplicate key.
- So bug 2 isn't reachable through the store on the same instance; this bug is reached instead.

**8. Minor points.**
- **Bugs not marked:** the two bugs are on `main` with no `bug:` comment and no failing specification.
- **The registration count:** "56 registrations in 41 files of tests and sample" includes about 13 in the tevent store's query-model base classes, which aren't all mechanical (a component registering on a subscriber passed in).
- **The prototype isn't the designed API:** its snapshot load is the public `ITaggregate.LoadFromSnapshot(snapshot, version)`, not the designed protected, unversioned one.

### 3. What I checked and found right

- **Both bugs, by reading:** `Taggregate..cs:76-81` and `:125-129`. Bug 1 is reachable through the tevent store, as stated.
- **Smaller findings confirmed:**
  - the closure in `WrapIn` (`Constructor..cs:32-35`);
  - three assembly scans per static constructor; an entity class runs two or three of those constructors, so it gets 6 to 9 scans;
  - a dispatcher configuration built per instance (`TeventiveRemovableEntity.cs:278`);
  - the README's "entirely negligible" sentence;
  - the broken `.slnx` line.
- **Static field order:** the compiler does warn (CS8604), even inside the declaration lambda. I ran it.
- **The arithmetic:** 0.6 to 1.1 GB today, about 27 MB with the prototypes.
- **The plain-object baselines** exclude shared strings and parent references, so the overheads are stated conservatively.
- **The queue is needed:** JAStudio's subscribers change the same note, and the tevent store needs the order kept.
- **The routing choices hold:** the virtual property beats a constructor argument, which fails under multi-level inheritance too. The reasons for rejecting the convention and lazy dispatchers hold.
- **The snapshot's marker and its version 1** are reasoned soundly, from facts about JAStudio.

### 4. My experiments

- **Branch `review-taggregate-improvements`,** commit `861474504`, on `fb75d60a0`. Not pushed.
  - It adds `test/Compze.Tests.Unit/CQRS/Taggregates/SnapshotsAndPublishCascades/Review_findings_specification.cs`.
  - Its two specifications pass, which demonstrates findings 1 and 2.
  - I ran only those two.
- **The compiler-warning check,** not committed: `C:\Users\magnu\AppData\Local\Temp\claude\2eb3c61e8403f425\8637568c-7b58-4dd1-b55c-939a38c15f2a\scratchpad\staticorder\`.

### 5. Questions that would change my verdict

1. Does Compze support an in-process handler changing the taggregate whose tevent it is handling, in the same unit of work? If not, the store can forbid it, and finding 1 shrinks.
2. Should the routing tables carry `BeforeHandlers`, `AfterHandlers` and `ForGenericTevent`? If not, the shared matching engine is much smaller.
3. Is the `TessageId` saving wanted, or is the `ITaggregateTevent` change only for truthfulness?
