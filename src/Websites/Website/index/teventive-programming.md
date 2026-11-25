<div>

#### [Teventive](~/Compze/Teventive/Taggregates/_docs/definition.md)
Teventive programming models a domain in [tevents](~/docs/concepts.md#tevent): events whose .NET types declare what they mean, and route them to their subscribers. Leveraging .NET type compatibility like this
* Gives an unprecedented ability to understand domains in terms of how the tevents that can occur relate to each other.
* Eliminates all need for manual event routing and type checking.
* Dramatically reduces the number of subscriptions needed.
* Enables modeling inheritance and composition of [taggregates](~/docs/concepts.md#taggregate), aggregates that change only by applying their own tevents, with elegant precision.
* [Unifies](~/Compze/Teventive/Taggregates/_docs/property-updated-tevents.md) fine-grained property-updated tevents and coarse-grained domain tevents.
* Enables subscribing to precisely the tevent you need, while being guaranteed that when new tevents are added, inheriting the current tevent, you will receive those too without needing to change anything in your subscriber code.

In spite of this, to this day, countless event driven applications are shock full of code that leverages none of these possibilities. Resulting in what can only be described as maintenance nightmares.

It's essentially the event equivalent of working in C# while completely refusing to use generics and object-oriented programming. Surely we should not even need to argue that this is ill-advised?

</div>
