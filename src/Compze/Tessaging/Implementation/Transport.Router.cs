using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
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

      IReadOnlyDictionary<Type, IInboxConnection> _tommandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
      IReadOnlyDictionary<Type, IInboxConnection> _tueryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
      IReadOnlyList<(Type EventType, IInboxConnection Connection)> _eventSubscriberRoutes = new List<(Type EventType, IInboxConnection Connection)>();
      IReadOnlyDictionary<Type, IReadOnlyList<IInboxConnection>> _eventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();

      internal void RegisterRoutes(IInboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
      {
         var eventSubscribers = new List<(Type EventType, IInboxConnection Connection)>();
         var tommandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
         var tueryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
         foreach(var typeId in handledTypeIds)
         {
            if(_typeMapper.TryGetType(typeId, out var tessageType))
            {
               if(IsRemoteEvent(tessageType))
               {
                  eventSubscribers.Add((tessageType, inboxConnection));
               } else if(IsRemoteTommand(tessageType))
               {
                  tommandHandlerRoutes.Add(tessageType, inboxConnection);
               } else if(IsRemoteTuery(tessageType))
               {
                  tueryHandlerRoutes.Add(tessageType, inboxConnection);
               } else
               {
                  throw new Exception($"Type {typeId} is neither a remote tommand, event or tuery.");
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

            if(tommandHandlerRoutes.Count > 0)
               OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tommandHandlerRoutes, tommandHandlerRoutes);

            if(tueryHandlerRoutes.Count > 0)
               OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tueryHandlerRoutes, tueryHandlerRoutes);
         }
      }

      internal IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
         _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForTessageTypeException(tommand.GetType());

      internal IInboxConnection ConnectionToHandlerFor<TTuery>(IRemotableTuery<TTuery> tuery) =>
         _tueryHandlerRoutes.TryGetValue(tuery.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForTessageTypeException(tuery.GetType());

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

      static bool IsRemoteTommand(Type type) => typeof(IRemotableTommand).IsAssignableFrom(type);
      static bool IsRemoteEvent(Type type) => typeof(IExactlyOnceTevent).IsAssignableFrom(type);
      static bool IsRemoteTuery(Type type) => typeof(IRemotableTuery<object>).IsAssignableFrom(type);
   }
}