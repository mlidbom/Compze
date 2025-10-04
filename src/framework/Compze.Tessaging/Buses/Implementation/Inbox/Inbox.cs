using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Buses.Implementation;

[UsedImplicitly]partial class Inbox : IInbox, IAsyncDisposable
{
   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly IMessageStorage _storage;
   readonly AspNetHost _aspNetHost;

   public Inbox(IServiceLocator serviceLocator, HandlerExecutionEngine handlerExecutionEngine, IMessageStorage messageStorage, IDependencyInjectionContainer container, AspNetHost aspNetHost)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = messageStorage;
      _aspNetHost = aspNetHost;
   }

   public EndPointAddress Address => new(aspNetAddress: _aspNetHost.Address);

   public async Task StartAsync()
   {
      _handlerExecutionEngine.Start();
      var storageStartTask = _storage.StartAsync();
      await Task.WhenAll(storageStartTask, _aspNetHost.StartAsync()).caf();
   }

   public async Task StopAsync() => await _aspNetHost.StopAsync().caf();

   public async ValueTask DisposeAsync()
   {
      _handlerExecutionEngine.Stop();
      await StopAsync().caf();
      await _aspNetHost.DisposeAsync().caf();
   }
}