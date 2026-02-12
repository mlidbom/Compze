### So what does all of that mean in practice?
Let's dive right in to some code illustrating semantic events.

This is the root of the event inheritance hierarchy. By some level of indirection, every event implements it:
[!code-csharp[](introduction.cs#IEvent)]

Every event raised by an aggregate will implement this interface:
[!code-csharp[](introduction.cs#IAggregateEvent)]

Every event that means an aggregate was created will implement this interface.
[!code-csharp[](introduction.cs#IAggregateCreatedEvent)]

Let's use User as an example aggregate. Here's the root of the User aggregate event hierarchy: 

[!code-csharp[](introduction.cs#IUserEvent)]

And of course things can happen related to users: 

[!code-csharp[](introduction.cs#UserEvents1)]

Now stop and look carefully at how the events so far implement each other. This is the core concept of semantic events. That the relationship in meaning between events can be modeled using .Net type compatibility, and that we can use the same mechanism to listen to exactly the events we need. Most of this information is declared by implementing various interfaces.

Let's examine a simple example of what this means in practice. Here's how you might subscribe to these events:
[!code-csharp[](introduction.cs#UserEventRegistration)]

Now let's see if your intuition is on target here. When an `IUserImported` event is published, What will be the output?
The correct answer is: 

>User: SOME-GUID something happened  
>User: SOME-GUID registered  
>User: SOME-GUID imported

The type `IUserImported` event is compatible with all the registered handlers, and will therefore be delivered to all of them in the order that the handlers were registered.

If an `IUserEvent` was published only the first subscriber would be called, if an `IUserRegistered` was published, the first two would be called.