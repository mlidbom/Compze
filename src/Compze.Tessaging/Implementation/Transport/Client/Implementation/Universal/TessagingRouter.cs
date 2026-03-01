using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Contracts;
using Compze.Functional;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public class TessagingRouter : ITessagingRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingRouter>().CreatedBy(
            (ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
               => new TessagingRouter(tessagesInFlightTracker, typeMapper, serializer, transportMessagePoster, tessageStorage, taskRunner, exceptionReporter)));

   readonly IMonitor _monitor = IMonitor.WithDefaultTimeout();
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   bool _stopped;

   IReadOnlyDictionary<EndpointId, TessagingConnection> _connections = new Dictionary<EndpointId, TessagingConnection>();
   IReadOnlyDictionary<Type, TessagingConnection> _tommandHandlerRoutes = new Dictionary<Type, TessagingConnection>();
   IReadOnlyList<(Type TeventType, TessagingConnection Connection)> _teventSubscriberRoutes = new List<(Type TeventType, TessagingConnection Connection)>();
   IReadOnlyDictionary<Type, IReadOnlyList<TessagingConnection>> _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<TessagingConnection>>();

   TessagingRouter(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _tessageStorage = tessageStorage;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertNotStopped();
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new TessagingConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _transportMessagePoster, _tessageStorage, _taskRunner, _exceptionReporter);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

      using(_monitor.TakeLock())
      {
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _connections, connection.EndpointInformation.Id, connection);
         RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
      }
   }

   public void StartDelivery()
   {
      foreach(var connection in _connections.Values)
         connection.StartDelivery();
   }

   public void StopDelivery()
   {
      foreach(var connection in _connections.Values)
         connection.StopDelivery();
   }

   void RegisterRoutes(TessagingConnection connection, ISet<TypeId> handledTypeIds)
   {
      var teventSubscribers = new List<(Type TeventType, TessagingConnection Connection)>();
      var tommandHandlerRoutes = new Dictionary<Type, TessagingConnection>();
      foreach(var typeId in handledTypeIds)
      {
         if(_typeMapper.TryGetType(typeId, out var tessageType))
         {
            if(tessageType.Is<IExactlyOnceTevent>())
            {
               teventSubscribers.Add((tessageType, connection));
            } else if(tessageType.Is<IExactlyOnceTommand>())
            {
               tommandHandlerRoutes.Add(tessageType, connection);
            }
            //Silently skip typermedia types — those are handled by TypermediaRouter
         }
      }

      if(teventSubscribers.Count > 0)
      {
         OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _teventSubscriberRoutes, teventSubscribers);
         _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<TessagingConnection>>();
      }

      if(tommandHandlerRoutes.Count > 0)
         OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tommandHandlerRoutes, tommandHandlerRoutes);
   }

   public void Stop() => _stopped = true;

   ContractAsserter AssertNotStopped() => Contract.State.Assert(!_stopped, () => "router is stopped");

   public ITessagingInboxConnection ConnectionForEndpoint(EndpointId endpointId) =>
      AssertNotStopped()._then(() =>
         _connections.TryGetValue(endpointId, out var connection)
            ? connection
            : throw new InvalidOperationException($"No connection found for endpoint {endpointId}"));

   public ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      AssertNotStopped()._then(() =>
         _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
            ? connection
            : throw new NoHandlerForTessageTypeException(tommand.GetType()));

   public IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent)
   {
      AssertNotStopped();
      if(_teventSubscriberRouteCache.TryGetValue(tevent.GetType(), out var cached)) return cached;

      var subscriberConnections = _teventSubscriberRoutes
                                 .Where(route => route.TeventType.IsInstanceOfType(tevent))
                                 .Select(route => route.Connection)
                                 .ToArray();

      using(_monitor.TakeLock())
      {
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _teventSubscriberRouteCache, tevent.GetType(), subscriberConnections);
      }

      return subscriberConnections;
   }

   bool _disposed;

   public void Dispose()
   {
      if(!_disposed)
      {
         _disposed = true;
         if(!_stopped)
         {
            Stop();
         }

         _connections.Values.DisposeAll();
      }
   }
}
