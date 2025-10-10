using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Implementation;

static class InboxRegistrar
{
   internal static IDependencyRegistrar Inbox(this IDependencyRegistrar registrar)
      => registrar.Register(Implementation.Inbox.RegisterWith);
}

[UsedImplicitly] partial class Inbox : IInbox, IAsyncDisposable
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(
         Singleton.For<Inbox.IMessageStorage>()
                  .CreatedBy((IServiceBusPersistenceLayer.IInboxPersistenceLayer persistenceLayer)
                                => new InboxMessageStorage(persistenceLayer)),
         Singleton.For<Inbox.HandlerExecutionEngine>()
                  .CreatedBy((IMessagesInFlightTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, IServiceLocator serviceLocator, Inbox.IMessageStorage storage, ITaskRunner taskRunner)
                                => new Inbox.HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, storage, taskRunner)),
         Singleton.For<IInbox>()
                  .CreatedBy((IServiceLocator serviceLocator, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage messageStorage, IDependencyInjectionContainer container, IInboxTransport transport)
                                => new Inbox(serviceLocator, handlerExecutionEngine, messageStorage, container, transport))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly IMessageStorage _storage;
   readonly IInboxTransport _transport;

   public Inbox(IServiceLocator serviceLocator, HandlerExecutionEngine handlerExecutionEngine, IMessageStorage messageStorage, IDependencyInjectionContainer container, IInboxTransport transport)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = messageStorage;
      _transport = transport;
   }

   public EndPointAddress Address => new(aspNetAddress: _transport.Address);

   public async Task StartAsync()
   {
      _handlerExecutionEngine.Start();
      var storageStartTask = _storage.StartAsync();
      await Task.WhenAll(storageStartTask, _transport.StartAsync()).caf();
   }

   public async Task StopAsync() => await _transport.StopAsync().caf();

   public async ValueTask DisposeAsync()
   {
      _handlerExecutionEngine.Stop();
      await StopAsync().caf();
      await _transport.DisposeAsync().caf();
   }
}
