#### Adding properties to our events
You may have noticed that the events on the last page were missing a little something, namely properties.\
Now the natural expectation would be for a slightly more realistic version of something like this:

[!code-csharp[](property-updated-events.cs#IUserCreated)]

to be something like this:
[!code-csharp[](property-updated-events.cs#IUserEmailEventsNaive)]

However, doing that does not work well at all.

#### The dilemma of Fine-grained vs Coarse-grained events
The reason the above code is terrible idea, is that some event listeners care only about updated data, and others care about semantics. If we do things like above, the code that care only about data will not only have to know about and manually listen to every single concrete event in the system that updates `Email`, we must keep track of every such listener for every single aggregate property in our whole system, and update  each of them every time a new event that updates a user email is created :fearful:

Classically the above problem has lead to projects being forced to choose between using fine-grained property-updated events and coarse-grained semantically meaningful domain events. The problem is that both choices are maintainability disasters. If you go with property-updated style events you lose virtually all ability to understand the semantics of what happened, as a single user interaction is exploded into a number of individual data atoms :grimacing: If you go with coarse-grained events, we run head first into the issues described in the previous paragraph. You cannot win as long as you accept the choice. 

#### Unifying Fine-grained and Coarse-grained events
With semantic events you don't need to choose. If you do this: 

[!code-csharp[](property-updated-events.cs#IUserEmailEventsWorking)]

The whole problem disappears like it never existed :relaxed:\
When we design events like that, a listener like this ...
[!code-csharp[](property-updated-events.cs#EmailPropertyUpdatedListener)]
... will never need to change throughout the whole lifetime of the system. It will be called whenever a user's email is updated. Period.

Likewise, listeners that care about when users are registered will listen to `IUserRegistered` and receive the same benefits. No matter how many new ways of registering users we add, no matter how many new subtypes of `IUserRegistered` are added. That code will always be called and does not need to change.

