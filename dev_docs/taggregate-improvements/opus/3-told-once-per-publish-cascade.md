# Told once per publish cascade

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

Found by taggregates-opus, and reproduced with Compze.Teventive 0.6.0-alpha on 2026-11-16. A `bug:` comment marks it at
the code.

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

## What it touches

`Taggregate` in Compze.Teventive. The tevent store's `TeventStoreUpdater` subscribes to `TeventStream` per tracked
taggregate, and keeps working unchanged.
