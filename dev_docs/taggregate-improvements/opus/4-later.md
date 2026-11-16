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

## Subscribing to a shared tomponent's inner tevents across every owner

Routing unwraps one wrapper level at delivery, by decision D5. A shared tomponent's tevent arrives wrapped twice: in
the slot's adopting wrapper tevent, then in the owner's wrapper. So a subscriber who wants a shared tomponent's inner
tevent from every owner, such as every audio-list change in the corpus, needs a wrapper-of-a-wrapper subscription today.
A subscription kind that unwraps all the way would be new. Later, when JAStudio has a concrete case.
