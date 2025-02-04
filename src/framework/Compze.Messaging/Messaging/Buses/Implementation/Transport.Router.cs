﻿using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Refactoring.Naming;
using Compze.SystemCE.ThreadingCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Messaging.Buses.Implementation;

partial class Transport
{
   class Router(ITypeMapper typeMapper)
   {
      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
      readonly ITypeMapper _typeMapper = typeMapper;

      IReadOnlyDictionary<Type, IInboxConnection> _commandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
      IReadOnlyDictionary<Type, IInboxConnection> _queryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
      IReadOnlyList<(Type EventType, IInboxConnection Connection)> _eventSubscriberRoutes = new List<(Type EventType, IInboxConnection Connection)>();
      IReadOnlyDictionary<Type, IReadOnlyList<IInboxConnection>> _eventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();

      internal void RegisterRoutes(IInboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
      {
         var eventSubscribers = new List<(Type EventType, IInboxConnection Connection)>();
         var commandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
         var queryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
         foreach(var typeId in handledTypeIds)
         {
            if(_typeMapper.TryGetType(typeId, out var messageType))
            {
               if(IsRemoteEvent(messageType))
               {
                  eventSubscribers.Add((messageType, inboxConnection));
               } else if(IsRemoteCommand(messageType))
               {
                  commandHandlerRoutes.Add(messageType, inboxConnection);
               } else if(IsRemoteQuery(messageType))
               {
                  queryHandlerRoutes.Add(messageType, inboxConnection);
               } else
               {
                  throw new Exception($"Type {typeId} is neither a remote command, event or query.");
               }
            }
         }

         using(_monitor.TakeUpdateLock())
         {
            if(eventSubscribers.Count > 0)
            {
               ThreadSafe.AddRangeToCopyAndReplace(ref _eventSubscriberRoutes, eventSubscribers);
               _eventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();
            }

            if(commandHandlerRoutes.Count > 0)
               ThreadSafe.AddRangeToCopyAndReplace(ref _commandHandlerRoutes, commandHandlerRoutes);

            if(queryHandlerRoutes.Count > 0)
               ThreadSafe.AddRangeToCopyAndReplace(ref _queryHandlerRoutes, queryHandlerRoutes);
         }
      }

      internal IInboxConnection ConnectionToHandlerFor(IRemotableCommand command) =>
         _commandHandlerRoutes.TryGetValue(command.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForMessageTypeException(command.GetType());

      internal IInboxConnection ConnectionToHandlerFor<TQuery>(IRemotableQuery<TQuery> query) =>
         _queryHandlerRoutes.TryGetValue(query.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForMessageTypeException(query.GetType());

      internal IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceEvent @event)
      {
         if(_eventSubscriberRouteCache.TryGetValue(@event.GetType(), out var connection)) return connection;

         var subscriberConnections = _eventSubscriberRoutes
                                    .Where(route => route.EventType.IsInstanceOfType(@event))
                                    .Select(route => route.Connection)
                                    .ToArray();


         ThreadSafe.AddToCopyAndReplace(ref _eventSubscriberRouteCache, @event.GetType(), subscriberConnections);
         return subscriberConnections;
      }

      static bool IsRemoteCommand(Type type) => typeof(IRemotableCommand).IsAssignableFrom(type);
      static bool IsRemoteEvent(Type type) => typeof(IExactlyOnceEvent).IsAssignableFrom(type);
      static bool IsRemoteQuery(Type type) => typeof(IRemotableQuery<object>).IsAssignableFrom(type);
   }
}