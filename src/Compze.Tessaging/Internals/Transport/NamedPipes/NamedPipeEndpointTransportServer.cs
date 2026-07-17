using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Internals.Transport.NamedPipes;

public static class NamedPipeEndpointTransportServerRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint's one transport server (<see cref="IEndpointTransportServer"/>)<br/>
   /// together with the endpoint's <see cref="TransportRequestHandlerMap"/> it serves, unless a transport already registered one —<br/>
   /// guarded so that every communication style's named-pipe transport registration can demand the server without conflicting when<br/>
   /// an endpoint hosts several styles.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransportServerIfNotRegistered(this IComponentRegistrar registrar) =>
      registrar.IsRegistered<IEndpointTransportServer>()
         ? registrar
         : registrar.Register(TransportRequestHandlerMap.RegisterWith)
                    .Register(
                       Singleton.For<IEndpointTransportServer>()
                                .CreatedBy((TransportRequestHandlerMap handlerMap) => new NamedPipeEndpointTransportServer(handlerMap)));
}

///<summary>The named-pipe implementation of <see cref="IEndpointTransportServer"/>: one <see cref="NamedPipeTransportServer"/> serving<br/>
/// the endpoint's <see cref="TransportRequestHandlerMap"/> — every communication style's contributed request handlers plus<br/>
/// endpoint discovery.</summary>
class NamedPipeEndpointTransportServer : IEndpointTransportServer
{
   readonly NamedPipeTransportServer _server;

   internal NamedPipeEndpointTransportServer(TransportRequestHandlerMap handlerMap) => _server = new NamedPipeTransportServer(handlerMap.HandleAsync);

   public EndpointAddress Address => _server.Address;

   public async Task StartAsync() => await _server.StartAsync().caf();
   public async Task StopAsync() => await _server.StopAsync().caf();
   public async ValueTask DisposeAsync() => await _server.DisposeAsync().caf();
}
