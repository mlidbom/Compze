using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions.Transport;
using Compze.Tessaging.Hosting.Implementation.Abstractions.Transport.Client;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

static class TransportRegistrar
{
   internal static IComponentRegistrar Transport(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.TransportClient.RegisterWith);
}

partial class TransportClient : ITransportClient, IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportClient>().CreatedBy((IMessagesInFlightTracker messagesInFlightTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient)
                                                                     => new TransportClient(messagesInFlightTracker, typeMapper, serializer, remoteApiTransportClient)));

   TransportClient(IMessagesInFlightTracker messagesInFlightTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient)
   {
      _messagesInFlightTracker = messagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _remoteApiTransportClient = remoteApiTransportClient;
      _router = new Router(typeMapper);
   }

   readonly IMessagesInFlightTracker _messagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly IRemoteApiTransportClient _remoteApiTransportClient;

   bool _running = false;
   readonly Router _router;
   IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
      var clientConnection = new Outbox.InboxConnection(_messagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _remoteApiTransportClient);

      await clientConnection.InitAsync().caf();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

      _router.RegisterRoutes(clientConnection, clientConnection.EndpointInformation.HandledMessageTypes);
   }

   public IInboxConnection ConnectionToHandlerFor(IRemotableCommand command) =>
      AssertRunning().then(() => _router.ConnectionToHandlerFor(command));

   public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceEvent @event) =>
      AssertRunning().then(() => _router.SubscriberConnectionsFor(@event));

   public async Task PostAsync(IAtMostOnceHypermediaCommand atMostOnceCommand)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
      await connection.PostAsync(atMostOnceCommand).caf();
   }

   public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> atMostOnceCommand)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
      return await connection.PostAsync(atMostOnceCommand).caf();
   }

   public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(query);
      return await connection.GetAsync(query).caf();
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
