# Review by an independent Fable agent

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../the-naming-pass.md](../the-naming-pass.md) and [end-review-consolidated.md](end-review-consolidated.md).

2026-11-16. A Fable subagent that took no part in building the conclusions, asked by jastudio-rep-opus to review them
critically and independently. Recorded verbatim, as the agent returned it. Its experiments are on its branch
`review-taggregate-conclusions` (commit `a440911a7`), in `C:\Dev\Compze\.claude\worktrees\agent-ad538039c7b0a5f4e`.

---

# Review of the shared taggregate conclusions

The opus benchmark rerun against a Release build had finished before the interruptions, and its numbers are in finding 3.

## 1. Headline verdict

**The direction is sound, but four things need to change before Magnus decides.** Routing per class is the right model, both bugs are real, and every kept-bytes number I reran reproduces exactly.

1. **The declared table has silent mistakes, and the snapshot is what makes them silent.** Three of them lose data on JAStudio's load-then-save path.
2. **An exception from the hook or an observer doesn't leave what the design says it leaves.**
3. **Times and allocated bytes were measured against Debug builds of Compze.Teventive.** "15 to 40 times faster" doesn't describe JAStudio's shape, which is about 10 times.
4. **What goes beyond JAStudio's needs is unprototyped but presented as mechanical:** removable entities, shared tentities, the query models, three of the verbs, and `ITaggregateTevent`.

