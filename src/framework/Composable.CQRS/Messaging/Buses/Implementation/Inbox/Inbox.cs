using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses.Implementation;

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
      await Task.WhenAll(storageStartTask, _aspNetHost.StartAsync()).CaF();
   }

   public async Task StopAsync() => await _aspNetHost.StopAsync().CaF();

   public async ValueTask DisposeAsync()
   {
      _handlerExecutionEngine.Stop();
      await StopAsync().CaF();
      await _aspNetHost.DisposeAsync().CaF();
   }
}