using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Typermedia.Client;

public static class TypermediaRouterRegistrar
{
   public static IComponentRegistrar TypermediaRouter(this IComponentRegistrar registrar)
      => registrar.Register(Client.TypermediaRouter.RegisterWith);
}

///<summary>Routes typermedia tessages to the endpoints that handle their types, over connections it maintains one of two ways:<br/>
/// an external client connects explicitly to the one address it knows (<see cref="ConnectAsync"/>), while an endpoint discovering<br/>
/// other endpoints dynamically has the router reconcile its connections against an <see cref="IEndpointRegistry"/>'s live<br/>
/// membership (<see cref="StartMaintainingConnectionsAsync"/>) — the same dynamic topology the Tessaging router runs on.</summary>
class TypermediaRouter : ITypermediaRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
            Singleton.For<ITypermediaRouter, ITypermediaRouting>().CreatedBy(
               (ITypeMap typeMap, ITypermediaTransport transport, IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport)
                  => new TypermediaRouter(typeMap, transport, endpointDiscoveryQueryTransport)));

   TypermediaRouter(ITypeMap typeMap, ITypermediaTransport transport, IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport)
   {
      _typeMap = typeMap;
      _transport = transport;
      _endpointDiscoveryQueryTransport = endpointDiscoveryQueryTransport;
   }

   ///<summary>How long a reconciliation pass waits for a registry change signal before running anyway. The signal makes announced<br/>
   /// and retracted endpoints propagate at signal latency; this periodic pass exists for what no signal can carry — a crashed<br/>
   /// process's addresses disappearing (a crash signals nothing) and retrying connections that failed.</summary>
   static readonly TimeSpan ReconcileLivenessInterval = TimeSpan.FromSeconds(1);

   readonly ITypeMap _typeMap;
   readonly ITypermediaTransport _transport;
   readonly IEndpointDiscoveryQueryTransport _endpointDiscoveryQueryTransport;
   readonly IMonitor _monitor = IMonitor.New();

   readonly CancellationTokenSource _reconcileLoopCancellation = new();
   Task? _reconcileLoop;
   IEndpointRegistry? _endpointRegistry;

   bool _running;
   readonly Dictionary<EndpointId, TypermediaConnection> _connections = new();
   //Copy-on-write: the route tables are replaced whole under the monitor and read lock-free on the routing hot path.
   IReadOnlyDictionary<Type, TypermediaConnection> _tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();

   public async Task ConnectAsync(EndpointAddress endpointAddress)
   {
      AssertRunning();
      var endpointInformation = await _endpointDiscoveryQueryTransport.GetAsync(new TypermediaEndpointInformationQuery(), endpointAddress).caf();
      var connection = new TypermediaConnection(endpointAddress, endpointInformation);

      _monitor.Locked(() =>
      {
         var endpointId = connection.EndpointInformation.Id;
         //The endpoint restarted at a new address (addresses are per-instance; identity is the EndpointId): replace the connection, and the rebuild below re-derives its routes.
         _connections[endpointId] = connection;
         RebuildRouteTables();
      });
   }

   public async Task StartMaintainingConnectionsAsync(IEndpointRegistry endpointRegistry)
   {
      AssertRunning();
      _endpointRegistry = endpointRegistry;
      await ReconcileConnectionsAsync().caf();
      _reconcileLoop = Task.Factory.StartNew(ReconcileLoop, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
   }

   ///<summary>Runs on its own thread (the registry wait blocks it): waits until the registry signals that membership may have<br/>
   /// changed — or <see cref="ReconcileLivenessInterval"/> elapses, covering what no signal carries — then reconciles.</summary>
   void ReconcileLoop()
   {
      while(!_reconcileLoopCancellation.IsCancellationRequested)
      {
         _endpointRegistry!.AwaitPossibleMembershipChange(ReconcileLivenessInterval, _reconcileLoopCancellation.Token);
         if(_reconcileLoopCancellation.IsCancellationRequested) return;

         try
         {
            ReconcileConnectionsAsync().WaitUnwrappingException();
         }
#pragma warning disable CA1031 //A reconciliation pass must never kill the loop; an unexpected failure is logged and the next pass retries.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            this.Log().Error(exception, "A typermedia connection reconciliation pass failed; the next pass retries.");
         }
      }
   }

   ///<summary>One reconciliation pass: connections converge on the registry's current membership.</summary>
   async Task ReconcileConnectionsAsync()
   {
      var desiredAddresses = _endpointRegistry!.ServerEndpointAddresses.ToHashSet();

      List<EndpointAddress> addressesToConnect = [];
      _monitor.Locked(() =>
      {
         if(!_running) return;
         var connectedAddresses = _connections.Values.Select(connection => connection.Address).ToHashSet();
         addressesToConnect = [..desiredAddresses.Where(address => !connectedAddresses.Contains(address))];

         //An endpoint whose address left the registry is gone (stopped, or crashed and pruned by liveness): its routes must not
         //survive - a typermedia tessage sent to a departed endpoint must fail loud as unhandled, not target a dead address.
         var departedConnections = _connections.Values.Where(connection => !desiredAddresses.Contains(connection.Address)).ToArray();
         if(departedConnections.Length == 0) return;

         foreach(var departedConnection in departedConnections)
            _connections.Remove(departedConnection.EndpointInformation.Id);
         RebuildRouteTables();
      });

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

   ///<summary>Re-derives the route tables from the current connections — membership changed, so routes derived from a dropped or<br/>
   /// replaced connection must not survive. Must be called under the monitor.</summary>
   void RebuildRouteTables()
   {
      var tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
      var tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();

      foreach(var connection in _connections.Values)
      {
         foreach(var typeIdString in connection.EndpointInformation.HandledTypermediaTypes)
         {
            var tessageType = _typeMap.GetId(typeIdString).Type;

            if(tessageType.Is<IAtMostOnceTypermediaTommand>())
            {
               tommandHandlerRoutes.Add(tessageType, connection);
            } else if(tessageType.Is<IRemotableTuery<object>>())
            {
               tueryHandlerRoutes.Add(tessageType, connection);
            }
            //Exactly-once types are the Tessaging router's to route.
         }
      }

      Interlocked.Exchange(ref _tommandHandlerRoutes, tommandHandlerRoutes);
      Interlocked.Exchange(ref _tueryHandlerRoutes, tueryHandlerRoutes);
   }

   TypermediaConnection ConnectionToHandlerFor(IAtMostOnceTypermediaTommand tommand) =>
      _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
         ? connection
         : throw new NoHandlerForTypermediaTypeException(tommand.GetType());

   TypermediaConnection ConnectionToHandlerFor<TTuery>(IRemotableTuery<TTuery> tuery) =>
      _tueryHandlerRoutes.TryGetValue(tuery.GetType(), out var connection)
         ? connection
         : throw new NoHandlerForTypermediaTypeException(tuery.GetType());

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      AssertRunning();
      var connection = ConnectionToHandlerFor(tommand);
      await _transport.PostAsync(tommand, connection.Address).caf();
   }

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand)
   {
      AssertRunning();
      var connection = ConnectionToHandlerFor(typermediaTommand);
      return await _transport.PostAsync(typermediaTommand, connection.Address).caf();
   }

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery)
   {
      AssertRunning();
      var connection = ConnectionToHandlerFor(tuery);
      return await _transport.GetAsync(tuery, connection.Address).caf();
   }

   public void Start() => State.Assert(!_running, () => "already running")
                                .__(_running = true);

   public void Stop() => AssertRunning().__(() =>
   {
      _running = false;
      _reconcileLoopCancellation.Cancel();
   });

   bool _disposed;

   public void Dispose()
   {
      if(_disposed) return;

      _disposed = true;
      if(_running)
      {
         Stop();
      }

      _reconcileLoopCancellation.Cancel();
      //Awaited outside the monitor: a reconciliation pass in flight takes the monitor to finish, so waiting for the loop while holding it would deadlock.
      _reconcileLoop?.WaitUnwrappingException();
      _reconcileLoopCancellation.Dispose();
   }

   Unit AssertRunning() => State.Assert(_running, () => $"The typermedia router is not running. An endpoint's router runs only when the endpoint declares the registry it discovers other endpoints through — DiscoverEndpointsThrough/ParticipateIn on its distributed-Typermedia feature; an external client composition starts it explicitly ({nameof(Start)} + {nameof(ConnectAsync)}).").__(unit);
}
