using Compze.Core.Tessaging.Transport.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

public static class MemoryTypermediaTransportServerRegistrar
{
   public static IComponentRegistrar MemoryTypermediaTransportServer(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<MemoryTypermediaTransportServer>()
                                  .CreatedBy((IServiceLocator serviceLocator) => new MemoryTypermediaTransportServer(serviceLocator)));
}

public class MemoryTypermediaTransportServer : ISupplementalTransportServer
{
   readonly LazyCE<TypermediaHandlerExecutor> _executor;
   EndPointAddress? _address;

   public MemoryTypermediaTransportServer(IServiceLocator serviceLocator) =>
      _executor = new LazyCE<TypermediaHandlerExecutor>(serviceLocator.Resolve<TypermediaHandlerExecutor>);

   public Task StartAsync(EndPointAddress address)
   {
      _address = address;
      InMemoryTypermediaNetwork.BindExecutor(address, _executor.Value);
      return Task.CompletedTask;
   }

   public Task StopAsync()
   {
      if(_address != null)
         InMemoryTypermediaNetwork.UnBind(_address);
      return Task.CompletedTask;
   }
}
