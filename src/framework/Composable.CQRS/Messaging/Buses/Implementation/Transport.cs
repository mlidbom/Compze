using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Http;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation;

partial class Transport : ITransport, IDisposable
{
   readonly IGlobalBusStateTracker _globalBusStateTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly IRpcClient _rpcClient;
   readonly IMessageSender _messageSender;

   bool _running;
   readonly Router _router;
   IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();
   readonly AssertAndRun _runningAndNotDisposed;

   public Transport(IGlobalBusStateTracker globalBusStateTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IRpcClient rpcClient, IMessageSender messageSender)
   {
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse ReSharper incorrectly believes nullable reference types to deliver runtime guarantees.
      _runningAndNotDisposed = new AssertAndRun(() => Assert.State.Assert(_running));
      _router = new Router(typeMapper);
      _serializer = serializer;
      _rpcClient = rpcClient;
      _messageSender = messageSender;
      _globalBusStateTracker = globalBusStateTracker;
      _typeMapper = typeMapper;
      _running = true;
   }

   public async Task ConnectAsync(EndPointAddress remoteEndpointAdress)
   {
      _runningAndNotDisposed.Assert();
      var clientConnection = new Outbox.InboxConnection(_globalBusStateTracker, remoteEndpointAdress, _typeMapper, _serializer, _rpcClient, _messageSender);

      await clientConnection.Init().CaF();

      ThreadSafe.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

      _router.RegisterRoutes(clientConnection, clientConnection.EndpointInformation.HandledMessageTypes);
   }

   public IInboxConnection ConnectionToHandlerFor(IRemotableCommand command) =>
      _runningAndNotDisposed.Do(() => _router.ConnectionToHandlerFor(command));

   public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceEvent @event) =>
      _runningAndNotDisposed.Do(() => _router.SubscriberConnectionsFor(@event));

   public async Task PostAsync(IAtMostOnceHypermediaCommand atMostOnceCommand)
   {
      _runningAndNotDisposed.Assert();
      var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
      await connection.PostAsync(atMostOnceCommand).CaF();
   }

   public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> atMostOnceCommand)
   {
      _runningAndNotDisposed.Assert();
      var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
      return await connection.PostAsync(atMostOnceCommand).CaF();
   }

   public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
   {
      _runningAndNotDisposed.Assert();
      var connection = _router.ConnectionToHandlerFor(query);
      return await connection.GetAsync(query).CaF();
   }

   public void Stop() => _runningAndNotDisposed.Do(() =>
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
}