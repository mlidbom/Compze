# Shared tomponents
A [tomponent](tomponents.md) belongs to one taggregate: its tevents are nested in that taggregate's tevent hierarchy. A *shared tomponent* is written once, and used in many different taggregates without duplicating its code. It publishes tevents and applies them to its own state, as a tomponent does, but is tied to no particular taggregate. The taggregate that holds it, or the tomponent of a taggregate that does, is its **owner**, and the owner's class declares a **slot** for each shared tomponent it holds.

Because it belongs to no particular taggregate:
* **Its tevents are its own,** rooted in an interface that belongs to no taggregate. The owner publishes them as its own tevents, by wrapping them.
* **Subscribers still subscribe by type.** The tevent the owner publishes contains, in its type, the type of the shared tomponent's tevent.
* **An owner can hold several of the same shared tomponent.** Each is in a slot of its own, and the tevent the owner publishes says which.

Each of these is done with a wrapper tevent, the mechanism of [taggregate inheritance](taggregate-inheritance.md), applied once more.

Let's build one: a price, which products and bundles of products both have. A product has two of them, a list price and a sale price.

## The shared tomponent's own tevents
A shared tomponent's tevents are rooted in a tevent interface of its own, which implements nothing but `ITevent`. They carry no taggregate id and say nothing of which taggregate they happen in. Only the owner knows that.

