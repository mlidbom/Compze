#### How to name tevents

On the last page we were starting to run into some slightly unwieldy tevent names. Like `IUserEmailPropertyUpdatedTevent`. Now that it still reasonably readable and mostly unambiguous. That will not hold true as things get more complicated though if we keep naming tevents like that.

Take this slightly more complicated tevent name for instance: `IUserProfileNamePropertyUpdatedTevent`. Is this a tevent published by a `UserProfile` taggregate? Or is it a tevent published by a `Profile` component within a `User` taggregate? Or is it a `PropertyUpdatedTevent` from the `ProfileName` component of the `User` taggregate? Imagine trying to untangle such questions when you have four or more levels of nested composition in your taggregate.... 

We've tried a number of different ways of dealing with tevent naming. One is using namespaces, which sounds like it should work. `User.Tevents.ProfileComponent.PropertyUpdatedTevents.IName`. Not too bad, right? Except when searching for types, or after your IDE helpfully simplifies references for you, you end up looking at just `IName`, which is unhelpful...

[!code-csharp[](tevent-naming.cs#Unhelpful)]

The best structure we've found to deal with tevent naming is this:
[!code-csharp[](tevent-naming.cs#nested-tevents2)]
Nice and structured with each level of nesting inheriting from the previous level.

Giving us:
[!code-csharp[](tevent-naming.cs#helpful2)]

Readable and unambiguous. Not bad.

>[!NOTE]
> We do not repeat `Tevent` for every level of nesting. And should you dislike the repeated `I` we do not condemn doing this: `IUserTevent.Profile.PropertyUpdated.Name` :wink: