>[!NOTE]
>You are absolutely not expected to understand all the how's and why's of this definition the first time you read it. Think of it as combination cheat sheet and teaser.

# Tevents

A tevent is an event, a message saying that something happened, that Compze routes by its .NET type: a type-routed event. Its type is its whole contract. The type declares what the tevent means, in as much detail as the domain needs, and decides which subscribers receive it. Tevents are what [teventive programming](~/docs/concepts.md#teventive) models a domain in, and [Tessaging](~/Compze/Tessaging/_docs/introduction.md) delivers them.

What sets a tevent apart from an event as most systems have them:

#### Declare their meaning through their type
Tevents use the type system of the language in which they are implemented to declare their meaning, in as much detail and as unambiguously as possible. All the semantics of the event should be part of the declaration of the event type.

#### Are always interfaces
In .Net tevents are always interfaces, because to encode meaning in detail multiple inheritance is required. Some class will implement the interface, but you always subscribe to interfaces and design in terms of interfaces.

#### Implements `ITevent`
Every tevent implements `ITevent`

#### Are routed by type compatibility
The type of a tevent is the means through which tevents are routed to subscribers. Every single type-compatible registered handler method will be called when a tevent is published: in the publishing process, and, for a tevent whose type makes it remotable, in every other endpoint that subscribes to it (see [Tessaging's delivery guarantees](~/Compze/Tessaging/_docs/delivery-guarantees.md)).

To illustrate: Given that the method `void HandleTevent(ISomeTevent tevent)` is registered as a tevent handler, then every single tevent that can be assigned to a variable of type `ISomeTevent` will be delivered to `HandleTevent`

##### This type compatibility includes support for generic covariance
Interface inheritance is not enough to model realistic domains effectively. To support inheritance of [taggregates](~/docs/concepts.md#taggregate), and reusable tevent based components, tevents leverage generic covariance. The covariant wrapper interface is `IPublisherTevent<out TTevent>`: a wrapper whose own type identifies the tevent's publisher while its type parameter carries the wrapped tevent's full type.

For example: this handler: `void HandleTevent(IPublisherTevent<ITevent> tevent)` would be called whenever any type of `IPublisherTevent<T>` was published. `IPublisherTevent<IUserTevent>`, `IPublisherTevent<IAnimalTevent>` and so on.

Likewise, given a publisher-specific wrapper interface `IAnimalTevent<out T> : IPublisherTevent<T>;` when any `IAnimalTevent<T>` was published, the above handler would also be called.