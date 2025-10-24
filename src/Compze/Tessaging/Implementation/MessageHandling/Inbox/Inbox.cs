using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.MessageHandling;

static class InboxRegistrar
{
   internal static IComponentRegistrar Inbox(this IComponentRegistrar registrar)
      => registrar.Register(MessageHandling.Inbox.RegisterWith);
}

[UsedImplicitly] partial class Inbox : IInbox, IAsyncDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<Inbox.IMessageStorage>()
                  .CreatedBy((IServiceBusSqlLayer.IInboxSqlLayer sqlLayer)
                                => new InboxMessageStorage(sqlLayer)),
         Singleton.For<Inbox.HandlerExecutionEngine>()
                  .CreatedBy((IMessagesInFlightTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, IServiceLocator serviceLocator, Inbox.IMessageStorage storage, ITaskRunner taskRunner, EndpointConfiguration configuration)
                                => new Inbox.HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, storage, taskRunner, configuration.Id)),
         Singleton.For<IInbox>()
                  .CreatedBy((IServiceLocator serviceLocator, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage messageStorage, IDependencyInjectionContainer container, IInboxTransportServer transportServer)
                                => new Inbox(serviceLocator, handlerExecutionEngine, messageStorage, container, transportServer))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly IMessageStorage _storage;
   readonly IInboxTransportServer _transportServer;

   public Inbox(IServiceLocator serviceLocator, HandlerExecutionEngine handlerExecutionEngine, IMessageStorage messageStorage, IDependencyInjectionContainer container, IInboxTransportServer transportServer)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = messageStorage;
      _transportServer = transportServer;
   }

   public HttpEndPointAddress Address => new(aspNetAddress: _transportServer.Address);

   public async Task StartAsync()
   {
      _handlerExecutionEngine.Start();
      var storageStartTask = _storage.StartAsync();
      await Task.WhenAll(storageStartTask, _transportServer.StartAsync()).caf();
   }

   public async Task<object?> Receive(TransportMessage.InComing message)
   {
      var saveResult = _storage.SaveIncomingMessage(message);

      if(saveResult == IServiceBusSqlLayer.SaveMessageResult.Duplicate)
      {
         return null;
      }

      return await _handlerExecutionEngine.Enqueue(message).caf();
   }

   public async Task StopAsync() => await _transportServer.StopAsync().caf();

   public async ValueTask DisposeAsync()
   {
      _handlerExecutionEngine.Stop();
      await StopAsync().caf();
      await _transportServer.DisposeAsync().caf();
   }
}
