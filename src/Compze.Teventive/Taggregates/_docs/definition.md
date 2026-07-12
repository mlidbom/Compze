>[!NOTE]
>You are absolutely not expected to understand all the how's and why's of this definition the first time you read it. Think of it as combination cheat sheet and teaser.

# Semantic Events:

#### Declare their meaning through their type
Semantic events use the type system of the language in which they are implemented to declare their meaning, in as much detail and as unambiguously as possible. All the semantics of the event should be part of the declaration of the event type.

#### Are always interfaces
In .Net semantic events are always interfaces, because to encode meaning in detail multiple inheritance is required. Some class will implement the interface, but you always subscribe to interfaces and design in terms of interfaces.

#### Implements `IEvent`
Every semantic event implements `IEvent`

#### Are routed by type compatibility
The type of an event is the means through which events are routed to subscribers. Every single type-compatible registered handler method, local or remote, will be called when an event is published.

To illustrate: Given that the method `void HandleEvent(IEventType anEvent)` is registered as an event handler, then every single event that can be assigned to a variable of type IEventType will be delivered to `HandleEvent`

##### This type compatibility includes support for generic covariance
Interface inheritance is not enough to model realistic domains effectively. To support inheritance of event based aggregates, and reusable event based components, semantic events leverage generic covariance. The covariant wrapper interface is `IPublisherIdentifyingTevent<out TTevent>`: a wrapper whose own type identifies the event's publisher while its type parameter carries the wrapped event's full type.

For example: this handler: `void HandleTevent(IPublisherIdentifyingTevent<IEvent> tevent)` would be called whenever any type of `IPublisherIdentifyingTevent<T>` was published. `IPublisherIdentifyingTevent<IUserEvent>`, `IPublisherIdentifyingTevent<IAnimalEvent>` and so on.

Likewise, given a publisher-specific wrapper interface `IAnimalTevent<out T> : IPublisherIdentifyingTevent<T>;` when any `IAnimalTevent<T>` was published, the above handler would also be called.