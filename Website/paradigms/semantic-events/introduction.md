Rather than get bogged down in trying for a definition, we are just going to dive right in to code using semantic events.

This is the root of the event inheritance hierarchy. By some level of indirection, Every event implements it:
[!code-csharp[](introduction.cs#IEvent)]

Every event raised by an aggregate will implement this interface:
[!code-csharp[](introduction.cs#IAggregateEvent)]

Every event that means an aggregate was created will implement this interface.
[!code-csharp[](introduction.cs#IAggregateCreatedEvent)]

Most applications have users, so let's use User as an example aggregate. Here's the root event: 

[!code-csharp[](introduction.cs#IUserEvent)]

And of course things can happen related to users: 

[!code-csharp[](introduction.cs#UserEvents1)]

Now stop and look at how these interfaces inherit each other. This is the core concept of semantic events. That the relationship in meaning between events can be modeled using .Net type compatibility.


Let's examine a simple example of what this means in practice. Here's how you might subscribe to these events:
[!code-csharp[](introduction.cs#UserEventRegistration)]

Now let's see if your intuition is on target here. When an `IUserImportedFromGoogle` event is published, What will be the output?
The correct answer is: 

>Something happened to user: SOME-GUID  
>User: SOME-GUID was registered  
>User: SOME-GUID was imported from Google

The type `IUserImportedFromGoogle` is compatible with all the registered handlers, and will therefore be delivered to all of them in the order that the handlers were registered.