Paths below are relative to two roots unless given in full:
- **Prototype:** `C:\Dev\Compze.worktrees\taggregate-experiments-fable-static-slots\src\Compze.Teventive\`
- **Main:** `C:\Dev\Compze\src\`

## 2. Findings, most important first

### Finding 1: silent mistakes in the declared table

The case for the declared table over convention is that mistakes are loud. A snapshot tevent implements every property-updated tevent of the root and its components, so it always matches some route. "An unhandled tevent is an error" therefore never fires on a load.

- **A forgotten `.Component(…)` entry (ran):** the vocab loads from its snapshot without an exception, and the component has 0 of its 2 forms. The next save loses them. Publishing through the component is loud (`TeventUnhandledException`).
- **Two shared tomponents constructed with each other's slots (ran):** `Synonyms.Add` leaves Synonyms with 0 words and Antonyms with 1, with no exception. Today's slot object can't be mismatched this way, because the tomponent attaches to the slot it is constructed with. The static slot has two declarations that must agree.
- **A subclass that forgets to override `TeventAppliers` (ran):** its applier ran 0 times, with no exception. The exact-class assertion the design relies on is in no prototype (prototype `TeventRouting.cs:60-65`).
- **A subclass that overrides but starts from `DeclareAppliers` instead of `Extend` (by reading):** the base class's appliers are silently skipped for a snapshot tevent. `DeclareAppliers` is a public static for any class.
- **`For<TTevent>` accepts any `ITevent` (compiles, never runs):** prototype `TeventRouting.cs:87`. Today's `For` is constrained to the class's own tevents (main `Compze.Teventive\ITeventSubscriber.cs:14`).

What the conclusions should say:
- A part asserts, when constructed, that its parent's table has an entry for it.
- Route-back through a slot asserts that the tomponent it reaches was constructed with that slot.
- A class's first table comes only from a protected static factory on the Compze base class, typed on the class's own type parameter, and a subclass's only from `Extend`. The fable standalone prototype has this shape.
- The table is typed on the class's tevent type too, so `For` keeps today's constraint.
- The exact-class assertion is prototyped before the virtual property is chosen, and the property is abstract.

**This adds to the design. It doesn't change the recommendation to declare.**

### Finding 2: an exception from the hook or an observer

- **The claim:** `the-design.md:174-175` says the taggregate stays usable "with the tevents not yet handed over still queued, in order".
- **What I ran:** a cascade of Edited v2 and Reacted v3, with an observer that throws on Edited. The observer was told of Edited v2 and later Marked v4. Reacted v3 was never handed on.
- **Why:** the prototype dequeues the cascade before the call (prototype `Taggregate..cs:123`).
- **What it costs:** a tevent store session that lived on would hold a history missing version 3. JAStudio's subscribers would miss a tevent while the note carries on. If the hook commits first, as JAStudio's does, the tevent is gone from the uncommitted list too.
- **Also:** cascades queued behind the failed one wait for the next publish, whenever that is.
- **The hook's base call:** the hook is virtual and its base implementation feeds `TeventStream` (prototype `Taggregate..cs:172-176`). An override that omits `base` silently stops the stream, and with it a tevent store's saving. The base class should feed the stream itself and then call an empty hook.

What the conclusions should say: either one rule, that any exception out of a cascade or its hand-over makes the taggregate refuse further use, or a plain statement that the cascade in hand is dropped. **This changes the scope of decision 5.**

### Finding 3: the measurements

Both teams' programs reference `bin\Debug` of Compze.Teventive:
- `C:\Dev\Compze.worktrees\taggregate-experiments-fable-static-slots\experiments\taggregate-improvements-fable\SlotsOfAClassMeasured\SlotsOfAClassMeasured.csproj:11`
- `C:\Dev\Compze\dev_docs\taggregate-improvements\opus\benchmark\README.md:62`

Kept bytes reproduce exactly in both builds: 350, 9,952, 832 and 640 B, and 733 and 1,088 B for the opus shape.

| Slot shape, load of one vocab | Stated (Debug) | Release, three runs |
|---|---:|---:|
| Allocated, slot object per instance | 4,192 B | 3,416 B |
| Allocated, slots of the class | 4,800 B | 3,770 to 3,800 B |
| Allocated, registered per instance | 22,104 B | 20,027 to 20,266 B |
| Publish through a slot, declared | 1,032 B | 688 B |
| Time, registered per instance | 36.6 µs | 33 to 38 µs |
| Time, declared | 4.4 to 4.8 µs | 3.1 to 4.0 µs |

- **Slot shape, the nearest to JAStudio's:** 9 to 12 times faster in Release, at 5 to 18% other load. The teams' own Debug row gives 8 times.
- **Opus shape, one tevent through 15 components:** 2.98 to 3.08 s against 0.04 s for 80,000 notes in Release, about 75 times. This was one run of three rounds, at 2 to 3% other load.
- **`README.md:27` says "15 to 40 times".** That fits neither shape. It should name the shape and the build.
- **"Bytes are exact" holds for kept bytes only.** Allocated bytes depend on the build and vary between Release runs, so the performance specification's bound must name its configuration and leave headroom.
- **"About 30 MB"** is extrapolated from the standalone prototype's per-teventive bytes. My arithmetic from JAStudio's part counts gives about 27 MB.

**Correction of detail. The conclusion stands.**

### Finding 4: beyond JAStudio's needs, unproven and called mechanical

- **Compze's own base classes:** `Extend` covers a consumer's inheritance. Nothing says how `Tentity`, `TeventiveRemovableEntity`, `SharedTentity` and `SharedTentityCollection` add their own appliers and `IgnoreUnhandled` to a subclass's table.
- **Removable entities (by reading, not run):** under a declaring parent one throws at construction. Prototype `Taggregates\BaseClasses\TeventiveRemovableEntity.cs:36-48` registers on the parent, whose `RegisterTeventAppliers` asserts.
- **Shared tentities** are untouched in every prototype.
- **The query models:** a parallel hierarchy in `Compze.Teventive.TeventStore\QueryModels\` with `HandlesTevent`, which the table has no counterpart for. It isn't prototyped, and JAStudio doesn't need it. It should be a decision of its own, not a step.
- **The count in `README.md:63`:** "56 registrations in 41 files of Compze's tests and sample" is all of `src`, `test` and `samples`. Tests and sample hold 33 in 28 files. The other 13 files are in `src`, 7 of them the query-model family. The 33 are mechanical: I read the root and nested registrations, and all sit in constructors.
- **`BeforeHandlers`, `AfterHandlers` and `ForGenericTevent`:** no taggregate, component or entity in the repository uses them. "Matching rules implemented once, shared with `IMutableTeventDispatcher`" is in no prototype (prototype `_private\TeventRoute.cs` duplicates them). The tables could start with what taggregates use.

**This changes the order of work and the decision list.**

### Finding 5: the thread assertion against the tevent store's blocking bridge

- **What I ran:** an observer that publishes on the taggregate from the publishing thread is fine. From another thread, while the publishing thread waits for it, it throws `InvalidOperationException`.
- **Where the tevent store does this:** main `Compze.Teventive.TeventStore\_private\TeventStoreUpdater.cs:134` blocks on `PublishAsync`. Main `Compze.Tessaging\Engine\_private\TessageHandlerExecutor.cs:38-39` awaits each handler, so handlers can continue on pool threads meanwhile.
- **Consequence:** a handler that changes the same taggregate, which is the scenario of bug 1, works through the queue on one thread and fails the assertion on another.
- **Limit of this evidence:** I showed the mechanism. I didn't run it through the real store.
- **Overstatement:** `the-design.md:172-173` says the assertion "turns a silent corruption into a loud failure". It catches only two threads inside `Publish` or `Commit` at the same moment.

What the conclusions should say: name the case, decide it, and pin it with a specification through the tevent store. **A risk to name. It may change the assertion.**

### Finding 6: `ITaggregateTevent` as a plain `ITevent`

- **The tevent store identifies every stored tevent by its `TessageId`:** main `Compze.Teventive.TeventStore\TeventStore.cs:109`, `TeventStore.PersistMigrations.cs:133-153`, and `Compze.Teventive.TeventStore.Abstractions\_internal\SqlLayer\Abstractions\TeventDataRow.cs:14`.
- **So every stored hierarchy needs an id,** not only the "about five of 24" delivered between endpoints.
- **The two claims can't both hold.** Either `TaggregateTevent` keeps its `Id`, and the "32 B and a Guid's creation" saving isn't there, or every stored hierarchy declares identity. The design says neither.

**This needs design before it is a decision. The direction stands.**

### Finding 7: could be simpler

- **The snapshot marker:** Compze uses it only to take the id from the first tevent and to assert its position (prototype `Taggregate..cs:196` and `266-270`). `LoadFromSnapshot(TaggregateId id, …)` needs no new public interface, and JAStudio can keep a marker of its own. The marker is new public API and is missing from Magnus's list.
- **`WrapperTeventImplementation`** could live in the table: one declaration per class instead of two overrides.
- **Tevent handlers routed through components** are in the prototype, though the design says they aren't wanted. Don't carry them over.

### Finding 8: smaller corrections

- **`README.md:25-26`** says "without the second, it isn't", then argues memory, which is the first change.
- **Bug 2 and the tevent store:** `TeventStoreUpdater.Save`'s callback is the only `Commit` a session makes (`TeventStoreUpdater.cs:98-121`). With the fix, a tevent published from inside it stays uncommitted for good and is still never stored.
- **`Commit` from inside a tevent handler (ran):** it hands over tevents whose invariants then fail (prototype `Taggregate..cs:243`). It should be asserted against.
- **Order of work:** bug 1's fix is the queue, so step 1 holds part of step 2. The snapshot's adopted tevents need the static slots of step 3, and nothing waits for an early snapshot, so routing with slots should come first.
- **The attribute way** has the same metadata-order flaw the convention was rejected for. `the-design.md` lists only its upsides.

## 3. What I checked and found right

- **Both bugs:** the fable team's `ClaimsCheckedByRunning`, run against 0.6.0-alpha, reproduces the duplicate notification, the stack overflow (0xC00000FD) and the tevent lost in `Commit`.
- **The queue's order argument,** by reading. It also helps when there are several observers.
- **Static initialization order:** the compiler warns (CS8604) inside the lambda and across partial files, in a small program I ran. I found no initialization cycles.
- **The shared tables are safe for several threads,** by reading.
- **`Compze.Core.slnx` fails with MSB3202** (ran).
- **By reading:** the doc comment naming `SingleTaggregateQueryModelUpdater`, the validator's three scans per class, and the `WrapIn` closure.
- **Deskmancer** uses only `IMutableTeventDispatcher`, which stays as it is.
- **The scaffolding bytes:** 24 + 32 + 64 = 120 B, by counting fields.
- **Refusal after a failed cascade** is the right call.

Not checked:
- the full suite on any prototype branch;
- the "Passed!" after a host crash;
- the convention prototype;
- the root README's "negligible" sentence, which I searched for but didn't weigh against the claim.

## 4. My experiments

- **Branch and commit:** `review-taggregate-conclusions`, commit `a440911a7`, on `fb75d60a0`, in `C:\Dev\Compze\.claude\worktrees\agent-ad538039c7b0a5f4e`. Not pushed.
- **`ReviewChecks`:** `C:\Dev\Compze\.claude\worktrees\agent-ad538039c7b0a5f4e\experiments\review-of-the-shared-conclusions\ReviewChecks\Program.cs`. Seven checks against a Release build of the static-slots prototype, cited above as "ran".
- **`SlotsOfAClassMeasured.csproj`** now takes `-p:CompzeConfiguration`, Debug by default.
- **The opus benchmark against Release** ran from my scratchpad and is not committed.

## 5. Questions that would change my verdict

1. **Does JAStudio's hook commit before it dispatches, and may a note refuse use after a subscriber throws?** If not, the cascade in hand needs a defined fate (finding 2).
2. **May a tevent store's participation handler change the taggregate whose tevent it handles?** If yes, the thread assertion must change. If no, bug 1 isn't a tevent-store scenario either.
3. **Will anything backed by a tevent store ever load from a snapshot?** If never, the id parameter replaces the marker.
4. **Is per-instance registration removed outright, or kept for the query models?** That decides whether they block step 3.
5. **Does JAStudio have subclasses of concrete teventive classes that add no appliers?** The exact-class assertion makes each declare an empty table.
