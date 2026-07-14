using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport.AspNet;

namespace Compze.Typermedia.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTypermediaTransportServerRegistrar
{
   ///<summary>
   /// Registers the ASP.NET Core server side of the Typermedia transport: the <see cref="TypermediaController"/> that
   /// receives tueries and tommands over HTTP, contributed to the endpoint's one ASP.NET Core transport server
   /// (registering the server itself if no other communication style already did). Requires the shared transport
   /// infrastructure (the serializer and the infrastructure-query plumbing) to be registered by the composing layer.
   ///</summary>
   public static IComponentRegistrar AspNetCoreTypermediaTransportServer(this IComponentRegistrar registrar) =>
      registrar.AspNetCoreEndpointTransportServerIfNotRegistered()
               .Register(TypermediaController.RegisterWith)
               .Register(Singleton.ForSet<AspNetCoreControllerContribution>().CreatedBy(() => new AspNetCoreControllerContribution(typeof(TypermediaController).Assembly)));
}
