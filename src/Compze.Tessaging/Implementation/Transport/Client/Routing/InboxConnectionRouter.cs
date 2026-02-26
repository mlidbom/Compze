using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Contracts;
using Compze.Functional;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing;

public class InboxConnectionRouter(ITypeMapper typeMapper)
{
   readonly IMonitorCE _monitor = IMonitorCE.WithDefaultTimeout();
   readonly ITypeMapper _typeMapper = typeMapper;
   bool _stopped;

   public void Stop() => _stopped = true;

   unit AssertNotStopped() => Contract.State.Assert(!_stopped, () => "router is stopped")._then(unit.Value);

   IReadOnlyDictionary<Type, RemoteEndpointConnection> _tommandHandlerRoutes = new Dictionary<Type, RemoteEndpointConnection>();
   IReadOnlyDictionary<Type, RemoteEndpointConnection> _tueryHandlerRoutes = new Dictionary<Type, RemoteEndpointConnection>();
   IReadOnlyList<(Type TeventType, RemoteEndpointConnection Connection)> _teventSubscriberRoutes = new List<(Type TeventType, RemoteEndpointConnection Connection)>();
   IReadOnlyDictionary<Type, IReadOnlyList<RemoteEndpointConnection>> _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<RemoteEndpointConnection>>();

   public void RegisterRoutes(RemoteEndpointConnection connection, ISet<TypeId> handledTypeIds)
   {
      var teventSubscribers = new List<(Type TeventType, RemoteEndpointConnection Connection)>();
      var tommandHandlerRoutes = new Dictionary<Type, RemoteEndpointConnection>();
      var tueryHandlerRoutes = new Dictionary<Type, RemoteEndpointConnection>();
      foreach(var typeId in handledTypeIds)
      {
         if(_typeMapper.TryGetType(typeId, out var tessageType))
         {
            if(tessageType.Is<IExactlyOnceTevent>())
            {
               teventSubscribers.Add((tessageType, connection));
            } else if(tessageType.Is<IRemotableTommand>())
            {
               tommandHandlerRoutes.Add(tessageType, connection);
            } else if(tessageType.Is<IRemotableTuery<object>>())
            {
               tueryHandlerRoutes.Add(tessageType, connection);
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
            _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<RemoteEndpointConnection>>();
         }

         if(tommandHandlerRoutes.Count > 0)
            OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tommandHandlerRoutes, tommandHandlerRoutes);

         if(tueryHandlerRoutes.Count > 0)
            OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tueryHandlerRoutes, tueryHandlerRoutes);
      }
   }

   public RemoteEndpointConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      AssertNotStopped()._then(() =>
         _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForTessageTypeException(tommand.GetType()));

   public RemoteEndpointConnection ConnectionToHandlerFor<TTuery>(IRemotableTuery<TTuery> tuery) =>
      AssertNotStopped()._then(() =>
         _tueryHandlerRoutes.TryGetValue(tuery.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForTessageTypeException(tuery.GetType()));

   public IReadOnlyList<RemoteEndpointConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent)
   {
      AssertNotStopped();
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