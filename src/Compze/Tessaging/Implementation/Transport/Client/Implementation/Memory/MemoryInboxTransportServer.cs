using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

class MemoryInboxTransportServer : IInboxTransportServer
{
   internal MemoryInboxTransportServer(EndpointId endpointId) =>
      Address = new Uri($"memory://{endpointId.GuidValue.ToString()}");

   public Uri Address { get; }
   public ValueTask DisposeAsync() => ValueTask.CompletedTask;
   public Task StartAsync() => Task.CompletedTask;
   public Task StopAsync() => Task.CompletedTask;
}
