using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Implementation;

[UsedImplicitly]partial class Inbox : IInbox, IAsyncDisposable
{
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