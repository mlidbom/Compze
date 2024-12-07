using System.Threading;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Compze.Messaging;

static class WrapperEvent
{
   // Todo: The fact that we can wrap like this, without the types of the wrapping events, does that not also mean that we could, eventually, receive events on the bus without having the type information for all the wrapping events to deserialize to?
   // Note the eventually though! This is not a priority, but certainly something to keep in mind. If we can dig out just the inner event and wrap it like this, a listening endpoint need only know
   // the types for the inner event that it listens to, not the types in which it is wrapped. Just a heads up so we don't remove this strange code when we implement aggregates more cleanly. This still has great potential...
   public static IWrapperEvent<IEvent> WrapEvent(IEvent theEvent) =>
      WrapperEventImplementationGenerator.ConstructorFor(theEvent.GetType()).Invoke(theEvent);
}

public class WrapperEvent<TEventInterface>(TEventInterface @event) : IWrapperEvent<TEventInterface>
   where TEventInterface : IEvent
{
   public TEventInterface Event { get; } = @event;
}