[!code-csharp[](shared-tomponents-and-tentities.cs#IPriceTevent)]

Something must implement the interfaces. The classes mirror them:

[!code-csharp[](shared-tomponents-and-tentities.cs#PriceTevent)]

## The shared tomponent
[!code-csharp[](shared-tomponents-and-tentities.cs#Price)]

* It derives from `SharedTomponent<IPriceTevent>`, typed on its own tevent root.
* Its state changes only in its tevent appliers, just as a taggregate's does. `ChangeTo` doesn't set `Amount`: it publishes a tevent, and the applier of that tevent sets `Amount`, when the tevent is published and again whenever the taggregate's history is replayed.
* Its appliers are declared once for the class, with `DeclareAppliers`, kept in a static field, and handed to the base class by overriding the protected `TeventAppliers` property. More on that [below](#routing-tevents-back-to-the-shared-tomponent).
* Its constructor takes a `SharedTomponentSlotInAnOwner<IPriceTevent>`: the slot it is in, in its owner, which the owner hands it, as we'll see. That is how `Publish` knows where the tevent goes.
* It knows nothing of products, or of bundles.

## The owner's tevents adopt the shared tomponent's
The product's tevents are those of any taggregate: a root tevent interface, and a wrapper interface for it, as on the [taggregate inheritance](taggregate-inheritance.md) page. We'll come to `IRepriced` in a moment.

[!code-csharp[](shared-tomponents-and-tentities.cs#IProductTevent)]

[!code-csharp[](shared-tomponents-and-tentities.cs#ProductTevent)]

Now the new part. For each slot, the owner declares an **adopting wrapper tevent**: a tevent of the owner that wraps a tevent of the shared tomponent.

[!code-csharp[](shared-tomponents-and-tentities.cs#IProductPriceTevents)]

Read `IProductListPriceTevent<out T>` like this: it is a product tevent, here one that means the product was repriced, and it is an `IPublisherTevent<T>`, the [covariant wrapper](definition.md) of a price tevent. So it says three things at once: what happened, which is `T`; which taggregate it happened in, a product; and in which member of the product, the list price. The two slots wrap the very same price tevents: only their adopting wrapper tevents tell them apart.

Each adopting wrapper tevent has a class implementing it:

[!code-csharp[](shared-tomponents-and-tentities.cs#ProductPriceTevents)]

When the list price changes, the product publishes an `IProductTevent<IProductListPriceTevent<IPriceTevent.IChanged>>`. That is the price's tevent, adopted by the list price slot, and then wrapped, as every tevent a product publishes is, in the wrapper that says which class of product published it. Two wrappings, each covariant.

## The slot the owner's class declares
[!code-csharp[](shared-tomponents-and-tentities.cs#ProductSlots)]

A `SharedTomponentSlot` holds what is the same for every product:
* The class implementing the slot's adopting wrapper tevent, as an open generic type. The slot closes it over each tevent's own type, so the published tevent carries the full type of what happened.
* How to get the shared tomponent in this slot from a product.

Its five type arguments are the owner's class, the owner's tevent root and the class implementing it, the shared tomponent's tevent root, and the adopting wrapper tevent closed over that root.

The slots are static: declared once, shared by every product, and costing a product nothing. Keep them in a nested static class, such as `Slots`. The runtime then creates them before they are first used, wherever the routing that uses them is written. Declared beside the routing in the owner's class itself, a slot would have to be written above it, since a class's static fields are initialized in the order written.

The product constructs each price with its slot in this product: `Slots.ListPrice.In(this)`.

[!code-csharp[](shared-tomponents-and-tentities.cs#ProductConstructor)]

An owner can also be a component of a taggregate. A slot in a component is declared, constructed with, and routed exactly as in a taggregate.

## Routing tevents back to the shared tomponent
[!code-csharp[](shared-tomponents-and-tentities.cs#ProductRouting)]

Every teventive class, whether a taggregate, a component or a shared tomponent, declares its **tevent routing** once: which tevent applier each tevent reaches. `DeclareAppliers` declares it, and the class keeps it in a static field and returns it from its override of the protected `TeventAppliers` property. Every instance of the class shares it.

* `For<T>` routes every tevent assignable to `T` to an applier. The applier is a `static` lambda that gets the instance as its first parameter: the routing is shared by every instance, so it can hold no state of its own.
* `SharedTomponent(slot)` routes every tevent in that slot's adopting wrapper tevent to the shared tomponent in that slot, which applies it through its own routing.
* A tevent reaches every route it matches, one after another, in the order they were declared. A class that inherits from another extends its base class's routing with `Extend`, and the base class's routes come first. So an applier can rely on those declared before it having applied the same tevent.
* A tevent that reaches no applier is an error, unless the routing lets it go with `IgnoreUnhandled`.

So when the list price changes,

[!code-csharp[](shared-tomponents-and-tentities.cs#ChangeTheListPrice)]

the list price slot wraps the change in a `ProductListPriceTevent<>`, the product publishes that as its own tevent, the product's routing routes it through the list price slot back to the list price, and the list price's applier sets its `Amount`. The sale price never sees it.

The mistakes that are easy to make are loud:
* A class that doesn't override `TeventAppliers` doesn't compile. A subclass that inherits its base class's routing, instead of extending it, is refused when it is first constructed.
* A slot handed a class that doesn't implement its adopting wrapper tevent, such as another slot's, throws when the slot is made.
* A shared tomponent constructed with another slot than the one whose member it is kept in throws when it first publishes, and when its owner's history is loaded.

## Subscribing to a shared tomponent's tevents
To follow the list price of every product, subscribe to the whole published tevent:

[!code-csharp[](shared-tomponents-and-tentities.cs#SubscribeToTheListPrice)]

The handler gets the published tevent as it stands: `changed.Tevent` is the adopting wrapper tevent, a product tevent carrying the product's id, and `changed.Tevent.Tevent` is the price's own tevent.

The adopting wrapper tevent is a tevent of the owner, so it can say what the change means to the owner. Both price slots' adopting wrapper tevents are `IProductTevent.IRepriced`, so this subscriber hears of every change to either price, and of nothing else:

[!code-csharp[](shared-tomponents-and-tentities.cs#SubscribeToRepricing)]

In the same way, a subscriber to `IProductTevent` gets every adopting wrapper tevent along with the product's other tevents.

And since every adopting wrapper tevent is an `IPublisherTevent<>`, a subscriber can follow a shared tomponent's tevents in every slot of every owner without knowing any of them. This one hears of every price change, in products and bundles alike:

[!code-csharp[](shared-tomponents-and-tentities.cs#SubscribeToEveryPrice)]

>[!WARNING]
> A subscription sees one level of wrapping. A subscription to a type that is no wrapper is matched against what the published wrapper wraps, and a subscription to a wrapper type is matched against the published wrapper itself. A shared tomponent's tevent is wrapped twice, so:
> * A subscriber to `IPriceTevent.IChanged` receives nothing: what the product's wrapper wraps is the adopting wrapper tevent, not the change.
> * A subscriber to `IProductListPriceTevent<IPriceTevent.IChanged>` receives nothing either: it is a wrapper type, and the published wrapper is the product's. Wrap it once more, and it receives the list price's changes: `IPublisherTevent<IProductListPriceTevent<IPriceTevent.IChanged>>`.

A shared tomponent's counterpart with an id of its own, of which an owner can hold any number, is a [shared tentity](shared-tentities.md).
