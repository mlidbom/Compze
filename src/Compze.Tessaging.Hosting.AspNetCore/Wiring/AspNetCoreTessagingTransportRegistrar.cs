using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the ASP.NET Core implementation of the Tessaging transport: the inbox server and controller that
   /// receive tessages over HTTP, and the client that posts them. Requires the shared transport infrastructure
   /// (<see cref="Compze.Internals.Transport.IHttpClientFactoryCE"/> and the infrastructure-query transport) to be
   /// registered by the composing layer.
   ///</summary>
   public static IComponentRegistrar AspNetCoreTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.HttpApiTransportClient()
               .Register(AspNetInboxTransportServer.RegisterWith,
                         TessagingController.RegisterWith);
}
