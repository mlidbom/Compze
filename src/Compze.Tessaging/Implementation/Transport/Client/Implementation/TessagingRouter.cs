using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation;

static class TessagingTransportRegistrar
{
   internal static IComponentRegistrar TessagingTransport(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRouter.RegisterWith);
}

class TessagingRouter : ITessagingRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingRouter>().CreatedBy(
            (ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, ITessagingSerializer serializer, ITransportMessagePoster transportMessagePoster, IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
               => new TessagingRouter(tessagesInFlightTracker, typeMap, serializer, transportMessagePoster, endpointDiscoveryQueryTransport, tessageStorage, taskRunner, exceptionReporter)));

   static readonly TimeSpan ReconcileInterval = TimeSpan.FromSeconds(1);

   readonly IMonitor _monitor = IMonitor.New();
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMap _typeMap;
   readonly ITessagingSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly IEndpointDiscoveryQueryTransport _endpointDiscoveryQueryTransport;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   readonly CancellationTokenSource _reconcileLoopCancellation = new();
   Task? _reconcileLoop;
   IEndpointRegistry? _endpointRegistry;
   EndpointAddress? _ownInboxAddress;

   bool _deliveryStarted;
   bool _stopped;
   bool _disposed;

   readonly Dictionary<EndpointId, TessagingConnection> _connections = new();
   readonly Dictionary<Type, TessagingConnection> _tommandHandlerRoutes = new();
   readonly List<(Type TeventType, TessagingConnection Connection)> _teventSubscriberRoutes = [];
   readonly Dictionary<Type, IReadOnlyList<TessagingConnection>> _teventSubscriberRouteCache = new();

   TessagingRouter(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, ITessagingSerializer serializer, ITransportMessagePoster transportMessagePoster, IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport, Outbox.Outbox.ITessageStorage tessageStorage, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMap = typeMap;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _endpointDiscoveryQueryTransport = endpointDiscoveryQueryTransport;
      _tessageStorage = tessageStorage;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public async Task StartMaintainingConnectionsAsync(IEndpointRegistry endpointRegistry, EndpointAddress ownInboxAddress)
   {
      _endpointRegistry = endpointRegistry;
      _ownInboxAddress = ownInboxAddress;
      await ReconcileConnectionsAsync().caf();
      _reconcileLoop = Task.Run(ReconcileLoopAsync);
   }

   async Task ReconcileLoopAsync()
   {
      while(!_reconcileLoopCancellation.IsCancellationRequested)
      {
         try
         {
            await Task.Delay(ReconcileInterval, _reconcileLoopCancellation.Token).caf();
         }
         catch(OperationCanceledException) //Stopping: the canceled delay is the shutdown signal itself.
         {
            return;
         }

         try
         {
            await ReconcileConnectionsAsync().caf();
         }
#pragma warning disable CA1031 //A reconciliation pass must never kill the loop; an unexpected failure is reported and surfaces the way every background exception does.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            _exceptionReporter.ReportException(exception);
         }
      }
   }

   ///<summary>One reconciliation pass: connections converge on the registry's current membership (plus our own inbox — scheduled<br/>
   /// tommands dispatch to ourselves over the remote protocol for the delivery guarantees).</summary>
   async Task ReconcileConnectionsAsync()
   {
      var desiredAddresses = _endpointRegistry!.ServerEndpointAddresses.ToHashSet();
      desiredAddresses.Add(_ownInboxAddress!);

      List<EndpointAddress> addressesToConnect = [];
      List<TessagingConnection> connectionsToDrop = [];
      _monitor.Locked(() =>
      {
         if(_stopped) return;
         var connectedAddresses = _connections.Values.Select(connection => connection.RemoteAddress).ToHashSet();
         addressesToConnect = [..desiredAddresses.Where(address => !connectedAddresses.Contains(address))];
         connectionsToDrop = [.._connections.Values.Where(connection => !desiredAddresses.Contains(connection.RemoteAddress))];
      });

      //An endpoint whose address left the registry is gone (stopped, or crashed and pruned by liveness). Its undelivered
      //tessages stay in the outbox's storage - exactly-once means they wait for the endpoint's return, and the connection
      //created when it returns loads them, in send order.
      connectionsToDrop.ForEach(DropConnection);

      foreach(var address in addressesToConnect)
      {
         try
         {
            await ConnectAsync(address).caf();
         }
#pragma warning disable CA1031 //A listed address may have just crashed (the liveness filter has not pruned it yet) or not be reachable yet - topology churn, not a bug. The next pass retries; a persistent failure keeps being logged.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            this.Log().Warning(exception, $"Connecting to registered endpoint address {address.Uri} failed; will retry on the next reconciliation pass.");
         }
      }
   }

   void DropConnection(TessagingConnection connection) => _monitor.Locked(() =>
   {
      _connections.Remove(connection.EndpointInformation.Id);
      RebuildRoutes();
      connection.Dispose();
   });

   async Task ConnectAsync(EndpointAddress remoteEndpointAddress)
   {
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new TessagingConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMap, _serializer, _transportMessagePoster, _endpointDiscoveryQueryTransport, _tessageStorage, _taskRunner, _exceptionReporter);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

      _monitor.Locked(() =>
      {
         if(_stopped) //A reconciliation pass racing shutdown: the router is no longer connecting to anyone.
         {
            connection.Dispose();
            return;
         }

         var endpointId = connection.EndpointInformation.Id;
         if(_connections.TryGetValue(endpointId, out var existingConnection))
         {
            //The endpoint restarted at a new address (addresses are per-instance; identity is the EndpointId): replace the
            //connection. The new connection loads the endpoint's undelivered backlog when its delivery starts, so the
            //backlog follows the endpoint to its new address.
            existingConnection.Dispose();
            _connections[endpointId] = connection;
            RebuildRoutes();
         }
         else
         {
            _connections.Add(endpointId, connection);
            RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
         }

         if(_deliveryStarted) connection.StartDelivery();
      });
   }

   public void StartDelivery() => _monitor.Locked(() =>
   {
      _deliveryStarted = true;
      foreach(var connection in _connections.Values)
         connection.StartDelivery();
   });

   public void StopDelivery()
   {
      _reconcileLoopCancellation.Cancel();
      _monitor.Locked(() =>
      {
         _deliveryStarted = false;
         foreach(var connection in _connections.Values)
            connection.StopDelivery();
      });
   }

   ///<summary>Rebuilds the route tables from the current connections — membership changed, so routes derived from a dropped or replaced connection must not survive.</summary>
   void RebuildRoutes()
   {
      _tommandHandlerRoutes.Clear();
      _teventSubscriberRoutes.Clear();
      _teventSubscriberRouteCache.Clear();
      foreach(var connection in _connections.Values)
         RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
   }

   void RegisterRoutes(TessagingConnection connection, ISet<string> handledTypeIdStrings)
   {
      foreach(var typeIdString in handledTypeIdStrings)
      {
         var tessageType = _typeMap.GetId(typeIdString).Type;

         if(tessageType.Is<ITevent>())
         {
            //An advertised tevent subscription is a wrapper type; covariance answers whether the tevents it matches may travel remotely at all.
            //Which delivery leg a matching tevent travels is not routing's concern - the published tevent's own type decides that (see ITeventPublisher).
            if(tessageType.Is<IPublisherTevent<IRemotableTevent>>())
            {
               _teventSubscriberRoutes.Add((tessageType, connection));
            }
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

   public IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) =>
      _monitor.Locked(() =>
      {
         AssertNotStopped();
         var wrapperTeventType = wrappedTevent.GetType();
         if(_teventSubscriberRouteCache.TryGetValue(wrapperTeventType, out var cached)) return cached;

         cached = [.._teventSubscriberRoutes
                    .Where(route => route.TeventType.IsInstanceOfType(wrappedTevent))
                    .Select(route => route.Connection)
                    .Distinct()]; //An endpoint is one subscriber however many of its advertised subscriptions match: it receives the tevent once and its own registry fans out to every matching handler.
         _teventSubscriberRouteCache[wrapperTeventType] = cached;
         return cached;
      });

   public void Dispose()
   {
      var alreadyDisposed = false;
      _monitor.Locked(() =>
      {
         alreadyDisposed = _disposed;
         _disposed = true;
         _stopped = true;
      });
      if(alreadyDisposed) return;

      _reconcileLoopCancellation.Cancel();
      //Awaited outside the monitor: a reconciliation pass in flight takes the monitor to finish, so waiting for the loop while holding it would deadlock.
      _reconcileLoop?.WaitUnwrappingException();
      _reconcileLoopCancellation.Dispose();

      _monitor.Locked(() => _connections.Values.DisposeAll());
   }
}
