>[!NOTE]
>You are absolutely not expected to understand all the how's and why's of this definition the first time you read it. Think of it as combination cheat sheet and teaser.

# Semantic Events:

#### Declare their meaning through their type
Semantic events use the type system of the language in which they are implemented to declare their meaning, in as much detail and as unambiguously as possible. All the semantics of the event should be part of the declaration of the event type.

#### Are always interfaces
In .Net semantic events are always interfaces, because to encode meaning in detail multiple inheritance is required. Some class will implement the interface, but you always subscribe to interfaces and design in terms of interfaces.

#### Implements `ITevent`
Every semantic tevent implements `ITevent`

#### Are routed by type compatibility
The type of a tevent is the means through which tevents are routed to subscribers. Every single type-compatible registered handler method, local or remote, will be called when a tevent is published.

To illustrate: Given that the method `void HandleTevent(ISomeTevent tevent)` is registered as a tevent handler, then every single tevent that can be assigned to a variable of type `ISomeTevent` will be delivered to `HandleTevent`

##### This type compatibility includes support for generic covariance
Interface inheritance is not enough to model realistic domains effectively. To support inheritance of tevent based taggregates, and reusable tevent based components, semantic tevents leverage generic covariance. The covariant wrapper interface is `IPublisherTevent<out TTevent>`: a wrapper whose own type identifies the tevent's publisher while its type parameter carries the wrapped tevent's full type.

For example: this handler: `void HandleTevent(IPublisherTevent<ITevent> tevent)` would be called whenever any type of `IPublisherTevent<T>` was published. `IPublisherTevent<IUserTevent>`, `IPublisherTevent<IAnimalTevent>` and so on.

Likewise, given a publisher-specific wrapper interface `IAnimalTevent<out T> : IPublisherTevent<T>;` when any `IAnimalTevent<T>` was published, the above handler would also be called.