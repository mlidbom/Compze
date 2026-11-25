# Taggregate Inheritance
A taggregate class can inherit another, as any class can. Here `Cat` and `Dog` inherit `Animal`, and the tevents of `Animal` are theirs too: a cat's birth and a dog's birth are both an `IAnimalTevent.IBorn`.

[!code-csharp[](taggregate-inheritance.cs#noises1)]

## Telling the publisher apart
A subscriber that cares only about dogs' births can't tell them apart by the birth tevent: there is one `IBorn`, which every animal publishes. So every taggregate publishes each tevent inside a *wrapper tevent* of its class: a generic interface that says which class of taggregate published the tevent, and whose type argument is the tevent itself.

[!code-csharp[](taggregate-inheritance.cs#noises1wrapped)]

A dog's birth is published as an `IDogTevent<IAnimalTevent.IBorn>`, and a cat's as an `ICatTevent<IAnimalTevent.IBorn>`. Both wrappers inherit `IAnimalTevent<T>`, and their type parameter is covariant, `out T`, so each is also an `IAnimalTevent<IAnimalTevent.IBorn>`, just as an `IEnumerable<string>` is an `IEnumerable<object>`:

[!code-csharp[](taggregate-inheritance.cs#enumerable-type-compatibility)]

Since tevents are routed by type compatibility, each subscriber receives exactly what its type asks for. The births of dogs only:

[!code-csharp[](taggregate-inheritance.cs#doglistener)]

Of cats only:

[!code-csharp[](taggregate-inheritance.cs#catlistener)]

Of every animal, whichever kind published it:

[!code-csharp[](taggregate-inheritance.cs#animallistener)]

Of every animal, handling each kind differently:

[!code-csharp[](taggregate-inheritance.cs#wrappedanimallistener)]

## The taggregates
Each kind of animal publishes its tevents in a wrapper of its own. The tevent classes mirror the interfaces: a class for each tevent, and a generic class for each wrapper.

[!code-csharp[](taggregate-inheritance.cs#tevent-classes)]

`Animal` is a taggregate like any other, written as on the [Taggregates](taggregates.md) page. Its tevent routing has the applier that records when an animal was born.

[!code-csharp[](taggregate-inheritance.cs#animal)]

`Cat` and `Dog` each extend `Animal`'s routing, so `Animal`'s applier applies their births too, and each names its own wrapper class. That is all it takes for a cat's birth to be published as an `ICatTevent<IAnimalTevent.IBorn>`, and a dog's as an `IDogTevent<IAnimalTevent.IBorn>`:

[!code-csharp[](taggregate-inheritance.cs#cat-and-dog)]

A kind of animal with tevents of its own declares their appliers in the lambda it hands `Extend`. `Animal`'s appliers apply each tevent first.
