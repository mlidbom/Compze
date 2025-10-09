using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;

public static class AspNetCoreTransportRegistrar
{
   public static IDependencyRegistrar AspNetCoreTransport(this IDependencyRegistrar registrar) =>
      registrar.Register(CompzeControllerActivator.RegisterWith,
                         AspNetInboxTransport.RegisterWith,
                         RpcController.RegisterWith,
                         TessagingController.RegisterWith);
}
