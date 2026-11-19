# Later: the smaller changes, and those JAStudio doesn't need yet

Agreed on 2026-11-16 by both sessions to come after the first three. None is needed for JAStudio's first slice.

## `ITaggregateTevent` claims a delivery guarantee it doesn't have

`ITaggregateTevent` extends `IExactlyOnceTevent`. That claims remote, transactional, exactly-once delivery, which a
taggregate's tevents have only when a tevent store and the outbox carry them, and a note's in-memory tevents never do.
Each tevent also gets a `TessageId`, which exists only for exactly-once delivery.

Decision D6, in `src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md`, says a tevent's interfaces
choose its delivery guarantees. Consistent with it: `ITaggregateTevent` extends only `ITevent`, and the tevent roots of
store-persisted taggregates add `IExactlyOnceTevent` themselves. It touches `TaggregateTevent`'s `Id`, the tevent store
and the bus.

It costs JAStudio nothing in function. With rare edits, a Guid and a timestamp per tevent don't matter.

## The README's claim of negligible overhead

The repository's root `README.md`, line 20, says: "In memory performance overhead is entirely negligible in the great
majority of systems." That's true per dispatch, not per instance held. Reword it to say "per dispatch". Once tevent
routing is per class, it's nearly true per instance too.

## A source generator for tevent classes

The tevent class hierarchy mirrors the tevent interface hierarchy mechanically, so a generator could write the classes
from the interfaces. A big saving for a vocab with 15 components. A separate effort.

## Subscribing to a shared tomponent's inner tevents across every owner: possible today, no new routing needed

The question was whether routing needs a subscription kind that unwraps all the way: decision D5 unwraps one wrapper
level at delivery, and a shared tomponent's tevent arrives wrapped twice, first in its slot's adopting wrapper tevent,
then in the owner's wrapper. It doesn't. Two ways already work, checked against 0.6.0-alpha on 2026-11-16 with
[a check](checks-against-0.6.0-alpha/README.md) of an order with two address slots and an unrelated warehouse with one,
dispatched into one corpus-wide `IMutableTeventDispatcher<ITevent>`:

- **A subscription to a wrapper of a wrapper,** found by jastudio-rep-opus:
  `ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent>>>`. By covariance it matches every slot of
  every owner, with no declaration on any owner, and it can choose inner tevent types, as
  `IPublisherTevent<IPublisherTevent<IPostalAddressTevent.Changed>>` does. The handler gets the owner's wrapper. Its
  `Tevent` is the adopting wrapper, which says which slot, and that one's `Tevent` is the tomponent's tevent.
- **A marker on the adopting wrappers,** proposed by taggregates-opus. Each owner's adopting wrapper interfaces also
  implement a marker, and subscribers subscribe to that, such as `ForGenericTevent<IAddressAdopted>`. It needs a
  declaration on each owner, and covers every tevent of the slot, not chosen ones. A marker within the owner's own
  hierarchy works the same, so JAStudio's note types could share one under `JPNote`'s tevent root.

The first is the general one. The type is long-winded, so at most a convenience method could come later, such as one
that subscribes to a tomponent's tevent type and delivers the inner tevent. That's ergonomics, not routing.

## A marker interface for property-updated tevents

Property-updated tevents, such as `IUserEmailPropertyUpdated`, are a naming pattern today. A marker interface of their
own would let the per-class tables check, once per class, that a snapshot tevent implements every property-updated
tevent the class's appliers handle. That would catch the silent data loss described in
[loading from a snapshot](1-loading-a-taggregate-from-a-snapshot.md#a-gap-a-snapshot-can-leave-silently). Worth it
only if the marker earns its place for other reasons too: the consumer's round-trip specification catches the same
loss.
