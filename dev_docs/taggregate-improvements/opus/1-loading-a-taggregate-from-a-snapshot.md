# Loading a taggregate from a snapshot, without a tevent store

Proposed 2026-11-16 and agreed by both sessions. Not started.

## Why

JAStudio stores each note's state, not its history, and restores all 80,000 notes at startup. A taggregate can only
come into being by publishing its creation tevent, or by `ITaggregate.LoadFromHistory` from a tevent store's history.

taggregates-opus tested the pattern that fits: a creation tevent that implements every property-updated tevent of the
note, applied through `LoadFromHistory`. That runs the appliers only, with no tevent handlers, no `TeventStream` and
nothing left uncommitted. So loading and editing share the appliers, as in the docs' pattern of
`IUserRegistered : IUserEmailPropertyUpdated`.

It works only after setting the tevent's version through `IMutableTaggregateTevent.SetTaggregateVersionInternal`,
which is `[Obsolete]` for internal use. Otherwise `Version` stays 0, and the next `Publish` throws "The first published
tevent ... did not implement ITaggregateCreatedTevent".

## The concept: a snapshot

Event sourcing has a name for this: a snapshot, which is a taggregate's state as of version N, restored without the
history that led to it, and possibly followed by tevents N+1 onward. JAStudio's case is a snapshot with no tevents
after it. A tevent store could use the same feature to load long histories faster.

## The change

`ITaggregate.LoadFromSnapshot(ITaggregateTevent snapshot, int version)`:

- asserts that `Version` is 0;
- asserts that the tevent implements the snapshot marker (below);
- wraps the tevent in the taggregate's own wrapper tevent, as `Publish` does, since the caller shouldn't need to know
  the wrapper;
- sets the tevent's `TaggregateVersion` to `version`;
- runs the appliers only;
- asserts the invariants.

### A snapshot marker of its own, not `ITaggregateCreatedTevent`

A restored note wasn't created, and Compze reads `ITaggregateCreatedTevent` as creation: `Taggregate.Publish` requires
it of the first tevent, and `SingleTaggregateQueryModelGenerator` and `SelfGeneratingQueryModel` take the query
model's id from it. JAStudio will have both kinds:

- a creation tevent, published when a note is made in the UI or imported;
- a snapshot tevent, applied only through `LoadFromSnapshot`.

Both establish the taggregate's identity, so `Taggregate.ApplyTevent` sets `Id` for either marker. The name is
Magnus's call: `ITaggregateSnapshotTevent` or `ITaggregateRestoredTevent`.

### What `Version` means for a taggregate whose history isn't kept

The number of tevents the taggregate has had since its creation, with the snapshot standing in for the first N.

- **JAStudio passes 1,** so `Version` means "tevents since loaded". It doesn't store the version in a note's JSON. Its
  "changed since saved" is already in the taggregate, as the tevents not yet committed: it saves and commits on each
  publish cascade, through the [per-cascade hook](3-told-once-per-publish-cascade.md). A stored version would change with
  every edit, which is noise in the corpus diffs Magnus reviews. It would also change the corpus format, which is
  Magnus's decision.
- **A tevent store passes the snapshot's version,** so later tevents continue the numbering.

## Rejected

- **Making `LoadFromHistory` number unversioned tevents itself.** In a store's history, a tevent without a version is a
  bug in the stored data, and numbering it would hide the bug.
- **Publishing the snapshot tevent as a creation tevent.** That runs the tevent handlers, emits on `TeventStream` and
  leaves the tevent uncommitted, for something that isn't happening in the domain.

## What it touches

`ITaggregate` and `Taggregate` in Compze.Teventive, plus a new marker interface beside `ITaggregateCreatedTevent`.
Specifications: a snapshot restores state through the same appliers that editing uses; the next publish gets the
snapshot's version + 1; tevent handlers and `TeventStream` observers see nothing of the load; and loading twice, or
loading a tevent without the marker, fails.
