using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

static class TransportRegistrar
{
   internal static IComponentRegistrar Transport(this IComponentRegistrar registrar)
      => registrar.Register(RoutingInboxClient.RegisterWith);
}

partial class RoutingInboxClient : IRoutingInboxClient, IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRoutingInboxClient>().CreatedBy((ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster)
                                                                     => new RoutingInboxClient(tessagesInFlightTracker, typeMapper, serializer, transportMessagePoster)));

   RoutingInboxClient(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _inboxConnectionRouter = new InboxConnectionRouter(typeMapper);
   }

   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;

   bool _running = false;
   readonly InboxConnectionRouter _inboxConnectionRouter;
   IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
      var clientConnection = new Outbox.Outbox.InboxConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _transportMessagePoster);

      await clientConnection.InitAsync().caf();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

      //urgent: we can't have routes be discovered at startup based on the assumption that all endpoints are up...
      _inboxConnectionRouter.RegisterRoutes(clientConnection, clientConnection.EndpointInformation.HandledTessageTypes);
   }

   public IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      AssertRunning().then(() => _inboxConnectionRouter.ConnectionToHandlerFor(tommand));

   public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent) =>
      AssertRunning().then(() => _inboxConnectionRouter.SubscriberConnectionsFor(tevent));

   public async Task PostAsync(IAtMostOnceTypermediaTommand atMostOnceTommand)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(atMostOnceTommand);
      await connection.PostAsync(atMostOnceTommand).caf();
   }

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> atMostOnceTypermediaTommand)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(atMostOnceTypermediaTommand);
      return await connection.PostAsync(atMostOnceTypermediaTommand).caf();
   }

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(tuery);
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
