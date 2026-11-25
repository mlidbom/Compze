>[!NOTE]
> Taggregate specific nested components and entities are fully supported already. We've already [shown examples](tevent-naming.md) of how tevents for them look. You just nest component/entity tevent interfaces within your root tevent interface. Simple. What we are discussing on this page is how to create tevent based components and entities that can be used in multiple different tevent based taggregates without code duplication. That is another ballgame.

# Reusable tevent based components and entities
So as in the note above, the question is how we go about it when there is some sort of component or entity that we want to be able to reuse in multiple different taggregates without code duplication. We would consider any approach to modeling that did not support this without breaking a sweat to be badly limited.

Since it is a shared, reusable component:
* It cannot publish taggregate tevents, that must be up to the taggregate.
* We must be able to subscribe to the published tevents by type, meaning that the tevent ultimately published by the taggregate must declaratively, statically, contain the type of the tevent from the component.
* One taggregate may hold several of the same component, so the taggregate, and its subscribers, must be able to tell them apart.

Thankfully the solution to [inheriting taggregates](taggregate-inheritance.md) has taken us most of the way to a solution for this problem too. We will just need to wrap one more time, generic covariance to the rescue once more.

Compze calls such a component a **shared tomponent**: a teventive component, which publishes tevents and applies them to its own state, that is tied to no particular taggregate. The taggregate that holds it, or the component of a taggregate that does, is its **owner**. And the owner's class declares a **slot** for each shared tomponent it holds.

Let's build one: a price, which products and bundles of products both have. A product has two of them, a list price and a sale price.

## The shared tomponent's own tevents
A shared tomponent's tevents are rooted in a tevent interface of its own, which implements nothing but `ITevent`. They carry no taggregate id and say nothing of which taggregate they happen in. Only the owner knows that.

