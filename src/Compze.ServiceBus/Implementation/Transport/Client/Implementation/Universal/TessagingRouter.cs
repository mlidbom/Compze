using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.ServiceBus.Implementation.TessageHandling.Dispatching;
using Compze.ServiceBus.Implementation.Transport.Abstractions;
using Compze.ServiceBus.Implementation.Transport.Client.Internal;
using Compze.ServiceBus.SystemCE.ThreadingCE;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.ServiceBus.Implementation.Transport.Client.Implementation.Universal;

static class TessagingTransportRegistrar
{
   internal static IComponentRegistrar TessagingTransport(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRouter.RegisterWith);
}

class TessagingRouter : ITessagingRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingRouter>().CreatedBy(
            (ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster, IInfrastructureQueryTransport infrastructureQueryTransport, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
               => new TessagingRouter(tessagesInFlightTracker, typeMap, serializer, transportMessagePoster, infrastructureQueryTransport, tessageStorage, taskRunner, exceptionReporter)));

   readonly IMonitor _monitor = IMonitor.New();
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMap _typeMap;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly IInfrastructureQueryTransport _infrastructureQueryTransport;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   bool _stopped;
   bool _disposed;

   readonly Dictionary<EndpointId, TessagingConnection> _connections = new();
   readonly Dictionary<Type, TessagingConnection> _tommandHandlerRoutes = new();
   readonly List<(Type TeventType, TessagingConnection Connection)> _teventSubscriberRoutes = [];
   readonly Dictionary<Type, IReadOnlyList<TessagingConnection>> _teventSubscriberRouteCache = new();

   TessagingRouter(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster, IInfrastructureQueryTransport infrastructureQueryTransport, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMap = typeMap;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _infrastructureQueryTransport = infrastructureQueryTransport;
      _tessageStorage = tessageStorage;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public async Task ConnectAsync(EndpointAddress remoteEndpointAddress)
   {
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new TessagingConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMap, _serializer, _transportMessagePoster, _infrastructureQueryTransport, _tessageStorage, _taskRunner, _exceptionReporter);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

      _monitor.Locked(() =>
      {
         AssertNotStopped();
         _connections.Add(connection.EndpointInformation.Id, connection);
         RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
      });
   }

   public void StartDelivery() => _monitor.Locked(() =>
   {
      foreach(var connection in _connections.Values)
         connection.StartDelivery();
   });

   public void StopDelivery() => _monitor.Locked(() =>
   {
      foreach(var connection in _connections.Values)
         connection.StopDelivery();
   });

   void RegisterRoutes(TessagingConnection connection, ISet<string> handledTypeIdStrings)
   {
      foreach(var typeIdString in handledTypeIdStrings)
      {
         var tessageType = _typeMap.GetId(typeIdString).Type;

         if(tessageType.Is<IExactlyOnceTevent>())
         {
            _teventSubscriberRoutes.Add((tessageType, connection));
         } else if(tessageType.Is<IExactlyOnceTommand>())
         {
            _tommandHandlerRoutes.Add(tessageType, connection);
         }
      }

      _teventSubscriberRouteCache.Clear();
   }

   public void Stop() => _monitor.Locked(() => _stopped = true);

   ContractAsserter AssertNotStopped() => State.Assert(!_stopped, () => "router is stopped");

   public ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      _monitor.Locked(() =>
         AssertNotStopped().__(() =>
            _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
               ? connection
               : throw new NoHandlerForTessageTypeException(tommand.GetType())));

   public IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent) =>
      _monitor.Locked(() =>
      {
         AssertNotStopped();
         var teventType = tevent.GetType();
         if(!_teventSubscriberRouteCache.TryGetValue(teventType, out var cached))
         {
            cached = _teventSubscriberRoutes
                    .Where(route => route.TeventType.IsInstanceOfType(tevent))
                    .Select(route => route.Connection)
                    .ToArray();
            _teventSubscriberRouteCache[teventType] = cached;
         }

         return cached;
      });

   public void Dispose() => _monitor.Locked(() =>
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
   });
}
