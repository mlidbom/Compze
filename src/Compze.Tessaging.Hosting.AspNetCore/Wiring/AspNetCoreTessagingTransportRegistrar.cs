using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Internals.Transport.AspNet;
using Compze.Tessaging.Hosting.AspNetCore.Private;
using Compze.Tessaging.Implementation.Transport.Client.Implementation;

namespace Compze.Tessaging.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the Tessaging transport speaking HTTP: the client that posts tessages, the
   /// <see cref="TessagingController"/> contributed to the endpoint's one ASP.NET Core transport server (registering the server
   /// itself if no other communication style already did), and the HTTP endpoint transport client plus the endpoint-discovery
   /// query transport that runs on it (both shared with every other communication style, so registered only if nothing else
   /// did yet).
   ///</summary>
   public static IComponentRegistrar AspNetCoreTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.HttpEndpointTransportClientIfNotRegistered()
               .EndpointDiscoveryQueryTransportIfNotRegistered()
               .TessagingTransportMessagePoster()
               .AspNetCoreEndpointTransportServerIfNotRegistered()
               .Register(TessagingController.RegisterWith)
               .Register(Singleton.ForSet<AspNetCoreControllerContribution>().CreatedBy(() => new AspNetCoreControllerContribution(typeof(TessagingController).Assembly)));
}
