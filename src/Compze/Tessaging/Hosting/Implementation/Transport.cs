using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

internal static class TransportRegistrar
{
   internal static IDependencyRegistrar Transport(this IDependencyRegistrar registrar)
      => registrar.Register(Implementation.Transport.RegisterWith);
}

partial class Transport : ITransport, IDisposable
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<ITransport>().CreatedBy((IMessagesInFlightTracker messagesInFlightTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient)
                                                                     => new Transport(messagesInFlightTracker, typeMapper, serializer, httpApiClient)));

   Transport(IMessagesInFlightTracker messagesInFlightTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient)
   {
      _messagesInFlightTracker = messagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _httpApiClient = httpApiClient;
      _router = new Router(typeMapper);
   }

   readonly IMessagesInFlightTracker _messagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly IHttpApiClient _httpApiClient;

   bool _running = true;
   readonly Router _router;
   IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
      var clientConnection = new Outbox.InboxConnection(_messagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _httpApiClient);

      await clientConnection.Init().caf();

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
