>[!Note]
> The taggregate base classes now contain built-in support for this: a shared tomponent is written once as a `SharedTomponent`, and its owner wires it in through a `SharedTomponentSlot`, which performs the wrapping described below for you. This page has not yet caught up with teaching that support.

>[!NOTE]
> Taggregate specific nested components and entities are fully supported already. We've already [shown examples](tevent-naming.md) of how tevents for them look. You just nest component/entity tevent interfaces within your root tevent interface. Simple. What we are discussing on this page is how to create tevent based components and entities that can be used in multiple different tevent based taggregates without code duplication. That is another ballgame.

# Reusable tevent based components and entities
So as in the note above, the question is how we go about it when there is some sort of component or entity that we want to be able to reuse in multiple different taggregates without code duplication. We suspect this is not terribly common, yet we surely would consider any approach to modeling that did not support this without breaking a sweat to be badly limited.

So how would one go about it?

Since it is shared reusable component 
* It cannot publish taggregate tevents, that must be up to the taggregate.
* We must be able to subscribe to the published tevents by type, meaning that the tevent ultimately published by the taggregate must declaratively, statically, contain the type of the tevent from the component.

Thankfully the solution to inheriting taggregates has taken us most of the way to a solution for this problem too. We will just need to wrap one more time, generic covariance to the rescue once more.

.... More soon. 2024-12-14



