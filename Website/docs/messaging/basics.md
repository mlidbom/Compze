# Messaging Basics
> [!NOTE]
> The code blocks in this section contain pseudocode for illustration purposes. 
> It is not compatible with any specific library including Composable.

## Messaging
Any method call can, if you squint, be viewed as one object sending a message to another object.
However, this ties the sender tightly to the receiver.
Loose coupling benefits can be had by making the message passing explicit.
By sending messages to a receiver through some intermediary rather than directly.
Doing so is called messaging.

> [!TIP]     
> Messaging is also known as message passing.

### Messaging terms
Here we define some terms as they are used in the context of this document.

**Message**  
An object for the purpose of sending data to a receiver.

**Message Type**  
The System.Type returned by `message.GetType()`.

**Message Handler**  
In principle just a function that takes a message as a parameter.

    void Handle(RegisterAccountCommand command);


In practice most message handlers need to have one or more dependencies injected into them.
In order to support this handlers are often required to be wrapped inside interfaces.
 That way instances of implementing classes can be resolved from an IOC container easily.

    interface IMessageHandler<RegisterAccountCommand>
    {
        void Handle(RegisterAccountCommand aMessage);
    }

**Routing**  
The mechanism by which messages are delivered to handlers.

**Service Bus**
A component which decouples message senders from message handlers.
Instead of client code calling handler methods, clients send and receive messages via the bus.
The bus is responsible for routing the messages to the appropriate handler(s) and invoking them.

.Manual service invocation requires an instance of the service.

    serviceInstance.RegisterAccount(arguments....

Client don't even know where the service is when accessing it across a bus

    bus.Send(new RegisterAccountCommand(

> [!TIP]
> The benefits of this decoupling may not be obvious at first, but they are profound.

**Command**  
A message that instructs the handler to perform an action.

    class RegisterAccountCommand
    {
        AccountId AccountId { get; }
        Password Password { get; }
        Email Email { get; }
    }


**Event**  
A message that informs handlers about something that has happened.

    interface IAccountRegisteredEvent
    {
        AccountId AccountId { get; }
        Password Password { get; }
        Email Email { get; }
    }

**Query**  
A message that asks the handler to supply some data.

    class RecentlyRegisteredAccountsQuery
    {
        TimeSpan MaxAge { get; }
    }

**Command Handler**  
A message handler for a command. Must ensure that the command is successfully executed or throw an exception.

**Query Handler**  
A message handler for a query. Must ensure that the query is successfully executed or throw an exception.

**Event Handler**  
A message handler for an event.

**Event Listener**  
Synonym of Event Handler.

**Subscribe**  
The action of registering an Event Handler with a service bus.

**Subscriber**  
An event handler registered on a service bus.

**Sending a command or query**  
Asking a service bus to deliver a message to its handler.

**Publishing an event**  
Delivering an event to all it's subscribers.

**Raising an event**
Same as Publishing an event

> [!TIP]
> You always publish/Raise events. 
> Keeping Send separate from Publish in your mind is fundamental to understanding.