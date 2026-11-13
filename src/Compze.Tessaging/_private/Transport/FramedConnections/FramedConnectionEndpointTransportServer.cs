using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>The framed-connection transports' implementation of <see cref="IEndpointTransportServer"/>: one<br/>
/// <see cref="FramedConnectionServer"/> serving the endpoint's <see cref="TransportRequestHandlerMap"/> — every communication<br/>
/// style's contributed request handlers plus endpoint discovery. Which transport it listens on is decided by the<br/>
/// <see cref="IConnectionListener"/> it is given.</summary>
class FramedConnectionEndpointTransportServer : IEndpointTransportServer
{
   readonly FramedConnectionServer _server;
   volatile EndpointAddress? _address;

   internal FramedConnectionEndpointTransportServer(IConnectionListener listener, TransportRequestHandlerMap handlerMap) =>
      _server = new FramedConnectionServer(listener, handlerMap.HandleAsync);

   ///<summary>Null until the server is listening and again once it stops, honoring the endpoint transport server contract<br/>
   /// (see <see cref="IEndpointTransportServer.Address"/>).</summary>
   public EndpointAddress? Address => _address;

   public Task StartAsync()
   {
      _server.Start();
      _address = _server.Address;
      return Task.CompletedTask;
   }

   public async Task StopAsync()
   {
      _address = null;
      await _server.StopAsync().caf();
   }

   public async ValueTask DisposeAsync() => await _server.DisposeAsync().caf();
}
