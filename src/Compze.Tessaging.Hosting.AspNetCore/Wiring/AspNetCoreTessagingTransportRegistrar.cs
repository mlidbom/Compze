using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport.AspNet;
using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the ASP.NET Core implementation of the Tessaging transport: the client that posts tessages over HTTP, and the
   /// <see cref="TessagingController"/> contributed to the endpoint's one ASP.NET Core transport server (registering the server
   /// itself if no other communication style already did). Requires the shared transport infrastructure
   /// (<see cref="Compze.Internals.Transport.IHttpClientFactoryCE"/> and the infrastructure-query transport) to be
   /// registered by the composing layer.
   ///</summary>
   public static IComponentRegistrar AspNetCoreTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.HttpApiTransportClient()
               .AspNetCoreEndpointTransportServerIfNotRegistered()
               .Register(TessagingController.RegisterWith)
               .Register(Singleton.ForSet<AspNetCoreControllerContribution>().CreatedBy(() => new AspNetCoreControllerContribution(typeof(TessagingController).Assembly)));
}
