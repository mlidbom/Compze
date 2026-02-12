using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing;

public class InboxConnectionRouter(ITypeMapper typeMapper)
{
   readonly IMonitorCE _monitor = IMonitorCE.WithDefaultTimeout();
   readonly ITypeMapper _typeMapper = typeMapper;

   IReadOnlyDictionary<Type, IInboxConnection> _tommandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
   IReadOnlyDictionary<Type, IInboxConnection> _tueryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
   IReadOnlyList<(Type TeventType, IInboxConnection Connection)> _teventSubscriberRoutes = new List<(Type TeventType, IInboxConnection Connection)>();
   IReadOnlyDictionary<Type, IReadOnlyList<IInboxConnection>> _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();

   public void RegisterRoutes(IInboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
   {
      var teventSubscribers = new List<(Type TeventType, IInboxConnection Connection)>();
      var tommandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
      var tueryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
      foreach(var typeId in handledTypeIds)
      {
         if(_typeMapper.TryGetType(typeId, out var tessageType))
         {
            if(tessageType.Is<IExactlyOnceTevent>())
            {
               teventSubscribers.Add((tessageType, inboxConnection));
            } else if(tessageType.Is<IRemotableTommand>())
            {
               tommandHandlerRoutes.Add(tessageType, inboxConnection);
            } else if(tessageType.Is<IRemotableTuery<object>>())
            {
               tueryHandlerRoutes.Add(tessageType, inboxConnection);
            } else
            {
               throw new Exception($"Type {typeId} is neither a remote tommand, tevent or tuery.");
            }
         }
      }

      using(_monitor.TakeUpdateLock())
      {
         if(teventSubscribers.Count > 0)
         {
            OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _teventSubscriberRoutes, teventSubscribers);
            _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();
         }

         if(tommandHandlerRoutes.Count > 0)
            OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tommandHandlerRoutes, tommandHandlerRoutes);

         if(tueryHandlerRoutes.Count > 0)
            OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tueryHandlerRoutes, tueryHandlerRoutes);
      }
   }

   public IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
         ? connection
         : throw new NoHandlerForTessageTypeException(tommand.GetType());

   public IInboxConnection ConnectionToHandlerFor<TTuery>(IRemotableTuery<TTuery> tuery) =>
      _tueryHandlerRoutes.TryGetValue(tuery.GetType(), out var connection)
         ? connection
         : throw new NoHandlerForTessageTypeException(tuery.GetType());

   public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent)
   {
      if(_teventSubscriberRouteCache.TryGetValue(tevent.GetType(), out var connection)) return connection;

      var subscriberConnections = _teventSubscriberRoutes
                                 .Where(route => route.TeventType.IsInstanceOfType(tevent))
                                 .Select(route => route.Connection)
                                 .ToArray();

      using(_monitor.TakeUpdateLock())
      {
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _teventSubscriberRouteCache, tevent.GetType(), subscriberConnections);
      }

      return subscriberConnections;
   }
}