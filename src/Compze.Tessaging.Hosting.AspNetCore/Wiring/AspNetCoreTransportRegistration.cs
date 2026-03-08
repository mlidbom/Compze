using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.Transport;
using Compze.Internals.Transport.AspNet;
using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.AspNetCore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTransportRegistrar
{
   static readonly IReadOnlyList<ISupplementalTransportServer> EmptySupplementalServers = [];

   public static IComponentRegistrar AspNetCoreTransport(this IComponentRegistrar registrar) =>
      registrar.HttpClientFactoryCE()
               .HttpApiTransportClient()
               .HttpTypermediaTransport()
               .HttpInfrastructureQueryTransport()
               .Register(CompzeControllerActivator.RegisterWith,
                         AspNetInboxTransportServer.RegisterWith,
                         TypermediaController.RegisterWith,
                         InfrastructureQueryController.RegisterWith,
                         TessagingController.RegisterWith)
               .Register(Singleton.For<IReadOnlyList<ISupplementalTransportServer>>().Instance(EmptySupplementalServers));
}
