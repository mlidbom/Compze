using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public static class TransportRegistrar
{
   public static IComponentRegistrar TypermediaTransport(this IComponentRegistrar registrar)
      => registrar.Register(TypermediaRouter.RegisterWith);

   public static IComponentRegistrar TessagingTransport(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRouter.RegisterWith);
}

public class TypermediaRouter : ITypermediaRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
            Singleton.For<ITypermediaRouter>().CreatedBy(
               (ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster)
                  => new TypermediaRouter(typeMapper, serializer, transportMessagePoster)));

   TypermediaRouter(ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster)
   {
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
   }

   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly IMonitor _monitor = IMonitor.WithDefaultTimeout();


   bool _running;
   IReadOnlyDictionary<EndpointId, TypermediaConnection> _connections = new Dictionary<EndpointId, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tommandHandlerRoutes = new Dictionary<Type, TypermediaConnection>();
   IReadOnlyDictionary<Type, TypermediaConnection> _tueryHandlerRoutes = new Dictionary<Type, TypermediaConnection>();

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new TypermediaConnection(remoteEndpointAddress, _typeMapper, _serializer, _transportMessagePoster);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

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
      var topologyTuery = new TessageTypesInternal.NetworkTopologyTuery();
      var topologyTueryTessage = TransportTessage.OutGoing.Create(topologyTuery, _typeMapper, _serializer);
      var topology = await _transportMessagePoster.PostAsync<TessageTypesInternal.NetworkTopology>(topologyTueryTessage, seedAddress).caf();

      await Task.WhenAll(topology.EndpointAddresses.Select(address => ConnectAsync(address))).caf();
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
      await connection.ApiClient.PostAsync(tommand).caf();
   }

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand)
   {
      AssertRunning();
      var connection = ConnectionToHandlerFor(typermediaTommand);
      return await connection.ApiClient.PostAsync(typermediaTommand).caf();
   }

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery)
   {
      AssertRunning();
      var connection = ConnectionToHandlerFor(tuery);
      return await connection.ApiClient.GetAsync(tuery).caf();
   }

   public void Start() => Contract.State.Assert(!_running, () => "already running")
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

         _connections.Values.DisposeAll();
      }
   }

   unit AssertRunning() => Contract.State.Assert(_running, () => "not running")._then(unit.Value);
}
