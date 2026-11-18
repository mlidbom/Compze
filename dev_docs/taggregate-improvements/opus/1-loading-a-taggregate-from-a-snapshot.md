# Loading a taggregate from a snapshot, without a tevent store

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../shared/the-naming-pass.md](../shared/the-naming-pass.md) and
> [../shared/reviews/end-review-consolidated.md](../shared/reviews/end-review-consolidated.md).

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

**A snapshot is the shortest history that produces the state,** which isn't always one tevent. (taggregates-opus found
this on 2026-11-16, and the code confirms it.) Every part of the taggregate then restores through its own appliers, the
ones editing uses:

- **The taggregate and its owned components: one tevent.** A component's tevents are subtypes of its owner's tevent
  root, so one snapshot tevent can implement every property-updated tevent of the taggregate and of every component,
  and the routing reaches them all.
- **An entity collection: one tevent per entity.** `Tentity.CollectionManager` creates one entity for each
  `TEntityCreatedTevent`, with the id that `IdGetterSetter.GetId` reads from that tevent, so one tevent can create one
  entity at most. Each entity's snapshot tevent implements its created tevent, carrying that entity's state. Nothing in
  Compze reads an entity's created tevent as more than "add this entity to its collection", so it needs no marker of its
  own. Nested entities follow the same way.
- **A shared-tomponent slot: the tomponent's own snapshot, wrapped in the slot's adopting wrapper tevent.**
  `SharedTomponentSlot` routes to its tomponent only what is wrapped in its adopting wrapper tevent. An owner's snapshot
  tevent can't stand in for that. It would have to be the adopting wrapper itself, and two slots of the same tomponent
  type, such as a shipping and a billing `PostalAddress`, close `IPublisherTevent<T>` over the same inner type. They then
  share the one `Tevent` property, which can't return a different inner snapshot for each slot. A
  `SharedTentityCollection` in a slot needs one wrapped created tevent per tentity.

## The change

`ITaggregate.LoadFromSnapshot(IEnumerable<ITaggregateTevent> snapshot, int version)`:

- asserts that `Version` is 0;
- asserts that the first tevent implements the snapshot marker (below), and that no other tevent does;
- wraps each tevent in the taggregate's own wrapper tevent, as `Publish` does, since the caller shouldn't need to know
  the wrapper. A shared tomponent's snapshot tevents arrive already wrapped in their slot's adopting wrapper tevent, as
  the slot wraps what its tomponent publishes;
- sets every tevent's `TaggregateVersion` to `version`;
- runs the appliers only;
- asserts the invariants once, after the last tevent.

**Every snapshot tevent gets the one version,** not 1 to k. The snapshot as a whole stands in for the first N tevents of
the history, so `Version` after the load is `version`. How many tevents a snapshot needs depends on the shape of the
state, not on its history, so numbering them separately would mean nothing.

**Making a shared tomponent's snapshot tevent** today means constructing the slot's adopting wrapper implementation
around the tomponent's tevent, such as `new ShippingAddressTevent<IPostalAddressTevent>(snapshotOfTheAddress)`. Whether
the slot should offer that is a detail to settle when implementing.

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

- **JAStudio passes 1,** so `Version` minus one is the number of tevents since the note was loaded, and a `Version`
  above 1 means the note changed since it was loaded. JAStudio doesn't store the version in a note's JSON. Its
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
- **Restoring entity collections and shared tomponents from state objects,** outside the appliers. That brings back two
  code paths for state, one for loading and one for editing, which is what a snapshot exists to avoid.

## A gap a snapshot can leave silently

A property-updated tevent added later, which the snapshot tevent doesn't implement, leaves that property at its default
on load, without an error. The next save then loses the data.

- **The natural safety net is the consumer's:** a specification that loads, saves and compares.
- **Compze could check it once per class,** with the per-class tables: every property-updated tevent the class's appliers
  handle must be implemented by the snapshot tevent. That needs property-updated tevents to be recognizable, by a marker
  interface of their own. Today they are a naming pattern only. Later, and only if such a marker earns its place for
  other reasons too: see [later](4-later.md).

## What it touches

`ITaggregate` and `Taggregate` in Compze.Teventive, plus a new marker interface beside `ITaggregateCreatedTevent`.
Specifications: a snapshot restores state through the same appliers that editing uses, including an entity collection
of several entities and two slots of the same shared-tomponent type; the next publish gets the snapshot's version + 1;
tevent handlers and `TeventStream` observers see nothing of the load; and loading twice, loading a snapshot whose first
tevent lacks the marker, or one with the marker on a later tevent, fails.
