# The store's isolation: review by an independent Opus agent

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../the-naming-pass.md](../the-naming-pass.md) and [end-review-consolidated.md](end-review-consolidated.md).

2026-11-16. Magnus asked whether the recommended isolation, (b) "read uncommitted", was the best design for Compze and
for JAStudio, or a choice that minimized JAStudio's refactoring, and asked for subagents with no prior bias to answer. This
Opus subagent was given the three options neutrally, without the teams' recommendation, and told not to read either team's
documents. Recorded verbatim. Magnus then chose (a).

---

**Headline:** option (a), a private copy per unit of work, is the best design for both Compze and JAStudio, with two amendments:
- **Handles as the way to hold a taggregate:** a handle the store keeps is how anything holds a taggregate across units of work.
- **Derived state that sees the unit of work's own changes:** derived state answers inside a unit of work from the committed state plus that unit of work's copies, instead of being kept up by participation handlers.

Since the two answers agree, it should be one store with one isolation. Option (b)'s appeal comes almost entirely from sparing JAStudio a refactoring.

## For Compze: (a)

1. **It is the tevent store's programming model.** `TeventStoreUpdater` loads each taggregate into an instance private to its session. "The same programming model" therefore means (a). Under (b), the same `Get` would return a private instance from one store and a shared live one from the other.
2. **It is correct by construction where (b) is correct by rules.** Under (a), readers can't see uncommitted or rolled-back state, and a rollback just discards the copy, so there is no restore code to get wrong. (b) needs rules from three groups of people, and a published library can't make consumers it can't see remember them:
   - **Writers** must get a taggregate for update before reading the state they decide from.
   - **Taggregate authors** must restore in place correctly: reset every lazily cached value, and restore entity collections that the unit of work added to or removed from while keeping each part's identity.
   - **Consumers of derived state** must handle notifications that changed nothing, and rollback lists.
3. **It enforces "any change outside a transaction is a bug" structurally.** The committed instance throws on publish; the prototype does this through `ITaggregateStoreInternals.MakeReadOnly`. And (a)'s snapshot type is already (c)'s immutable state value, so (a) is a step toward (c), while (b), with its mutable shared instances, is a step away.

**Is any appeal mainly refactoring avoidance?** Yes, (b)'s. It keeps object references, and edit sites keep calling methods on the object they hold. Its only other gain is skipping a copy that costs about 1 µs. (c) is held back partly by its refactoring cost, which is no reason, and partly by having no design yet, which is a real reason.

## For JAStudio: (a)

1. **Today's lack of isolation is a defect.** Readers already see a rebuild's half-done notes. (b) would make that the specified behaviour; (a) removes it.
2. **(b)'s restore rule would fail in JAStudio's own code.** `VocabNoteForms` holds seven `LazyCE` caches, and `_notOwnedByOtherVocab` is computed from other vocab through the cache. Under (b), a reader that computes it while another vocab is uncommitted keeps that value after the other vocab rolls back. Restoring that vocab in place resets its own caches, not this one's. Under (a) this can't happen.
3. **The replacement model already exists.** `ReloadFromBackend` replaces every note object, and holders follow its notification. Under (a) the same thing happens per note at each commit.

**What JAStudio would have to change:**
- **Holders hold handles.** It matters for `NoteIndex` in particular. Its sets are `HashSet<TNote>` with id equality, so if they held instances, every commit would need a Remove and an Add in every set holding the note, and each touched set is copied, thousands of sentences for a common word. With handles, a commit touches no set unless the note's keys change.
- **Edit sites get the note for update.** The 51 `guard.Update`/`_guard.Update` wrappers (I counted them) become publishes, and their callers get the note for update inside a unit of work.
- **Cross-note changes leave the domain call.** These become participation handlers, which is a better design in its own right:
  - `PerfectSynonyms` pushing an answer to the other synonyms.
  - `VocabNoteQuestion` calling `RenameTheWordReferences`, which runs over every vocab.

  The rename must also first find the vocab that refer to the old name, then get only those for update. Copying every vocab would be wasteful.
- **Specifications** change their edit style.

Under Magnus's standard the refactoring cost should not change this answer. The measured cost of 8.5 to 12.5 µs per edit is invisible for edits made from the UI.

**The one real functional point in (b)'s favour** is reading derived state partway through an edit. `PerfectSynonyms` reads notes it has already changed. Since dispatch waits for the prepare phase, an index kept up by participation handlers is updated too late for reads inside the domain call; (b)'s notification after each settled publish cascade gets this right.

My fix under (a) is a unit-of-work-aware index: an index that, inside a unit of work, answers a lookup from the committed sets, minus the ids that unit of work holds copies of, plus those copies whose current keys match.
- It is correct at any moment, and needs no ordering between handlers.
- Handles give the same read-your-own-writes for the notes' own state.
- Compze should provide it: every consumer of (a) that keeps in-memory indexes will need it.

## If the answers differed

They don't. Even if they did, I would not offer two isolations: every consumer and every taggregate author would then have to know which one is in force, which is exactly the kind of rule to remember that the standard excludes.

## What I'm unsure of

- **The unit-of-work-aware index is my proposal, not prototyped.** Recomputing the keys of a few copies per lookup is cheap. For a batch holding thousands of copies in one unit of work, I don't know.
- **Copy cost and garbage are unmeasured.** The prototype measured a small note, not one with 25 parts. If the rebuild runs each note as its own unit of work, that is about 80,000 × 9.5 KB, roughly 760 MB of short-lived allocations, also unmeasured.
- **Reads of taggregates not gotten for update are read-committed.** A unit of work that reads a taggregate without getting it for update sees the latest committed state, which can change under it, so a decision based on that read isn't protected. It could be made correct by construction by recording the versions read and checking them at prepare. I don't know whether JAStudio has such decisions beyond `PerfectSynonyms.Accepts`.
- **Cross-note lazy caches go stale under every option.** `_notOwnedByOtherVocab` goes stale when another vocab commits whichever isolation is chosen. Isolation doesn't fix that, and I didn't check how JAStudio invalidates it today.

**What would change my answer:**
- If Compze must support taggregates too large to copy per unit of work, the answer becomes (c) with structurally shared state, not (b).
- If handles turn out to be pervasive and noisy in consumer code, that also points to (c), where the taggregate itself is the stable object.

Files I relied on:
- `C:\Dev\Compze\src\Compze.Teventive.TeventStore\_private\TeventStoreUpdater.cs`
- `C:\Dev\Compze.worktrees\taggregate-store-opus\src\Compze.Teventive.TeventStore\TaggregateStore\_private\TaggregateStoreUpdater.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Vocabulary\VocabNoteForms.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Collection\NoteIndex.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Vocabulary\RelatedVocab\PerfectSynonyms.cs`
- `C:\Dev\JAStudio.wt2\src\src_dotnet\JAStudio.Core\Note\Collection\JPCollection.cs`
