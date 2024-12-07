// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Persistence.EventStore;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Messaging.Events;

/// <summary>
/// Calls all matching handlers in the order they were registered when an event is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
partial class CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> : IMutableEventDispatcher<TEvent>
   where TEvent : class, IEvent
{
   readonly List<RegisteredHandler> _handlers = [];
   readonly List<Action<object>> _runBeforeHandlers = [];
   readonly List<Action<object>> _runAfterHandlers = [];
   readonly HashSet<Type> _ignoredEvents = [];
   int _totalHandlers;
   Dictionary<Type, Action<IEvent>[]> _typeToHandlerCache = new();
   int _cachedTotalHandlers;
   // ReSharper disable once StaticMemberInGenericType
   static readonly Action<IEvent>[] NullHandlerList = [];

   public IEventHandlerRegistrar<TEvent> Register() => new RegistrationBuilder(this);


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

   public bool Handles(IAggregateEvent @event) => GetHandlers(@event.GetType(), validateHandlerExists: false).Any();

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
}
