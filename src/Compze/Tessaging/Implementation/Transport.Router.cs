using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time;
using Compze.Tessaging.Implementation.MessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation;

partial class TransportClient
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
               OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _eventSubscriberRoutes, eventSubscribers);
               _eventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();
            }

            if(commandHandlerRoutes.Count > 0)
               OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _commandHandlerRoutes, commandHandlerRoutes);

            if(queryHandlerRoutes.Count > 0)
               OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _queryHandlerRoutes, queryHandlerRoutes);
         }
      }

      internal IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
         _commandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForMessageTypeException(tommand.GetType());

      internal IInboxConnection ConnectionToHandlerFor<TQuery>(IRemotableTuery<TQuery> tuery) =>
         _queryHandlerRoutes.TryGetValue(tuery.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForMessageTypeException(tuery.GetType());

      internal IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent)
      {
         if(_eventSubscriberRouteCache.TryGetValue(tevent.GetType(), out var connection)) return connection;

         var subscriberConnections = _eventSubscriberRoutes
                                    .Where(route => route.EventType.IsInstanceOfType(tevent))
                                    .Select(route => route.Connection)
                                    .ToArray();

         using(_monitor.TakeUpdateLock())
         {
             OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _eventSubscriberRouteCache, tevent.GetType(), subscriberConnections);
         }

         return subscriberConnections;
      }

      static bool IsRemoteCommand(Type type) => typeof(IRemotableTommand).IsAssignableFrom(type);
      static bool IsRemoteEvent(Type type) => typeof(IExactlyOnceTevent).IsAssignableFrom(type);
      static bool IsRemoteQuery(Type type) => typeof(IRemotableTuery<object>).IsAssignableFrom(type);
   }
}