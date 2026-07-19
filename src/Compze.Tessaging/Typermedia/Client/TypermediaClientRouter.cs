using Compze.TypeIdentifiers;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Contracts;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Threading;

namespace Compze.Tessaging.Typermedia.Client;

static class TypermediaClientRouterRegistrar
{
   public static IComponentRegistrar TypermediaClientRouter(this IComponentRegistrar registrar)
      => registrar.Register(Client.TypermediaClientRouter.RegisterWith);
}

///<summary>An external client application's typermedia router: it connects explicitly to the one or more endpoint addresses<br/>
/// the client knows (<see cref="ConnectAsync"/>) and routes typermedia tessages to the endpoint that handles their types.<br/>
/// An <em>endpoint</em> navigating other endpoints' typermedia does not use this: its typermedia tessages route through the<br/>
/// endpoint's one router, which discovers endpoints dynamically and routes every tessage kind (see <c>TypermediaRouting</c>).</summary>
class TypermediaClientRouter : ITypermediaClientRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
            Singleton.For<ITypermediaClientRouter, ITypermediaRouting>().CreatedBy(
               (ITypeMap typeMap, ITypermediaTransport transport, IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport)
                  => new TypermediaClientRouter(typeMap, transport, endpointDiscoveryQueryTransport)));

   TypermediaClientRouter(ITypeMap typeMap, ITypermediaTransport transport, IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport)
   {
      _typeMap = typeMap;
      _transport = transport;
      _endpointDiscoveryQueryTransport = endpointDiscoveryQueryTransport;
   }

   readonly ITypeMap _typeMap;
   readonly ITypermediaTransport _transport;
   readonly IEndpointDiscoveryQueryTransport _endpointDiscoveryQueryTransport;
   readonly IMonitor _monitor = IMonitor.New();

   bool _running;
   readonly Dictionary<EndpointId, TypermediaConnection> _connections = new();
   //Copy-on-write: the route tables are replaced whole under the monitor and read lock-free on the routing hot path.
   IReadOnlyDictionary<Type, IReadOnlyList<TypermediaConnection>> _tommandHandlerRoutes = new Dictionary<Type, IReadOnlyList<TypermediaConnection>>();
   IReadOnlyDictionary<Type, IReadOnlyList<TypermediaConnection>> _tueryHandlerRoutes = new Dictionary<Type, IReadOnlyList<TypermediaConnection>>();

   public async Task ConnectAsync(EndpointAddress endpointAddress)
   {
      AssertRunning();
      var endpointInformation = await _endpointDiscoveryQueryTransport.GetAsync(new EndpointInformationQuery(), endpointAddress).caf();
      var connection = new TypermediaConnection(endpointAddress, endpointInformation);

      _monitor.Locked(() =>
      {
         var endpointId = connection.EndpointInformation.Id;
         //The endpoint restarted at a new address (addresses are per-instance; identity is the EndpointId): replace the connection, and the rebuild below re-derives its routes.
         _connections[endpointId] = connection;
         RebuildRouteTables();
      });
   }

   ///<summary>Re-derives the route tables from the current connections — membership changed, so routes derived from a replaced<br/>
   /// connection must not survive. Several connections advertising one type build a multi-entry route: a duplicate is a<br/>
   /// diagnosable send-time condition (<see cref="MultipleHandlersForTypermediaTypeException"/>), never a rebuild failure.<br/>
   /// The endpoint's one advertisement carries every tessage kind; a client routes only the typermedia kinds and the rest are<br/>
   /// simply not its business. Must be called under the monitor.</summary>
   void RebuildRouteTables()
   {
      var tommandHandlerRoutes = new Dictionary<Type, List<TypermediaConnection>>();
      var tueryHandlerRoutes = new Dictionary<Type, List<TypermediaConnection>>();

      foreach(var connection in _connections.Values)
      {
         foreach(var typeIdString in connection.EndpointInformation.HandledTessageTypes)
         {
            var tessageType = _typeMap.GetId(typeIdString).Type;

            if(tessageType.Is<IAtMostOnceTypermediaTommand>())
            {
               tommandHandlerRoutes.GetOrAdd(tessageType, () => []).Add(connection);
            } else if(tessageType.Is<IRemotableTuery<object>>())
            {
               tueryHandlerRoutes.GetOrAdd(tessageType, () => []).Add(connection);
            }
         }
      }

      Interlocked.Exchange(ref _tommandHandlerRoutes, tommandHandlerRoutes.ToDictionary(route => route.Key, route => (IReadOnlyList<TypermediaConnection>)route.Value));
      Interlocked.Exchange(ref _tueryHandlerRoutes, tueryHandlerRoutes.ToDictionary(route => route.Key, route => (IReadOnlyList<TypermediaConnection>)route.Value));
   }

   TypermediaConnection ConnectionToHandlerFor(IAtMostOnceTypermediaTommand tommand) => SingleRoutedConnectionFor(tommand.GetType(), _tommandHandlerRoutes);

   TypermediaConnection ConnectionToHandlerFor<TTuery>(IRemotableTuery<TTuery> tuery) => SingleRoutedConnectionFor(tuery.GetType(), _tueryHandlerRoutes);

   ///<summary>The one connection a typermedia tessage executes on: no route throws <see cref="NoHandlerForTypermediaTypeException"/>;<br/>
   /// several routes throw <see cref="MultipleHandlersForTypermediaTypeException"/> naming the endpoints — never a silent pick.<br/>
   /// Both throw immediately, deliberately: a client's connections change only by its own explicit connects, so unlike an<br/>
   /// endpoint's waiting sends there is no discovery to wait for.</summary>
   static TypermediaConnection SingleRoutedConnectionFor(Type tessageType, IReadOnlyDictionary<Type, IReadOnlyList<TypermediaConnection>> handlerRoutes) =>
      handlerRoutes.TryGetValue(tessageType, out var connections)
         ? connections.Count == 1
              ? connections[0]
              : throw MultipleHandlersForTypermediaTypeException.AmongTheClientsConnectedEndpoints(tessageType, [..connections.Select(connection => connection.EndpointInformation.Id)])
         : throw NoHandlerForTypermediaTypeException.BecauseTheClientIsConnectedToNoHandler(tessageType);

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

   public void Stop() => AssertRunning().__(() => _running = false);

   bool _disposed;

   public void Dispose()
   {
      if(_disposed) return;

      _disposed = true;
      if(_running)
      {
         Stop();
      }
   }

   Unit AssertRunning() => State.Assert(_running, () => $"The typermedia client router is not running: an external client composition starts it explicitly ({nameof(Start)} + {nameof(ConnectAsync)}).").__(unit);
}
