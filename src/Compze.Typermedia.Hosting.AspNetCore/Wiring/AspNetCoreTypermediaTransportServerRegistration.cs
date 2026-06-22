using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.Hosting.AspNetCore.Wiring;

public static class AspNetCoreTypermediaTransportServerRegistrar
{
   ///<summary>
   /// Registers the ASP.NET Core server side of the Typermedia transport: the <see cref="TypermediaTransportServer"/>
   /// whose lifecycle the endpoint's Typermedia component drives, and the <see cref="TypermediaController"/> that
   /// receives tueries and tommands over HTTP. Requires the shared transport infrastructure (the serializer and the
   /// infrastructure-query plumbing) to be registered by the composing layer.
   ///</summary>
   public static IComponentRegistrar AspNetCoreTypermediaTransportServer(this IComponentRegistrar registrar) =>
      registrar.Register(TypermediaController.RegisterWith,
                         TypermediaTransportServer.RegisterWith);
}
