using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport;

///<summary>Where the endpoint's transport server is listening; null until it listens. The narrow view<br/>
/// <see cref="EndpointTransportServerFeature"/> holds of its running <see cref="EndpointTransportServerComponent"/>: the feature<br/>
/// reads the address, while the component's lifetime — including disposal — belongs to the endpoint that runs it.</summary>
interface IListeningAddressSource
{
   EndpointAddress? ListeningAddress { get; }
}

///<summary>The runtime lifecycle of the endpoint's one transport server (see <see cref="EndpointTransportServerFeature"/>):<br/>
/// the server listens through the listening phase, and the endpoint's address is announced to every declared<br/>
/// <see cref="IEndpointAddressAnnouncer"/> in the announcing phase — which the host runs after every endpoint's listening<br/>
/// and before any endpoint's sending, so an announced address is always one whose whole endpoint is actually listening,<br/>
/// and a router taking its first look at a registry when sending starts sees every endpoint the host announced.<br/>
/// Stopping runs in reverse: the announcement is retracted in the retracting phase, before any sending stops,<br/>
/// so the address stops being advertised before anything goes deaf.</summary>
sealed class EndpointTransportServerComponent : IEndpointComponent, IListeningAddressSource, IAsyncDisposable
{
   readonly IEndpointTransportServer _server;
   readonly IReadOnlyList<IEndpointAddressAnnouncer> _addressAnnouncers;
   readonly EndpointConfiguration _configuration;

   bool _isListening;

   internal EndpointTransportServerComponent(IEndpointTransportServer server, IReadOnlyList<IEndpointAddressAnnouncer> addressAnnouncers, EndpointConfiguration configuration)
   {
      _server = server;
      _addressAnnouncers = addressAnnouncers;
      _configuration = configuration;
   }

   ///<summary>The address where the endpoint's transport server listens; null until listening.</summary>
   public EndpointAddress? ListeningAddress => _isListening ? _server.Address : null;

   public async Task StartListeningAsync()
   {
      await _server.StartAsync().caf();
      _isListening = true;
   }

   public Task AnnounceAddressAsync()
   {
      _addressAnnouncers.ForEach(announcer => announcer.AnnounceEndpointAddress(_configuration.Id, _server.Address));
      return Task.CompletedTask;
   }

   public Task RetractAddressAsync()
   {
      _addressAnnouncers.ForEach(announcer => announcer.RetractEndpointAddress(_configuration.Id));
      return Task.CompletedTask;
   }

   public async Task StopListeningAsync()
   {
      _isListening = false;
      await _server.StopAsync().caf();
   }

   public async ValueTask DisposeAsync() => await _server.DisposeAsync().caf(); //The container the server is registered in also disposes it; server disposal is idempotent, and disposing what we hold keeps ownership legible.
}
