### So what does all of that mean in practice?
Let's dive right in to some code illustrating semantic tevents.

This is the root of the tevent inheritance hierarchy. By some level of indirection, every tevent implements it:
[!code-csharp[](introduction.cs#ITevent)]

Every tevent published by a taggregate will implement this interface:
[!code-csharp[](introduction.cs#ITaggregateTevent)]

Every tevent that means a taggregate was created will implement this interface.
[!code-csharp[](introduction.cs#ITaggregateCreatedTevent)]

Let's use User as an example taggregate. Here's the root of the User taggregate tevent hierarchy: 

[!code-csharp[](introduction.cs#IUserTevent)]

And of course things can happen related to users: 

[!code-csharp[](introduction.cs#UserTevents1)]

Now stop and look carefully at how the tevents so far implement each other. This is the core concept of semantic tevents. That the relationship in meaning between tevents can be modeled using .Net type compatibility, and that we can use the same mechanism to listen to exactly the tevents we need. Most of this information is declared by implementing various interfaces.

Let's examine a simple example of what this means in practice. Here's how you might subscribe to these tevents:
[!code-csharp[](introduction.cs#UserTeventRegistration)]

Now let's see if your intuition is on target here. When an `IUserImported` tevent is published, What will be the output?
The correct answer is: 

>User: SOME-GUID something happened  
>User: SOME-GUID registered  
>User: SOME-GUID imported

An `IUserImported` tevent is compatible with all the registered handlers, and will therefore be delivered to all of them in the order that the handlers were registered.

If an `IUserTevent` was published only the first subscriber would be called, if an `IUserRegistered` was published, the first two would be called.