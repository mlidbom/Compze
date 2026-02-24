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
using Compze.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public static class TransportRegistrar
{
   public static IComponentRegistrar Transport(this IComponentRegistrar registrar)
      => registrar.Register(RoutingInboxClient.RegisterWith);
}

public partial class RoutingInboxClient : IRoutingInboxClient, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
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
#pragma warning disable CA2000//We are passing this disposable into a collection that we  track disposal for
      var clientConnection = new Outbox.Outbox.InboxConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _transportMessagePoster);
#pragma warning restore CA2000

      await clientConnection.InitAsync().caf();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

      //urgent: we can't have routes be discovered at startup based on the assumption that all endpoints are up...
      _inboxConnectionRouter.RegisterRoutes(clientConnection, clientConnection.EndpointInformation.HandledTessageTypes);
   }

   public IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      AssertRunning()._then(() => _inboxConnectionRouter.ConnectionToHandlerFor(tommand));

   public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent) =>
      AssertRunning()._then(() => _inboxConnectionRouter.SubscriberConnectionsFor(tevent));

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(tommand);
      await connection.PostAsync(tommand).caf();
   }

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(typermediaTommand);
      return await connection.PostAsync(typermediaTommand).caf();
   }

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(tuery);
      return await connection.GetAsync(tuery).caf();
   }

   public void Start() => Assert.State.Is(!_running, () => "already running")
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

         _inboxConnections.Values.DisposeAll();
      }
   }

   unit AssertRunning() => Assert.State.Is(_running, () => "not running")._then(unit.Value);
}