[!code-csharp[](reusable-components.cs#IPriceTevent)]

Something must implement the interfaces. The classes mirror them:

[!code-csharp[](reusable-components.cs#PriceTevent)]

## The shared tomponent
[!code-csharp[](reusable-components.cs#Price)]

* It derives from `SharedTomponent<IPriceTevent>`, typed on its own tevent root.
* Its state changes only in its tevent appliers, just as a taggregate's does. `ChangeTo` doesn't set `Amount`: it publishes a tevent, and the applier of that tevent sets `Amount`, when the tevent is published and again whenever the taggregate's history is replayed.
* Its appliers are declared once for the class, with `DeclareAppliers`, kept in a static field, and handed to the base class by overriding the protected `TeventAppliers` property. More on that [below](#routing-tevents-back-to-the-shared-tomponent).
* Its constructor takes a `SharedTomponentSlotInAnOwner<IPriceTevent>`: the slot it is in, in its owner, which the owner hands it, as we'll see. That is how `Publish` knows where the tevent goes.
* It knows nothing of products, or of bundles.

## The owner's tevents adopt the shared tomponent's
The product's tevents are those of any taggregate: a root tevent interface, and a wrapper interface for it, as on the [taggregate inheritance](taggregate-inheritance.md) page. We'll come to `IRepriced` in a moment.

[!code-csharp[](reusable-components.cs#IProductTevent)]

[!code-csharp[](reusable-components.cs#ProductTevent)]

Now the new part. For each slot, the owner declares an **adopting wrapper tevent**: a tevent of the owner that wraps a tevent of the shared tomponent.

[!code-csharp[](reusable-components.cs#IProductPriceTevents)]

Read `IProductListPriceTevent<out T>` like this: it is a product tevent, here one that means the product was repriced, and it is an `IPublisherTevent<T>`, the [covariant wrapper](definition.md) of a price tevent. So it says three things at once: what happened, which is `T`; which taggregate it happened in, a product; and in which member of the product, the list price. The two slots wrap the very same price tevents: only their adopting wrapper tevents tell them apart.

Each adopting wrapper tevent has a class implementing it:

[!code-csharp[](reusable-components.cs#ProductPriceTevents)]

When the list price changes, the product publishes an `IProductTevent<IProductListPriceTevent<IPriceTevent.IChanged>>`. That is the price's tevent, adopted by the list price slot, and then wrapped, as every tevent a product publishes is, in the wrapper that says which class of product published it. Two wrappings, each covariant.

## The slot the owner's class declares
[!code-csharp[](reusable-components.cs#ProductSlots)]

A `SharedTomponentSlot` holds what is the same for every product:
* The class implementing the slot's adopting wrapper tevent, as an open generic type. The slot closes it over each tevent's own type, so the published tevent carries the full type of what happened.
* How to get the shared tomponent in this slot from a product.

Its five type arguments are the owner's class, the owner's tevent root and the class implementing it, the shared tomponent's tevent root, and the adopting wrapper tevent closed over that root.

The slots are static: declared once, shared by every product, and costing a product nothing. Keep them in a nested static class, such as `Slots`. The runtime then creates them before they are first used, wherever the routing that uses them is written. Declared beside the routing in the owner's class itself, a slot would have to be written above it, since a class's static fields are initialized in the order written.

The product constructs each price with its slot in this product: `Slots.ListPrice.In(this)`.

[!code-csharp[](reusable-components.cs#ProductConstructor)]

An owner can also be a component of a taggregate. A slot in a component is declared, constructed with, and routed exactly as in a taggregate.

## Routing tevents back to the shared tomponent
[!code-csharp[](reusable-components.cs#ProductRouting)]

Every teventive class, whether a taggregate, a component or a shared tomponent, declares its **tevent routing** once: which tevent applier each tevent reaches. `DeclareAppliers` declares it, and the class keeps it in a static field and returns it from its override of the protected `TeventAppliers` property. Every instance of the class shares it.

* `For<T>` routes every tevent assignable to `T` to an applier. The applier is a `static` lambda that gets the instance as its first parameter: the routing is shared by every instance, so it can hold no state of its own.
* `SharedTomponent(slot)` routes every tevent in that slot's adopting wrapper tevent to the shared tomponent in that slot, which applies it through its own routing.
* A tevent reaches every route it matches, one after another, in the order they were declared. A class that inherits from another extends its base class's routing with `Extend`, and the base class's routes come first. So an applier can rely on those declared before it having applied the same tevent.
* A tevent that reaches no applier is an error, unless the routing lets it go with `IgnoreUnhandled`.

So when the list price changes,

[!code-csharp[](reusable-components.cs#ChangeTheListPrice)]

the list price slot wraps the change in a `ProductListPriceTevent<>`, the product publishes that as its own tevent, the product's routing routes it through the list price slot back to the list price, and the list price's applier sets its `Amount`. The sale price never sees it.

The mistakes that are easy to make are loud:
* A class that doesn't override `TeventAppliers` doesn't compile. A subclass that inherits its base class's routing, instead of extending it, is refused when it is first constructed.
* A slot handed a class that doesn't implement its adopting wrapper tevent, such as another slot's, throws when the slot is made.
* A shared tomponent constructed with another slot than the one whose member it is kept in throws when it first publishes, and when its owner's history is loaded.

## Subscribing to a shared tomponent's tevents
To follow the list price of every product, subscribe to the whole published tevent:

[!code-csharp[](reusable-components.cs#SubscribeToTheListPrice)]

The handler gets the published tevent as it stands: `changed.Tevent` is the adopting wrapper tevent, a product tevent carrying the product's id, and `changed.Tevent.Tevent` is the price's own tevent.

The adopting wrapper tevent is a tevent of the owner, so it can say what the change means to the owner. Both price slots' adopting wrapper tevents are `IProductTevent.IRepriced`, so this subscriber hears of every change to either price, and of nothing else:

[!code-csharp[](reusable-components.cs#SubscribeToRepricing)]

In the same way, a subscriber to `IProductTevent` gets every adopting wrapper tevent along with the product's other tevents.

And since every adopting wrapper tevent is an `IPublisherTevent<>`, a subscriber can follow a shared tomponent's tevents in every slot of every owner without knowing any of them. This one hears of every price change, in products and bundles alike:

[!code-csharp[](reusable-components.cs#SubscribeToEveryPrice)]

>[!WARNING]
> A subscription sees one level of wrapping. A subscription to a type that is no wrapper is matched against what the published wrapper wraps, and a subscription to a wrapper type is matched against the published wrapper itself. A shared tomponent's tevent is wrapped twice, so:
> * A subscriber to `IPriceTevent.IChanged` receives nothing: what the product's wrapper wraps is the adopting wrapper tevent, not the change.
> * A subscriber to `IProductListPriceTevent<IPriceTevent.IChanged>` receives nothing either: it is a wrapper type, and the published wrapper is the product's. Wrap it once more, and it receives the list price's changes: `IPublisherTevent<IProductListPriceTevent<IPriceTevent.IChanged>>`.

## Reusable entities: shared tentities
A **shared tentity** is to an entity what a shared tomponent is to a component: a teventive entity, with an id of its own, written once, which any taggregate can keep a collection of. Reviews are an example. Every tevent of a review carries, as ordinary domain data, the id of the review it belongs to: its `EntityId`.

[!code-csharp[](reusable-components.cs#IReviewTevent)]

[!code-csharp[](reusable-components.cs#ReviewTevent)]

[!code-csharp[](reusable-components.cs#Review)]

`ReviewId` is a small value type of the example's own, around a `Guid`.

* A review derives from `SharedTentity<Review, ReviewId, IReviewTevent, IReviewTevent.IWritten>`: its own class, its id, its tevent root, and the tevent that creates one.
* Its routing starts with an applier of the base class's, which gives a new review the `EntityId` of its created tevent as its `Id`. The review's own applier of `IWritten` comes after it.
* A review publishes tevents that carry its own `Id`, and `Publish` asserts that they do.
* The owner keeps its reviews in a `SharedTentityCollection`, which is itself a shared tomponent: it takes one slot, and every review in it publishes through that slot's adopting wrapper tevent. The collection creates a review when a created tevent arrives, and routes every other tevent to the review whose id it carries. A review is added by publishing its created tevent, with `AddByPublishing`, which returns the new review.

## Start to finish: a second owner
A bundle of products has a price too, and reviews. Here is all of it:

[!code-csharp[](reusable-components.cs#Bundle)]

The price and the review are written once, each:
1. Its tevent interfaces, rooted in an interface of its own that implements `ITevent`, or `ISharedTentityTevent<TId>` for a shared tentity, and the classes implementing them.
2. Its class, deriving from `SharedTomponent<>` or `SharedTentity<,,,>`, with its tevent routing.

Each owner then writes, for each shared tomponent it holds:
1. An adopting wrapper tevent: an interface that is a tevent of the owner and an `IPublisherTevent<T>` of the shared tomponent's tevents, and a class implementing it.
2. A slot, in its nested `Slots` class, holding that class and how to get the shared tomponent from an owner.
3. The slot, entered in its tevent routing: `.SharedTomponent(Slots.Price)`.
4. The shared tomponent, constructed with the slot in this owner: `new Price(Slots.Price.In(this))`.
