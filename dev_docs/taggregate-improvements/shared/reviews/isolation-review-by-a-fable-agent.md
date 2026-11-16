# The store's isolation: review by an independent Fable agent

2026-11-16. Magnus asked whether the recommended isolation, (b) "read uncommitted", was the best design for Compze and
for JAStudio, or a choice that minimized JAStudio's refactoring, and asked for subagents with no prior bias to answer. This
Fable subagent was given the three options neutrally, without the teams' recommendation, and told not to read either
team's documents. Recorded verbatim. Magnus then chose (a).

---

**Private copy per unit of work, option (a), is the best design for both Compze and JAStudio.** Shared live instances, option (b), is the only option whose appeal comes mainly from minimizing the consumer's refactoring. One store, one isolation.

## For Compze: private copy per unit of work (a)

1. **Compze's stated guarantees hold by construction.** A committed instance never changes, so any thread reads one consistent version without a lock. Shared live instances break what the code already promises:
   - `Taggregate.Publish` allows a temporarily invalid state until the publish cascade settles. Other threads would see it.
   - `UnitOfWorkTeventPublisher` says "Observation observes committed facts only". An observer reading the live taggregate sees the next unit of work's uncommitted state.
   - It is weaker than a database's read uncommitted: an applier changing a list while another thread enumerates it is a torn read.
2. **Domain methods are correct as naturally written.** `if(condition) Publish(...)` inside a taggregate method runs on a locked private copy. With shared live instances, a method called on a held instance takes the lock at its first change, after the condition was read. The taggregate's author can't fix that, so every caller must remember to get for update first. A unit of work can also be long for consumers Compze can't see: async, with a database in the same transaction.
3. **It is the tevent store's model.** `TeventStoreUpdater` already gives each unit of work a private instance, so "the same programming model" is true in substance. Building a new instance from a snapshot runs on every update, so a forgotten field fails the first test that edits. Restoring in place runs only on rollback, so its mistakes stay latent.

Versioned state (c) gives the same isolation plus stable identity. I rank it second, and not for its size:
- Every read resolves through the ambient transaction, so two reads can straddle a commit unless the reader remembers to take the state once. A private copy is that consistent state.
- Its natural form separates state values from behavior, against the object-oriented standard.
- The tevent store's taggregates would need the same redesign to keep one model.

**One addition:** make the handle a first-class typed taggregate-reference: the stable object standing for one taggregate's identity, where an instance is one version of its state. Lookups return taggregate-references, and reading and getting for update go through them. Types then carry "don't hold an instance across units of work". A stale instance is at worst old and consistent, and throws on publish.

## For JAStudio: also (a)

- **It completes JAStudio's own principle.** `NoteCacheBase` and `NoteIndex` promise that a lookup sees the cache "as it was before a change or after it, never in the middle of one", by changing copies and publishing them. The notes those lookups return are still changed in place: `VocabNote.SetReadings` clears and refills a list any other thread may be copying. Shared live instances keeps that gap.
- **Notes are already identified by id, not by object.** `JPNote.Equals` compares ids, and `ReloadFromBackend` replaces every object.
- **It forces a modeling fix.** `_cardStatus` and `AnkiTags` are Anki's state, not stored with the note, riding on the note object. A copy through the snapshot drops them, so they need a home of their own. Shared live instances would keep that hidden.

**Refactoring:** for JAStudio, the appeal of shared live instances is mainly that `vocab.Question.SetText("x")` on a held object, `HashSet<TNote>` and the view models keep working. Its one real merit, lookups that see the edit's own changes for free, exists only because nothing is isolated. Under Magnus's standard the cost (every edit site, holder and specification) should not change the answer. The measured cost per edit is irrelevant at JAStudio's edit rate.

## If the two answers differ

They don't. Were (b) chosen for JAStudio anyway, I would not make it a second isolation of the same store. It changes the implementor's contract, the derived-state contract and what every reader may assume, so it would be two stores behind one name. One isolation, with the consumer adapting.

## What I am unsure of

- **Derived state is the unproven part** of (a), and of (c). Handlers that run at prepare can't serve a lookup made earlier in the same unit of work, which `PerfectSynonyms` and renaming a vocab appear to need. I would define an index as a function of taggregate state, as `NoteIndex`'s `keysOf(snapshot)` already is, overlaid at lookup with the unit of work's own copies. Maintaining it by handlers makes their order a rule. If the overlay can't be made cheap, the choice needs another look.
- **Copy cost was measured on a small note.** Measure a sentence with its parsing result, across a full rebuild.
- **The prototype commits a unit of work's taggregates one at a time,** so readers can briefly see half of a multi-taggregate change. Whether to promise atomic visibility is open.
- **I ran nothing.** The numbers are as given, and the code facts are from reading.
- **What would change my answer:**
  - Strictly single-threaded reads and writes would make (b) defensible, for JAStudio only.
  - A requirement to hold parts such as `vocab.Question` long-term, as for binding a UI to them, would point to (c), designed properly, not to (b).

Files read:
- `C:\Dev\Compze\src\Compze.Teventive\Taggregates\BaseClasses\Taggregate..cs`
- `C:\Dev\Compze\src\Compze.Teventive.TeventStore\_private\TeventStoreUpdater.cs`
- `C:\Dev\Compze\src\Compze.Tessaging\TessageBus\_private\UnitOfWorkTeventPublisher.cs`
- `C:\Dev\Compze.worktrees\taggregate-store-opus\src\Compze.Teventive.TeventStore\TaggregateStore\_private\TaggregateStoreUpdater.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\JPNote.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Collection\NoteIndex.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Collection\NoteCacheBase.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Vocabulary\RelatedVocab\PerfectSynonyms.cs`
