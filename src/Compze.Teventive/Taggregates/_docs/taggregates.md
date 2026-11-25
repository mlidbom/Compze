# Taggregates

A taggregate is an [aggregate](~/docs/concepts.md#aggregate) whose state changes only by applying the tevents it
publishes. It never sets its own state directly: it publishes a tevent that says what happened, and that tevent's
*applier* changes the state. Every tevent it publishes carries its id.

This page writes one: a `User`, which publishes user tevents like those of the previous pages.

## Its tevents
A taggregate's tevents are rooted in an interface of its own, which inherits `ITaggregateTevent`, the interface every
taggregate's tevents implement. They are nested as on the [tevent naming](tevent-naming.md) page:

[!code-csharp[](taggregates.cs#tevents)]

`IRegistered` is also an `ITaggregateCreatedTevent`: the tevent that creates a user, and gives it its id. Both
`IRegistered` and `IChangedEmail` are `IPropertyUpdated.IEmail`, as on the [property updated tevents](property-updated-tevents.md)
page.

## Its wrapper tevent
A taggregate publishes each of its tevents inside a *wrapper tevent*, which says which class of taggregate published
it. Every taggregate declares the interface of its wrapper; [taggregate inheritance](taggregate-inheritance.md) shows
what it makes possible.

[!code-csharp[](taggregates.cs#wrapper-tevent)]

## The tevent classes
Something must implement the interfaces. The classes mirror them: a class for each tevent, deriving from
`TaggregateTevent`, and a generic class for the wrapper.

[!code-csharp[](taggregates.cs#tevent-classes)]

Only the tevent that creates a user is given its id. Every other tevent gets it from the user that publishes it.

## The taggregate

[!code-csharp[](taggregates.cs#user)]

* **It derives from `Taggregate<...>`.** The five type arguments are the taggregate's own class, its root tevent
  interface and the class implementing it, and its wrapper tevent interface and the class implementing that.
* **It declares its tevent routing once, for the class,** with `DeclareAppliers`, keeps it in a static field, and
  returns it from its override of the protected `TeventAppliers` property. Every user shares it.
* **`For<T>` routes every tevent assignable to `T` to an applier.** Here registering and changing the email both reach
  the applier of `IPropertyUpdated.IEmail`. The applier is a `static` lambda that gets the user as its first parameter,
  since the routing is shared and holds no state of its own.
* **Its state changes only in its appliers.** `ChangeEmail` doesn't set `Email`: it publishes a tevent, and the applier
  sets it.
* **No public setters, and no public fields,** in the taggregate or in its tevents. Compze refuses such a class the
  first time it is used: a taggregate whose state can change other than by its tevents no longer matches them.
* **A tevent that reaches no applier is an error.** A taggregate's routing can let some go with `IgnoreUnhandled`.

## Using it

[!code-csharp[](taggregates.cs#using)]

Publishing a tevent applies it at once. The user's `Version` counts its changes: 1 when it is created, and one more for
each tevent it publishes.

## Where its tevents go
Taggregates are kept by a [taggregate store](~/docs/concepts.md#taggregate-store). A taggregate that a store keeps
publishes its tevents to the handlers of [Tessaging](~/Compze/Tessaging/_docs/in-one-process.md) right after each
publish, inside the unit of work that changes it, as the user tevents on the [introduction](introduction.md) page are
handled.

A taggregate can hold parts of its own: [tomponents](tomponents.md), and [tentities](tentities.md).
