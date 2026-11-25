# Shared tentities
A [tentity](tentities.md) belongs to one taggregate. A *shared tentity* is to a tentity what a [shared tomponent](shared-tomponents.md) is to a tomponent: written once, with an id of its own, and kept in a collection by any number of different taggregates. It builds on shared tomponents, so read that page first: this one continues its example of products and their prices.

Reviews are an example. Every tevent of a review carries, as ordinary domain data, the id of the review it belongs to: its `EntityId`.

[!code-csharp[](shared-tomponents-and-tentities.cs#IReviewTevent)]

[!code-csharp[](shared-tomponents-and-tentities.cs#ReviewTevent)]

[!code-csharp[](shared-tomponents-and-tentities.cs#Review)]

`ReviewId` is a small value type of the example's own, around a `Guid`.

* A review derives from `SharedTentity<Review, ReviewId, IReviewTevent, IReviewTevent.IWritten>`: its own class, its id, its tevent root, and the tevent that creates one.
* Its routing starts with an applier of the base class's, which gives a new review the `EntityId` of its created tevent as its `Id`. The review's own applier of `IWritten` comes after it.
* A review publishes tevents that carry its own `Id`, and `Publish` asserts that they do.
* The owner keeps its reviews in a `SharedTentityCollection`, which is itself a shared tomponent: it takes one slot, and every review in it publishes through that slot's adopting wrapper tevent. The collection creates a review when a created tevent arrives, and routes every other tevent to the review whose id it carries. A review is added by publishing its created tevent, with `AddByPublishing`, which returns the new review.

## Start to finish: a second owner
A bundle of products has a price too, and reviews. Here is all of it:

[!code-csharp[](shared-tomponents-and-tentities.cs#Bundle)]

The price and the review are written once, each:
1. Its tevent interfaces, rooted in an interface of its own that implements `ITevent`, or `ISharedTentityTevent<TId>` for a shared tentity, and the classes implementing them.
2. Its class, deriving from `SharedTomponent<>` or `SharedTentity<,,,>`, with its tevent routing.

Each owner then writes, for each shared tomponent it holds:
1. An adopting wrapper tevent: an interface that is a tevent of the owner and an `IPublisherTevent<T>` of the shared tomponent's tevents, and a class implementing it.
2. A slot, in its nested `Slots` class, holding that class and how to get the shared tomponent from an owner.
3. The slot, entered in its tevent routing: `.SharedTomponent(Slots.Price)`.
4. The shared tomponent, constructed with the slot in this owner: `new Price(Slots.Price.In(this))`.
