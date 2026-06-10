using Compze.Internals.Transport;
using Compze.Internals.Transport.AspNet;
using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTransportRegistrar
{
   public static IComponentRegistrar AspNetCoreTransport(this IComponentRegistrar registrar) =>
      registrar.HttpClientFactoryCE()
               .HttpApiTransportClient()
               .HttpInfrastructureQueryTransport()
               .Register(AspNetInboxTransportServer.RegisterWith,
                         InfrastructureQueryController.RegisterWith,
                         TessagingController.RegisterWith);
}
