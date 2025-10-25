using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation;

static class TransportRegistrar
{
   internal static IComponentRegistrar Transport(this IComponentRegistrar registrar)
      => registrar.Register(RoutingInboxTransportClient.RegisterWith);
}

partial class RoutingInboxTransportClient : IRoutingInboxTransportClient, IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRoutingInboxTransportClient>().CreatedBy((ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient)
                                                                     => new RoutingInboxTransportClient(tessagesInFlightTracker, typeMapper, serializer, remoteApiTransportClient)));

   RoutingInboxTransportClient(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _remoteApiTransportClient = remoteApiTransportClient;
      _router = new Router(typeMapper);
   }

   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly IRemoteApiTransportClient _remoteApiTransportClient;

   bool _running = false;
   readonly Router _router;
   IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();

   public async Task ConnectAsync(HttpEndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
      var clientConnection = new Outbox.Outbox.InboxConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _remoteApiTransportClient);

      await clientConnection.InitAsync().caf();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

      //urgent: we can't have routes be discovered at startup based on the assumption that all endpoints are up...
      _router.RegisterRoutes(clientConnection, clientConnection.EndpointInformation.HandledTessageTypes);
   }

   public IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      AssertRunning().then(() => _router.ConnectionToHandlerFor(tommand));

   public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent) =>
      AssertRunning().then(() => _router.SubscriberConnectionsFor(tevent));

   public async Task PostAsync(IAtMostOnceHypermediaTommand atMostOnceTommand)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(atMostOnceTommand);
      await connection.PostAsync(atMostOnceTommand).caf();
   }

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> atMostOnceTommand)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(atMostOnceTommand);
      return await connection.PostAsync(atMostOnceTommand).caf();
   }

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(tuery);
      return await connection.GetAsync(tuery).caf();
   }

   public void Start() => Assert.State.Is(!_running, () => "already running")
                                .then(_running = true);

   public void Stop() => AssertRunning().then(() =>
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

         _inboxConnections.Values.DisposeAll();
      }
   }

   unit AssertRunning() => Assert.State.Is(_running, () => "not running").then(unit.Value);
}
