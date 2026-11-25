# Tentities

A tentity is an [entity](~/docs/concepts.md#entity) of a [taggregate](taggregates.md): a part of it with an identity
of its own, of which the taggregate can hold any number, and which is teventive like the taggregate. It changes its
state only by applying the tevents it publishes, and those tevents are the taggregate's, as a [tomponent](tomponents.md)'s
are. What a tentity adds is its id: each of its tevents carries it, so that the taggregate knows which of its tentities
the tevent is for.

A tentity belongs to one taggregate. One written once, to be used in many different taggregates, is a
[shared tentity](shared-tentities.md).

This page gives the `User` of [Taggregates](taggregates.md) addresses: as many as it likes, each of which can move, and
be removed.

## Its tevents
A tentity's tevents are rooted in an interface nested in the taggregate's root tevent interface, as a tomponent's are,
which declares the tentity's id: `IUserTevent.IAddress` declares `AddressId`.

[!code-csharp[](tentities.cs#tevents)]

The tevents that matter most are those that begin and end a tentity: `IAdded`, which creates an address, and
`IRemoved`, which removes it.

The tevent classes mirror the interfaces. The class of the tentity's root tevent also has an *id getter-setter*, a small
class that tells Compze how to read and set the address id of an address tevent:

[!code-csharp[](tentities.cs#tevent-classes)]

Only `Added` is given the address's id. Every other address tevent gets it from the address that publishes it, through
the id getter-setter.

## The tentity

[!code-csharp[](tentities.cs#address)]

* **It derives from `User.RemovableEntity<...>`,** a base class every taggregate class has. Its type arguments are the
  tentity's own class, the type of its id, its root tevent interface and the class implementing it, the tevents that
  create and remove it, and its id getter-setter. A tentity that is never removed derives from `User.Entity<...>`
  instead, which takes no removed tevent.
* **Its constructor takes the user it belongs to.** Compze constructs it, when its created tevent is applied.
* **Its `Id` is the id its created tevent carries.**
* **It declares its tevent routing once, for the class,** as a taggregate does.
* **It publishes through the user,** as a tomponent does: `MoveTo` publishes an address tevent, which carries the
  address's id and the user's.

## The taggregate that holds them

[!code-csharp[](tentities.cs#user)]

The user keeps its addresses in a collection that manages itself: `CreateSelfManagingCollection` makes it, and the
user routes the address tevents to it with `Entities`. The collection creates an address when an `IAdded` arrives,
routes every other address tevent to the address whose id it carries, and removes an address when its `IRemoved`
arrives. `Entities` is the collection as the user's code reads it: in creation order, or by id.

`AddByPublishing` publishes a created tevent, and returns the tentity it created.

## Using it

[!code-csharp[](tentities.cs#using)]
