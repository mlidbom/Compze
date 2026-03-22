using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Typermedia.Client;

public static class TypermediaRouterRegistrar
{
   public static IComponentRegistrar TypermediaRouter(this IComponentRegistrar registrar)
      => registrar.Register(Client.TypermediaRouter.RegisterWith);
}

class TypermediaRouter : ITypermediaRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
            Singleton.For<ITypermediaRouter, ITypermediaRouting>().CreatedBy(
               (ITypeMapper typeMapper, ITypermediaTransport transport, IInfrastructureQueryTransport infrastructureQueryTransport)
                  => new TypermediaRouter(typeMapper, transport, infrastructureQueryTransport)));

   TypermediaRouter(ITypeMapper typeMapper, ITypermediaTransport transport, IInfrastructureQueryTransport infrastructureQueryTransport)
   {
      _typeMapper = typeMapper;
      _transport = transport;
      _infrastructureQueryTransport = infrastructureQueryTransport;
   }

   readonly ITypeMapper _typeMapper;
   readonly ITypermediaTransport _transport;
   readonly IInfrastructureQueryTransport _infrastructureQueryTransport;
   readonly IMonitor _monitor = IMonitor.New();

   bool _running;
   IReadOnlyDictionary<EndpointId, TypermediaConnection> _connections = new Dictionary<EndpointId, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();

   public async Task ConnectAsync(EndPointAddress endpointAddress)
   {
      AssertRunning();
      var endpointInformation = await _infrastructureQueryTransport.GetAsync(new TypermediaEndpointInformationQuery(), endpointAddress).caf();
      var connection = new TypermediaConnection(endpointAddress, endpointInformation);

      using(_monitor.TakeLock())
      {
         Interlocked.Exchange(ref _connections, _connections.AddToCopy(connection.EndpointInformation.Id, connection));

         //urgent: we can't have routes be discovered at startup based on the assumption that all endpoints are up...
         RegisterRoutes(connection, connection.EndpointInformation.HandledTypermediaTypes);
      } 
   }

   void RegisterRoutes(TypermediaConnection connection, ISet<string> handledTypeIdStrings)
   {
      var tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
      var tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
      foreach(var typeIdString in handledTypeIdStrings)
      {
         var tessageType = _typeMapper.FromPersistedTypeString(typeIdString);

         if(tessageType.Is<IAtMostOnceTypermediaTommand>())
         {
            tommandHandlerRoutes.Add(tessageType, connection);
         } else if(tessageType.Is<IRemotableTuery<object>>())
         {
            tueryHandlerRoutes.Add(tessageType, connection);
         }
         //Silently skip exactly-once types — those are handled by TessagingRouter
      }

      if(tommandHandlerRoutes.Count > 0)
         Interlocked.Exchange(ref _tommandHandlerRoutes, _tommandHandlerRoutes.AddRangeToCopy(tommandHandlerRoutes));

      if(tueryHandlerRoutes.Count > 0)
         Interlocked.Exchange(ref _tueryHandlerRoutes, _tueryHandlerRoutes.AddRangeToCopy(tueryHandlerRoutes));
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

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand)
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
   });

   bool _disposed;

   public void Dispose()
   {
      if(!_disposed)
      {
         _disposed = true;
         if(_running)
         {
            Stop();
         }
      }
   }

   Unit AssertRunning() => State.Assert(_running, () => "not running").__(unit);
}
