using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Contracts;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Typermedia;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public static class TransportRegistrar
{
   public static IComponentRegistrar TypermediaTransport(this IComponentRegistrar registrar)
      => registrar.Register(TypermediaRouter.RegisterWith);

   internal static IComponentRegistrar TessagingTransport(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRouter.RegisterWith);
}

class TypermediaRouter : ITypermediaRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
            Singleton.For<ITypermediaRouter, ITypermediaRouting>().CreatedBy(
               (ITypeMapper typeMapper, ITypermediaTransport transport)
                  => new TypermediaRouter(typeMapper, transport)));

   TypermediaRouter(ITypeMapper typeMapper, ITypermediaTransport transport)
   {
      _typeMapper = typeMapper;
      _transport = transport;
   }

   readonly ITypeMapper _typeMapper;
   readonly ITypermediaTransport _transport;
   readonly IMonitor _monitor = IMonitor.New();

   bool _running;
   IReadOnlyDictionary<EndpointId, TypermediaConnection> _connections = new Dictionary<EndpointId, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();

   async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
      var endpointInformation = await _transport.GetAsync(new TessageTypesInternal.EndpointInformationTuery(), remoteEndpointAddress).caf();
      var connection = new TypermediaConnection(remoteEndpointAddress, endpointInformation);

      using(_monitor.TakeLock())
      {
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _connections, connection.EndpointInformation.Id, connection);

         //urgent: we can't have routes be discovered at startup based on the assumption that all endpoints are up...
         RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
      }
   }

   public async Task DiscoverAndConnectAsync(EndPointAddress seedAddress)
   {
      AssertRunning();
      var topology = await _transport.GetAsync(new TessageTypesInternal.NetworkTopologyTuery(), seedAddress).caf();

      await Task.WhenAll(topology.EndpointAddresses.Select(ConnectAsync)).caf();
   }

   void RegisterRoutes(TypermediaConnection connection, ISet<TypeId> handledTypeIds)
   {
      var tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
      var tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
      foreach(var typeId in handledTypeIds)
      {
         if(_typeMapper.TryGetType(typeId, out var tessageType))
         {
            if(tessageType.Is<IAtMostOnceTypermediaTommand>())
            {
               tommandHandlerRoutes.Add(tessageType, connection);
            } else if(tessageType.Is<IRemotableTuery<object>>())
            {
               tueryHandlerRoutes.Add(tessageType, connection);
            }
            //Silently skip exactly-once types — those are handled by TessagingRouter
         }
      }

      if(tommandHandlerRoutes.Count > 0)
         OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tommandHandlerRoutes, tommandHandlerRoutes);

      if(tueryHandlerRoutes.Count > 0)
         OnlyWithinLocksThreadingHelpers.AddRangeToCopyAndReplace(ref _tueryHandlerRoutes, tueryHandlerRoutes);
   }

   TypermediaConnection ConnectionToHandlerFor(IAtMostOnceTypermediaTommand tommand) =>
      _tommandHandlerRoutes.TryGetValue(tommand.GetType(), out var connection)
         ? connection
         : throw new NoHandlerForTessageTypeException(tommand.GetType());

   TypermediaConnection ConnectionToHandlerFor<TTuery>(IRemotableTuery<TTuery> tuery) =>
      _tueryHandlerRoutes.TryGetValue(tuery.GetType(), out var connection)
         ? connection
         : throw new NoHandlerForTessageTypeException(tuery.GetType());

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
                                ._then(_running = true);

   public void Stop() => AssertRunning()._then(() =>
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

   unit AssertRunning() => State.Assert(_running, () => "not running")._then(unit.Value);
}
