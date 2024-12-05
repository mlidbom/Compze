using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Contracts.Deprecated;
using Compze.Functional;
using Compze.Messaging.Buses.Http;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Messaging.Buses.Implementation;

partial class Transport(IGlobalBusStateTracker globalBusStateTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient) : ITransport, IDisposable
{
   readonly IGlobalBusStateTracker _globalBusStateTracker = globalBusStateTracker;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly IHttpApiClient _httpApiClient = httpApiClient;

   bool _running = true;
   readonly Router _router = new(typeMapper);
   IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();

   public async Task ConnectAsync(EndPointAddress remoteEndpointAdress)
   {
      AssertRunning();
      var clientConnection = new Outbox.InboxConnection(_globalBusStateTracker, remoteEndpointAdress, _typeMapper, _serializer, _httpApiClient);

      await clientConnection.Init().CaF();

      ThreadSafe.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

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
      await connection.PostAsync(atMostOnceCommand).CaF();
   }

   public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> atMostOnceCommand)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
      return await connection.PostAsync(atMostOnceCommand).CaF();
   }

   public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
   {
      AssertRunning();
      var connection = _router.ConnectionToHandlerFor(query);
      return await connection.GetAsync(query).CaF();
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

   Unit AssertRunning() => Contract.Assert.That(_running, "not running");
}
