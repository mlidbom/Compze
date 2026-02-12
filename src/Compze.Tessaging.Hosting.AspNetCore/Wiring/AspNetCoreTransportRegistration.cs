using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTransportRegistrar
{
   public static IComponentRegistrar AspNetCoreTransport(this IComponentRegistrar registrar) =>
      registrar.HttpClientFactoryCE()
               .HttpApiTransportClient()
               .Register(CompzeControllerActivator.RegisterWith,
                         AspNetInboxTransportServer.RegisterWith,
                         TypermediaController.RegisterWith,
                         TessagingController.RegisterWith);
}
