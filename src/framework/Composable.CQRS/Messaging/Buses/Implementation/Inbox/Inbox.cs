using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses.Implementation;

[UsedImplicitly]partial class Inbox : IInbox, IAsyncDisposable
{
   readonly Runner _runner;

   readonly IMessageStorage _storage;
   readonly AspNetHost _aspNetHost;

   public Inbox(IServiceLocator serviceLocator, Runner runner, IMessageStorage messageStorage, IDependencyInjectionContainer container)
   {
      _runner = runner;
      _storage = messageStorage;
      _aspNetHost = new AspNetHost(serviceLocator, container);
   }

   public EndPointAddress Address => new(netMqAddress: _runner.Address, aspNetAddress: _aspNetHost.Address);

   public async Task StartAsync()
   {
      var storageStartTask = _storage.StartAsync();
      await Task.WhenAll(_runner.StartAsync(), storageStartTask, _aspNetHost.StartAsync()).CaF();
   }

   public async Task StopAsync()
   {
      _runner.Dispose();
      await _aspNetHost.StopAsync().CaF();
   }


   public async ValueTask DisposeAsync()
   {
      await StopAsync().CaF();
      await _aspNetHost.DisposeAsync().CaF();
   }
}