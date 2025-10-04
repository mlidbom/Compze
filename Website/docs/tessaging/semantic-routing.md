# Semantic Routing
>[!NOTE] 
> Semantic routing is used throughout the toolkit. It is foundational for the Event Store, Service Bus, Query Model updaters and Generators...


## Definition
* Events are delivered to every registered handler with a compatible argument type.
* Commands and query message types must have exactly one handler.

> [!TIP]
> The first rule is really just polymorphism.

> [!TIP]
> Semantic Routing is also known as "Polymorphic routing" or "Polymorphic  dispatching".

### Clarifying examples

Given these event interfaces and implementing classes

    interface IA
    interface IB : IA
    interface IC : IB
    
    class A : IA {}
    class B : IB {}
    class C : IC {}

And these handler methods registered on our service bus

    void HandleA(IA ia){} //Handles IA, IB and IC
    void HandleB(IB ib){} //Handles IB and IC
    void HandleC(IC ic){} //Handles only IC

.Let's publish some events and examine the results.

    serviceBus.Publish(new A()); //Delivered to HandleA
    serviceBus.Publish(new B()); //Delivered to HandleA and HandleB
    serviceBus.Publish(new C()); //Delivered to HandleA, HandleB and HandleC

### Loose coupling through interfaces
Working with events in terms of interfaces maintains flexibility.
Here is a partial list of things it is possible to do without having to change any code in any event listener.

* Refactoring event classes
* Adding event classes
* Adding event interfaces
* Changing event inheritance hierarchy

> [!TIP]
> Remember to think about events in terms of interfaces. The event classes are an implementation detail that should only ever be known by the code that publishes the event.

> [!WARNING]
> Do not subscribe to event classes. You will lose the benefits just discussed.

