>[!Note]
> Now we are uncharted waters. We have a fair amount of confidence that the design here will work out. And you may already do be able to do this in Compze on your own, but there is no support built into the Aggregate base classes to make simple, nor have we done it ourselves yet. You would need to perform the wrapping manually.

>[!NOTE]
> Aggregate specific nested components and entities are fully supported already. We've already [shown examples](event-naming.md) of how events for them look. You just nest component/entity event interfaces within your root event interface. Simple. What we are discussing on this page is how to create event based components and entities that can be used in multiple different event based aggregates without code duplication. That is another ballgame.

# Reusable event based components and entities
So as in the note above, the question is how we go about it when there is some sort of component or entity that we want to be able to reuse in multiple different aggregates without code duplication. We suspect this is not terribly common, yet we surely would consider any approach to modeling that did not support this without breaking a sweat to be badly limited.

So how would one go about it?

Since it is shared reusable component 
* It cannot raise aggregate events, that must be upp to the aggregate.
* We must be able to subscribe to the published events by type, meaning that the event ultimately raised by the aggregate must declaratively, statically, contain the type of the event from the component.

Thankfully the solution to inheriting aggregates has taken us most of the way to a solution for this problem too. We will just need to wrap one more time, generic covariance to the rescue once more. 



