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
Interface inheritance is not enough to a model realistic domains effectively. To support inheritance of event based aggregates, and reusable event based components, semantic events leverage generic covariance. 

For example: given the interface  `IWrapperEvent<out T>` this handler: `void HandleEvent(IWrapperEvent<IEvent> anEvent)` would be called whenever any type of `IWrapperEvent<T>` was published. `IWrapperEvent<IUserEvent>`, `IWrapperEvent<IAnimalEvent>` and so on.

Likewise, given the interface `IInheritingWrapperEvent<out T> : IWrapperEvent<T>;` when any `IInheritingWrapperEvent<T>` was published, the above handler would also be called.