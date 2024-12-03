// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Persistence.EventStore;
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

   class RegisteredHandler<THandledEvent>(Action<THandledEvent> handler) : RegisteredHandler
      where THandledEvent : IEvent
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

   class RegisteredWrappedHandler<THandledWrapperEvent>(Action<THandledWrapperEvent> handler) : RegisteredHandler
      where THandledWrapperEvent : IWrapperEvent<IEvent>
   {
      readonly Action<THandledWrapperEvent> _handler = handler;

      internal override Action<IEvent>? TryCreateHandlerFor(Type eventType) =>
         typeof(THandledWrapperEvent).IsAssignableFrom(eventType)
            ? @event => _handler((THandledWrapperEvent)@event)
            : null;
   }

   readonly List<RegisteredHandler> _handlers = [];

   readonly List<Action<object>> _runBeforeHandlers = [];
   readonly List<Action<object>> _runAfterHandlers = [];
   readonly HashSet<Type> _ignoredEvents = [];
   int _totalHandlers;

   ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
   internal IEventHandlerRegistrar<TEvent> RegisterHandlers() => new RegistrationBuilder(this);

   public IEventHandlerRegistrar<TEvent> Register() => new RegistrationBuilder(this);

   Dictionary<Type, Action<IEvent>[]> _typeToHandlerCache = new();
   int _cachedTotalHandlers;
   // ReSharper disable once StaticMemberInGenericType
   static readonly Action<IEvent>[] NullHandlerList = [];

   Action<IEvent>[] GetHandlers(Type type, bool validateHandlerExists = true)
   {
      if(_cachedTotalHandlers != _totalHandlers)
      {
         _cachedTotalHandlers = _totalHandlers;
         _typeToHandlerCache = new Dictionary<Type, Action<IEvent>[]>();
      }

      if(_typeToHandlerCache.TryGetValue(type, out var arrayResult))
      {
         return arrayResult;
      }

      var result = new List<Action<IEvent>>();
      var hasFoundHandler = false;

      // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
      foreach(var registeredHandler in _handlers)
      {
         var handler = registeredHandler.TryCreateHandlerFor(type);
         if(handler != null)
         {
            if(!hasFoundHandler)
            {
               result.AddRange(_runBeforeHandlers);
               hasFoundHandler = true;
            }

            result.Add(handler);
         }
      }

      if(hasFoundHandler)
      {
         result.AddRange(_runAfterHandlers);
      } else
      {
         if(validateHandlerExists && !_ignoredEvents.Any(ignoredEventType => ignoredEventType.IsAssignableFrom(type)))
         {
            throw new EventUnhandledException(GetType(), type);
         }

         return _typeToHandlerCache[type] = NullHandlerList;
      }

      return _typeToHandlerCache[type] = result.ToArray();
   }

   public void Dispatch(TEvent evt)
   {
      //Urgent: Wrapping here seems arguable at best.
      var wrapped = evt as IWrapperEvent<IEvent>
                 ?? WrapperEvent.WrapEvent((IEvent)evt);

      var handlers = GetHandlers(wrapped.GetType());
      for(var i = 0; i < handlers.Length; i++)
      {
         handlers[i](wrapped);
      }
   }

   public bool HandlesEvent<THandled>() => GetHandlers(typeof(THandled), validateHandlerExists: false).Any();
   public bool Handles(IAggregateEvent @event) => GetHandlers(@event.GetType(), validateHandlerExists: false).Any();
}

public class EventUnhandledException(Type handlerType, Type eventType) : Exception($"{handlerType} does not handle nor ignore incoming event {eventType}");