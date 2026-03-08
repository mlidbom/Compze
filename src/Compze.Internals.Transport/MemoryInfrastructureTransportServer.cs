using Compze.Core.Tessaging.Transport.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;

namespace Compze.Internals.Transport;

public static class MemoryInfrastructureTransportServerRegistrar
{
   public static IComponentRegistrar MemoryInfrastructureTransportServer(this IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<MemoryInfrastructureTransportServer>()
                                  .CreatedBy((IServiceLocator serviceLocator) => new MemoryInfrastructureTransportServer(serviceLocator)));
}

public class MemoryInfrastructureTransportServer : ISupplementalTransportServer
{
   readonly LazyCE<InfrastructureQueryExecutor> _executor;
   EndPointAddress? _address;

   public MemoryInfrastructureTransportServer(IServiceLocator serviceLocator) =>
      _executor = new LazyCE<InfrastructureQueryExecutor>(serviceLocator.Resolve<InfrastructureQueryExecutor>);

   public Task StartAsync(EndPointAddress address)
   {
      _address = address;
      InMemoryInfrastructureNetwork.BindExecutor(address, _executor.Value);
      return Task.CompletedTask;
   }

   public Task StopAsync()
   {
      if(_address != null)
         InMemoryInfrastructureNetwork.UnBind(_address);
      return Task.CompletedTask;
   }
}
