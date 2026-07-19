using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Internals.Transport.NamedPipes;

static class NamedPipeEndpointTransportClientRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint transport's client side (<see cref="IEndpointTransportClient"/>).<br/>
   /// Guarded so that every named-pipe transport registration can demand it without conflicting when an endpoint speaks several<br/>
   /// communication styles.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransportClientIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointTransportClient>()
            ? registrar
            : registrar.Register(Singleton.For<IEndpointTransportClient>().CreatedBy(() => new NamedPipeEndpointTransportClient()));
}

///<summary>The named-pipe implementation of <see cref="IEndpointTransportClient"/>: sends each request through<br/>
/// <see cref="NamedPipeTransportClient"/> to the endpoint's named-pipe transport server (<see cref="NamedPipeEndpointTransportServer"/>).</summary>
class NamedPipeEndpointTransportClient : IEndpointTransportClient
{
   public async Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default) =>
      await NamedPipeTransportClient.SendAsync(request, address, cancellationToken).caf();
}
