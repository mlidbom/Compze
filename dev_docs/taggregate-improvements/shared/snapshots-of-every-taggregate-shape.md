# Snapshots of taggregates of every shape: specified

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [the-naming-pass.md](the-naming-pass.md) and [reviews/end-review-consolidated.md](reviews/end-review-consolidated.md).

The bug Magnus filed on 2026-11-17: a taggregate with entities or shared tentities can't be restored from a snapshot, so
refactoring a part of a taggregate into entities makes it unusable with a state store. Magnus: "This is not really adding
API it is fixing a broken one." Fixed before the spike's end review, on his OK, so that the review judges the fixed API.
Every name is a placeholder for the naming pass.

## What is broken

1. **No way to restore an entity.** `RestoreFrom` sets a taggregate's state directly, its parts' included, and publishes
   nothing. `Tentity`'s collection and `SharedTentityCollection` add an entity only by publishing its created tevent.
2. **The round-trip check doesn't see entities.** `SnapshotRoundTrip.AssertLosesNothing` compares every field of the
   taggregate's own classes, and skips every type of Compze.Teventive's as machinery. An entity collection is one, so a
   `RestoreFrom` that loses every entity passes the check. A shared tentity collection is compared as a list of its
   tentities, but their ids are Compze's fields, so a wrong id passes too.

## The fix

- **`AddRestored(id)`, on `Tentity.ICollectionManager` (through `IEntityCollectionManager`) and on
  `SharedTentityCollection`:** creates the entity as a created tevent would, with its id, adds it to the collection, and
  returns it, publishing nothing. The caller, the parent's `RestoreFrom`, then gives the entity the rest of its state, as
  it does a component's. A removable entity's collection inherits it.
- **Only while the taggregate is being restored,** asserted: from `RestoreIdentity` until a taggregate store's restore
  returns from `RestoreFrom`, or, when a consumer's own code calls `RestoreFrom`, until its first publish; and never on a
  committed instance, which is read only. (The end review found the window too wide: a copy got for update was in it
  until it published. Closed in c68b42137.) Anything else would change an entity without a tevent. A part reaches its
  taggregate through its parents, so a collection in a component asks the component, which asks its parent.
- **The round-trip check compares entity collections as state:** each entity with its id, in creation order, whether the
  collection is `Tentity`'s or a shared tentity's. The entities' own fields are compared as any part's are.
- **Query models** are left as they are: none is snapshotable.

## Specifications

Snapshot round trips, through `SnapshotRoundTrip.AssertLosesNothing`, of the shapes Compze's specifications have:

- **the composite taggregate:** a component with a nested component, removable and non-removable entities in the
  component, and removable entities at the root with removable nested entities of their own, all with state;
- **the project with its checklist of shared tentities;**
- **an inheriting taggregate,** a cat whose class extends the animal's.

And:

- **a restore that leaves out an entity** fails the check, naming the collection's path;
- **`AddRestored` on a taggregate that isn't being restored** throws: on one created by publishing, and on a restored one
  after its first publish; and on a copy a store has restored, once its restore has returned.

The integer-id composite taggregate has the same shape as the Guid one, so it isn't specified again.

## Built (46cc881a6)

As specified, the full suite green (4,519 passing). Both failing checks name the collection's path, such as
`CompositeTaggregate._entities: 2 items in the original, 0 in the copy`. The one addition: the collections' routing hooks
became protected, which the inspections asked for. A consumer that doesn't use entities, such as JAStudio, is untouched.
