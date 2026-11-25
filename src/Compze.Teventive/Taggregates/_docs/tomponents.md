# Tomponents

A tomponent is a component of a [taggregate](taggregates.md): a part of it, with state of its own, that is teventive
like the taggregate. It changes its state only by applying the tevents it publishes, and those tevents are the
taggregate's: they are part of its tevent hierarchy, carry its id, and reach subscribers as the taggregate's tevents.

A tomponent belongs to one taggregate. One written once, to be used in many different taggregates, is a
[shared tomponent](shared-tomponents.md).

This page gives the `User` of [Taggregates](taggregates.md) a profile, which holds the user's name.

## Its tevents
A tomponent's tevents are rooted in an interface nested in the taggregate's root tevent interface, and inheriting it:
`IUserTevent.IProfile`, as the [tevent naming](tevent-naming.md) page has it.

[!code-csharp[](tomponents.cs#tevents)]

The tevent classes mirror the interfaces, nested the same way:

[!code-csharp[](tomponents.cs#tevent-classes)]

## The tomponent

[!code-csharp[](tomponents.cs#profile)]

* **It derives from `User.Component<...>`,** a base class every taggregate class has: its three type arguments are the
  tomponent's own class, its root tevent interface and the class implementing that.
* **Its constructor takes the user it belongs to.**
* **It declares its tevent routing once, for the class,** with `DeclareAppliers`, as a taggregate does.
* **It publishes through the user.** `Rename` publishes a profile tevent, which the user publishes as its own: it gets
  the user's id, counts as one of the user's changes, and is wrapped in the user's wrapper tevent.

## The taggregate that holds it

[!code-csharp[](tomponents.cs#user)]

The user constructs its profile with itself, and routes the profile's tevents to it with `Component`: every tevent
assignable to the profile's root tevent interface reaches the profile, which applies it through its own routing.

## Using it

[!code-csharp[](tomponents.cs#using)]

A tomponent can hold tomponents and [tentities](tentities.md) of its own, declared and routed the same way.
