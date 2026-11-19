# Told once per publish cascade

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../shared/the-naming-pass.md](../shared/the-naming-pass.md) and
> [../shared/reviews/end-review-consolidated.md](../shared/reviews/end-review-consolidated.md).

Proposed 2026-11-16 and agreed by both sessions. Not started.

A coined term, used throughout: a **publish cascade** is one call to `Taggregate.Publish` together with every `Publish`
that tevent handlers make from inside it. `Taggregate` asserts the invariants only at the end of a publish cascade, and
only then emits the cascade's tevents on `TeventStream`.

## Why

JAStudio wants to know when a note changed, and how: the whole publish cascade, after the invariants are asserted. Then
it can refile the note in its indexes, commit, save the note's file, and dispatch the tevents across the corpus, each
once.

`TeventStream` gives the tevents, but one at a time, so an observer can't tell where a publish cascade ends. That gap is
the main reason for the change. Memory is a lesser one: once [tevent routing is per class](2-tevent-routing-decided-once-per-class.md),
a `TeventStream` subscription costs about 100 to 150 B per note, or about 10 MB for the corpus. (`SimpleObservable` is
copy-on-write: an array, an observer and a disposable.)

## The change

- **A protected virtual hook in `Taggregate`,** called once at the end of each publish cascade, after the invariants are
  asserted, with a copy of the publish cascade's wrapped tevents.
- **Its base implementation emits them on `TeventStream`,** which is created lazily, so a taggregate that nobody
  observes carries no observable.
- **JAStudio's `JPNote` overrides the hook,** and forwards to the services it can reach.

There's no static state, which matters because specifications run several JAStudios in one process, and nothing per
instance.

## Rejected

- **A sink passed at construction:** a field per note. It also doesn't fit the tevent store, which creates taggregates
  through their default constructor.
- **A subscription to each note's `TeventStream`:** it doesn't give the end of a publish cascade, and costs a
  subscription per note.

## The reentrancy bug in `Taggregate.Publish`

Found by taggregates-opus, and reproduced with Compze.Teventive 0.6.0-alpha on 2026-11-16. This document is its only
record: the work is design for now, with no change to the code, so the `bug:` comment goes at the code when the fix is
made, or before if Magnus wants it there now.

At the end of a publish cascade, `Publish` enumerates
`_teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers` with a `foreach`, calls
`_teventStream.OnNext` for each, and then clears the list. When an observer publishes on the same taggregate from
inside `OnNext`:

1. The inner `Publish` adds its tevent to the list that the outer `foreach` is enumerating.
2. The inner `Publish` is at the end of a publish cascade itself, so it emits the outer cascade's tevents a second
   time, followed by its own.
3. The outer `foreach` then throws `InvalidOperationException` ("Collection was modified"), out of the domain method
   that published, after the state has changed.

In [the reproduction](checks-against-0.6.0-alpha/README.md), the observer of a rename republished on the same note. It
saw `Renamed, Renamed, Updated`, and the rename threw.

JAStudio will do exactly this: a subscriber across the corpus that reacts to a vocab's change may update the vocab's
generated data, such as its sentence counts after a reparse.

**The fix is part of the hook's design:** copy the list, clear it, then call the hook with the copy. A publish from
inside the hook then starts a publish cascade of its own, with its own call to the hook. A specification pins it:
an observer that publishes on the same taggregate sees each tevent once, in order, and nothing throws.

## The same bug in `ITaggregate.Commit`

Found by jastudio-rep-opus while checking that the hook may commit, and reproduced with 0.6.0-alpha on 2026-11-16 by
[a check](checks-against-0.6.0-alpha/README.md). It's more serious than the bug in `Publish`, because one form of it
loses a tevent without a word.

`Commit` hands its callback the live list of uncommitted tevents, and clears the list after the callback returns. A
publish on the same taggregate from inside the callback, such as a subscriber reacting while the callback dispatches
the tevents, then goes one of two ways:

- **If the callback is enumerating the tevents,** the publish adds to the list under the enumeration, and the callback
  throws `InvalidOperationException`.
- **If it isn't,** the publish's tevent is added to the list, and the `Clear` after the callback removes it. It's never
  handed to any commit. In the check, the note ended at version 3 with the name the inner publish gave it, and the
  next `Commit` handed over nothing. A tevent store would then hold a history without version 3.

**The fix has the same shape:** `Commit` copies the list, clears it, then calls the callback with the copy. A publish
from inside the callback leaves its tevents uncommitted, for a later `Commit`.

Rejected: deferring a `Commit` made inside a commit callback until the outer callback returns, then committing in a
loop. It would hand tevents to a callback other than the one their own `Commit` passed.

## Committing from inside the hook

JAStudio's override of the hook commits there: it refiles the note, calls `ITaggregate.Commit`, saves the note's file,
and dispatches the tevents across the corpus. With both fixes, that is allowed and specified:

- `Commit` may be called from inside the hook.
- A publish the hook makes after committing, or that a subscriber makes while the hook dispatches, is a publish cascade
  of its own. Its tevents stay uncommitted until its own call to the hook commits them.
- **Each tevent is handed to exactly one commit callback, and the callbacks are entered in the order of the tevents.**
  A nested publish's callback can finish before the outer callback does, so a callback that persists should persist
  before it does anything that can publish.

A specification pins it: commit in the hook, publish again from inside the hook, and every tevent is committed exactly
once, in order.

## Asserting that one taggregate is used by one thread at a time

A taggregate isn't thread-safe, and isn't meant to be. But today, two threads publishing on one instance at the same
time corrupt `_reentrancyLevel` and the lists without a word. JAStudio could do it: a full rebuild, on background
threads, updates a vocab's generated data while the UI thread edits the same vocab. Today the editors are disabled
during a reparse, by hand, for exactly that reason. (taggregates-opus raised it on 2026-11-16.)

The assertion turns that silent corruption into a loud failure, as the standard on defensive coding asks, at little
cost:

- When a publish cascade starts (reentrancy level 0 to 1), record the publishing thread with
  `Interlocked.CompareExchange` on a field, and throw if another thread holds it. When the cascade ends, release it.
  The compare-exchange makes the check reliable when two threads start a publish cascade at the same moment, which a
  plain read and write wouldn't be.
- `Commit`, `LoadFromHistory` and `LoadFromSnapshot` take the same field.
- **One at a time, not one thread for ever.** A note loaded on one thread and edited later on the UI thread is fine.
  `Publish` is synchronous, so a publish cascade never moves to another thread midway.
- **Cost:** a field per instance, and one compare-exchange per publish cascade.

Compze's existing usage guards (`IUsageGuard`, `SingleTransactionUsageGuard` in Compze.Internals.SystemCE) don't fit:
each is an object per guarded instance, and none means "one thread at a time".

## What it touches

`Taggregate` in Compze.Teventive. The tevent store's `TeventStoreUpdater` subscribes to `TeventStream` per tracked
taggregate, and commits in `Save`. It keeps working unchanged.
