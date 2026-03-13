using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

static class TessagingTransportRegistrar
{
   internal static IComponentRegistrar TessagingTransport(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRouter.RegisterWith);
}

class TessagingRouter : ITessagingRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingRouter>().CreatedBy(
            (ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster, IInfrastructureQueryTransport infrastructureQueryTransport, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
               => new TessagingRouter(tessagesInFlightTracker, typeMapper, serializer, transportMessagePoster, infrastructureQueryTransport, tessageStorage, taskRunner, exceptionReporter)));

   readonly IMonitor _lock = IMonitor.New();
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly IInfrastructureQueryTransport _infrastructureQueryTransport;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   bool _stopped;

   IReadOnlyDictionary<EndpointId, TessagingConnection> _connections = new Dictionary<EndpointId, TessagingConnection>();
   IReadOnlyDictionary<Type, TessagingConnection> _tommandHandlerRoutes = new Dictionary<Type, TessagingConnection>();
   IReadOnlyList<(Type TeventType, TessagingConnection Connection)> _teventSubscriberRoutes = new List<(Type TeventType, TessagingConnection Connection)>();
   IReadOnlyDictionary<Type, IReadOnlyList<TessagingConnection>> _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<TessagingConnection>>();

   TessagingRouter(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster, IInfrastructureQueryTransport infrastructureQueryTransport, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _infrastructureQueryTransport = infrastructureQueryTransport;
      _tessageStorage = tessageStorage;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertNotStopped();
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new TessagingConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _transportMessagePoster, _infrastructureQueryTransport, _tessageStorage, _taskRunner, _exceptionReporter);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

      using(_lock.TakeLock())
      {
         _connections = _connections.AddToCopy(connection.EndpointInformation.Id, connection);
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
         _teventSubscriberRoutes = _teventSubscriberRoutes.AddRangeToCopy(teventSubscribers);
         _teventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<TessagingConnection>>();
      }

      if(tommandHandlerRoutes.Count > 0)
      {
         _tommandHandlerRoutes = _tommandHandlerRoutes.AddRangeToCopy(tommandHandlerRoutes);
      }
   }

   public void Stop() => _stopped = true;

   ContractAsserter AssertNotStopped() => State.Assert(!_stopped, () => "router is stopped");

   public ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      AssertNotStopped().__(() =>
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

      using(_lock.TakeLock())
      {
         _teventSubscriberRouteCache = _teventSubscriberRouteCache.AddToCopy(tevent.GetType(), subscriberConnections);
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
