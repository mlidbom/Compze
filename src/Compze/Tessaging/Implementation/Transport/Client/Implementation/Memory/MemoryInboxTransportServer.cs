using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

static class MemoryInboxTransportServerRegistrar
{
   internal static IComponentRegistrar MemoryTransport(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IInboxTransportServer>()
                                  .CreatedBy((EndpointId endpointId) => new MemoryInboxTransportServer(endpointId)));
}

class MemoryInboxTransportServer : IInboxTransportServer
{
   internal MemoryInboxTransportServer(EndpointId endpointId) =>
      Address = new Uri($"memory://{endpointId.GuidValue.ToString()}");

   public Uri Address { get; }
   public ValueTask DisposeAsync() => ValueTask.CompletedTask;
   public Task StartAsync() => Task.CompletedTask;
   public Task StopAsync() => Task.CompletedTask;
}
