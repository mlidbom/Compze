#### How to name events

On the last page we were starting to run into some slightly unwieldy event names. Like `IUserEmailPropertyUpdatedEvent`. Now that it still reasonably readable and mostly unambiguous. That will not hold true as things get more complicated though if we keep naming events like that.

Take this slightly more complicated event name for instance: `IUserProfileNamePropertyUpdatedEvent`. Is this an event raised by a `UserProfile` aggregate? Or is it an event raised by a `Profile` component within a `User` aggregate? Or is it a `PropertyUpdatedEvent` from the `ProfileName` component of the `User` aggregate? Imagine trying to untangle such questions when you have four or more levels of nested composition in your aggregate.... 

We've tried a number of different ways of dealing with event naming. One is using namespaces, which sounds like it should work. `User.Events.ProfileComponent.PropertyUpdatedEvents.IName`. Not too bad, right? Except when searching for types, or after your IDE helpfully simplifies references for you, you end up looking at just `IName`, which is unhelpful...

[!code-csharp[](event-naming.cs#unhelpful)]

What we've mostly ended up with so far is a structure similar to the below code. We would certainly not call it pretty, but it works reasonably well in practice in our experience: 

[!code-csharp[](event-naming.cs#nested-events)]

This gives us:
[!code-csharp[](event-naming.cs#helpful)]

We are not satisfied with the above though. A promising alternative we recently came up with is something like this:
[!code-csharp[](event-naming.cs#nested-events2)]

Giving us:

[!code-csharp[](event-naming.cs#helpful2)]
You know, this we actually like pretty well. But we have not used it much yet and dare not promise that it does not carry any nasty surprises. Assuming we don't find any nasty gotchas, this will most likely become our standard approach.

