// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using Compze.SystemCE.ReflectionCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Messaging.Events;

/// <summary>
/// Calls all matching handlers in the order they were registered when an event is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
partial class CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> where TEvent : class, IEvent
{
   class RegistrationBuilder(CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> owner) : IEventHandlerRegistrar<TEvent>
   {
      readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _owner = owner;

      ///<summary>Registers a for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
      RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent => ForGenericEvent(handler);

      RegistrationBuilder ForWrapped<TWrapperEvent>(Action<TWrapperEvent> handler) where TWrapperEvent : IWrapperEvent<TEvent>
      {
         MessageTypeInspector.AssertValidForSubscription(typeof(TWrapperEvent));
         _owner._handlers.Add(new RegisteredWrappedHandler<TWrapperEvent>(handler));
         _owner._totalHandlers++;
         return this;
      }

      ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
      /// Useful for listening to generic events such as IAggregateCreatedEvent or IAggregateDeletedEvent
      /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
      /// </summary>
      RegistrationBuilder ForGenericEvent<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : IEvent
      {
         MessageTypeInspector.AssertValidForSubscription(typeof(THandledEvent));
         if(typeof(THandledEvent).Is<IWrapperEvent<IEvent>>()) throw new Exception($"Handlers of type {typeof(IWrapperEvent<>).Name} must be registered through the {nameof(ForWrapped)} method.");
         _owner._handlers.Add(new RegisteredHandler<THandledEvent>(handler));
         _owner._totalHandlers++;
         return this;
      }

      RegistrationBuilder BeforeHandlers(Action<TEvent> runBeforeHandlers)
      {
         //Urgent: fix this. Use the registered handler classes above
         _owner._runBeforeHandlers.Add(e => runBeforeHandlers(((IWrapperEvent<TEvent>)e).Event));
         _owner._totalHandlers++;
         return this;
      }

      RegistrationBuilder AfterHandlers(Action<TEvent> runAfterHandlers)
      {
         //Urgent: fix this
         _owner._runAfterHandlers.Add(e => runAfterHandlers(((IWrapperEvent<TEvent>)e).Event));
         return this;
      }

      RegistrationBuilder IgnoreUnhandled<T>() where T : IEvent
      {
         _owner._ignoredEvents.Add(typeof(T));                //Urgent: Remove?
         _owner._ignoredEvents.Add(typeof(IWrapperEvent<T>)); //urgent: Is this correct?
         _owner._totalHandlers++;
         return this;
      }

      IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.ForGenericEvent<THandledEvent>(Action<THandledEvent> handler) => ForGenericEvent(handler);
      IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers) => BeforeHandlers(e => runBeforeHandlers((THandledEvent)e));
      IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers) => AfterHandlers(e => runAfterHandlers((THandledEvent)e));
      IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.IgnoreUnhandled<THandledEvent>() => IgnoreUnhandled<THandledEvent>();
      IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.For<THandledEvent>(Action<THandledEvent> handler) => For(handler);
      IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.ForWrapped<TWrapperEvent>(Action<TWrapperEvent> handler) => ForWrapped(handler);
   }
}
