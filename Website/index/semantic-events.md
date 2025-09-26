<div>

#### [Semantic Events](../paradigms/semantic-events/definition.md)
Leveraging well established and understood C# features enables an event modeling paradigm which
* Gives an unprecedented ability to understand domains in terms of how the events that can occur relate to each other.
* Eliminates all need for manual event routing and type checking.
* Dramatically reduces the number of event subscriptions needed.
* Enables modeling inheritance and composition of event based [aggregates](../docs/prerequisite-terms.md#aggregate) with elegant precision.
* [Unifies](../paradigms/semantic-events/property-updated-events.md) fine-grained property-updated style events and coarse-grained domain events.
* Enables subscribing to precisely the event you need, while being guaranteed that when new events are added, inheriting the current event, you will receive those too without needing to change anything in your subscriber code.

In spite of this, to this day, countless event driven applications are shock full of code that leverages none of these possibilities. Resulting in what can only be described as maintenance nightmares.

It's essentially the event equivalent of working in C# while completely refusing to use generics and object-oriented programming. Surely we should not even need to argue that this is ill-advised?

</div>
