using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Internals.Transport.AspNet;
using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the ASP.NET Core implementation of the Tessaging transport: the client that posts tessages over HTTP, the
   /// <see cref="TessagingController"/> contributed to the endpoint's one ASP.NET Core transport server (registering the server
   /// itself if no other communication style already did), and the HTTP endpoint-discovery query transport that discovery
   /// runs on (shared with every other HTTP communication style, so registered only if nothing else did yet).
   ///</summary>
   public static IComponentRegistrar AspNetCoreTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.HttpEndpointDiscoveryQueryTransportIfNotRegistered()
               .HttpApiTransportClient()
               .AspNetCoreEndpointTransportServerIfNotRegistered()
               .Register(TessagingController.RegisterWith)
               .Register(Singleton.ForSet<AspNetCoreControllerContribution>().CreatedBy(() => new AspNetCoreControllerContribution(typeof(TessagingController).Assembly)));
}
