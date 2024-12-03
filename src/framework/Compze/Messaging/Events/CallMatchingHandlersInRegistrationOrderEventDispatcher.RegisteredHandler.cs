// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using Compze.SystemCE.ReflectionCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Messaging.Events;

/// <summary>
/// Calls all matching handlers in the order they were registered when an event is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
public partial class CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> : IMutableEventDispatcher<TEvent>
   where TEvent : class, IEvent
{
   abstract class RegisteredHandler
   {
      internal abstract Action<IEvent>? TryCreateHandlerFor(Type eventType);
   }

   class RegisteredHandler<THandledEvent>(Action<THandledEvent> handler) : RegisteredHandler where THandledEvent : IEvent
   {
      //Since handler has specified no preference for wrapper type the most generic of all will do and any wrapped event containing a matching event should be dispatched to this handler.
      readonly Action<THandledEvent> _handler = handler;

      internal override Action<IEvent>? TryCreateHandlerFor(Type eventType)
      {
         if(typeof(THandledEvent).IsAssignableFrom(eventType))
         {
            return @event => _handler((THandledEvent)@event);
         } else if(eventType.Is<IWrapperEvent<THandledEvent>>())
         {
            return @event => _handler(((IWrapperEvent<THandledEvent>)@event).Event);
         } else
         {
            return null;
         }
      }
   }

   class RegisteredWrappedHandler<THandledWrapperEvent>(Action<THandledWrapperEvent> handler) : RegisteredHandler where THandledWrapperEvent : IWrapperEvent<IEvent>
   {
      readonly Action<THandledWrapperEvent> _handler = handler;

      internal override Action<IEvent>? TryCreateHandlerFor(Type eventType) =>
         typeof(THandledWrapperEvent).IsAssignableFrom(eventType)
            ? @event => _handler((THandledWrapperEvent)@event)
            : null;
   }
}