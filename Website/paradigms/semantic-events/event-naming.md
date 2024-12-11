#### How to name events

On the last page we were starting to run into some slightly unwieldy event names. Like `IUserEmailPropertyUpdatedEvent`. Now that it still reasonably readable and mostly unambiguous. That will not hold true as things get more complicated though if we keep naming events like that.

Take this slightly more complicated event name for instance: `IUserProfileNamePropertyUpdatedEvent`. Is this an event raised by a `UserProfile` aggregate? Or is it an event raised by a `Profile` component within a `User` aggregate? Or is it a `PropertyUpdatedEvent` from the `ProfileName` component of the `User` aggregate? Imagine trying to untangle such questions when you have four or more levels of nested composition in your aggregate.... 

We've tried a number of different ways of dealing with event naming. One is using namespaces, which sounds like it should work. `User.Events.ProfileComponent.PropertyUpdatedEvents.IName`. Not too bad, right? Except when searching for types, or after your IDE helpfully simplifies references for you, you end up looking at just `IName`, which is unhelpful...

[!code-csharp[](event-naming.cs#unhelpful)]

The best structure we've found to deal with event naming is this:
[!code-csharp[](event-naming.cs#nested-events2)]
Nice and structured with each level of nesting inheriting from the previous level.

Giving us:
[!code-csharp[](event-naming.cs#helpful2)]

Readable and unambiguous. Not bad.

>[!NOTE]
> We do not repeat `Event` for every level of nesting. And should you feel that the repeated `I` is an eyesore we will not condemn doing this: `IUserEvent.Profile.PropertyUpdated.Name` :wink: