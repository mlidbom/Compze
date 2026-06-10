using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTypermediaTransportServerRegistrar
{
   public static IComponentRegistrar AspNetCoreTypermediaTransportServer(this IComponentRegistrar registrar) =>
      registrar.Register(TypermediaController.RegisterWith,
                         TypermediaTransportServer.RegisterWith);
}
