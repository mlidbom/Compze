using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTransportRegistrar
{
   public static IComponentRegistrar AspNetCoreTransport(this IComponentRegistrar registrar) =>
      registrar.Register(CompzeControllerActivator.RegisterWith,
                         AspNetInboxTransport.RegisterWith,
                         RpcController.RegisterWith,
                         TessagingController.RegisterWith);
}
